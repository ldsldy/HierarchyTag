using System;
using System.Collections.Generic;
using HierarchyTags.Application;
using HierarchyTags.Contracts;
using UnityEditor;
using UnityEngine;

namespace HierarchyTags.Editor.Presentation
{
    /// <summary>
    /// 통합 태그 트리와 수동 등록 편집 화면을 표시합니다.
    /// </summary>
    internal sealed class HierarchyTagSettingsView
    {
        private readonly Dictionary<TagId, bool> foldoutStates = new();

        private string pendingTag = string.Empty;
        private string message = string.Empty;
        private MessageType messageType = MessageType.None;
        private Vector2 scrollPosition;

        private TagId tagBeingRenamed = TagId.None;
        private string renamedValue = string.Empty;

        public void Draw(
            ManualTagService manualTags,
            ITagCatalog catalog,
            string status,
            Action refresh)
        {
            bool addRequested;
            bool renameRequested;
            bool refreshRequested;
            TagId tagToRemove = TagId.None;

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            try
            {
                EditorGUILayout.LabelField("Hierarchy Tags", EditorStyles.boldLabel);

                addRequested = DrawAddField();
                renameRequested = DrawRenameField(catalog);

                if (messageType != MessageType.None)
                {
                    EditorGUILayout.HelpBox(message, messageType);
                }

                EditorGUILayout.Space();

                refreshRequested = GUILayout.Button("사전 새로고침");

                if (catalog == null)
                {
                    EditorGUILayout.HelpBox(
                        string.IsNullOrEmpty(status)
                            ? "태그 사전을 사용할 수 없습니다."
                            : status,
                        MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.LabelField($"등록된 태그: {catalog.Tags.Count}", EditorStyles.boldLabel);

                    if (catalog.Tags.Count == 0)
                    {
                        EditorGUILayout.LabelField("등록된 태그가 없습니다.");
                    }

                    foreach (TagInfo info in catalog.Tags)
                    {
                        if (info.Parent.IsNone)
                        {
                            DrawNode(
                                catalog,
                                info,
                                manualTags.Source,
                                ref tagToRemove);
                        }
                    }
                }
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }

            // 화면을 모두 그린 뒤 변경 명령을 실행합니다.
            if (renameRequested)
            {
                ApplyRename(manualTags, catalog);
            }
            else if (addRequested)
            {
                ApplyChange(manualTags, catalog, pendingTag, false);
            }
            else if (!tagToRemove.IsNone)
            {
                ApplyChange(manualTags, catalog, tagToRemove.Value, true);
            }
            else if (refreshRequested)
            {
                refresh();
            }
        }

        private bool DrawRenameField(ITagCatalog catalog)
        {
            if (tagBeingRenamed.IsNone)
            {
                return false;
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("이름 변경 대상", tagBeingRenamed.Value);

            bool applyRequested;

            using (new EditorGUILayout.HorizontalScope())
            {
                renamedValue = EditorGUILayout.TextField("새 이름", renamedValue);

                using (new EditorGUI.DisabledScope(
                    catalog == null ||
                    string.IsNullOrWhiteSpace(renamedValue)))
                {
                    applyRequested = GUILayout.Button("변경", GUILayout.Width(70f));
                }

                if (GUILayout.Button("취소", GUILayout.Width(70f)))
                {
                    tagBeingRenamed = TagId.None;
                    renamedValue = string.Empty;
                }
            }

            return applyRequested;
        }

        private bool DrawAddField()
        {
            bool addRequested;

            EditorGUILayout.BeginHorizontal();

            pendingTag = EditorGUILayout.TextField("새 태그", pendingTag);

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(pendingTag)))
            {
                addRequested = GUILayout.Button("추가", GUILayout.Width(70f));
            }

            EditorGUILayout.EndHorizontal();

            return addRequested;
        }

        private void DrawNode(
            ITagCatalog catalog,
            TagInfo info,
            TagSourceId editableSource,
            ref TagId tagToRemove)
        {
            string value = info.Tag.Value;
            string displayName = value.Substring(value.LastIndexOf('.') + 1);
            GUIContent nodeContent = CreateNodeContent(info, displayName);

            bool hasChildren = info.Children.Count > 0;
            bool expanded = false;

            EditorGUILayout.BeginHorizontal();

            if (hasChildren)
            {
                foldoutStates.TryGetValue(info.Tag, out expanded);

                expanded = EditorGUILayout.Foldout(expanded, nodeContent, true);

                foldoutStates[info.Tag] = expanded;
            }
            else
            {
                EditorGUILayout.LabelField(
                    new GUIContent($"    {nodeContent.text}", nodeContent.tooltip));
            }

            EditorGUILayout.LabelField(CreateSourceContent(info), GUILayout.Width(100f));

            // 현재 편집 가능한 출처(Config나 에디터)에서 등록이 있는지 확인합니다.
            bool canRemove = HasRegistrationFrom(info, editableSource);

            string tooltip = canRemove
                ? "현재 설정의 등록만 삭제합니다. " +
                  "다른 출처나 자식 때문에 필요한 태그는 남습니다."
                : info.IsExplicit
                    ? "현재 설정의 등록이 없습니다. " +
                      "코드 태그는 선언에서 제거하세요."
                    : "자식 때문에 생성된 부모입니다. " +
                      "직접 삭제할 등록이 없습니다.";

            using (new EditorGUI.DisabledScope(!canRemove))
            {
                if (GUILayout.Button(
                    new GUIContent(
                        "이름 변경",
                        "현재 설정의 태그 이름을 변경하고 Redirect를 저장합니다."),
                    GUILayout.Width(80f)))
                {
                    tagBeingRenamed = info.Tag;
                    renamedValue = info.Tag.Value;

                    message = string.Empty;
                    messageType = MessageType.None;
                }

                if (GUILayout.Button(
                    new GUIContent("수동 등록 삭제", tooltip),
                    GUILayout.Width(110f)))
                {
                    tagToRemove = info.Tag;
                }
            }

            EditorGUILayout.EndHorizontal();

            if (!hasChildren || !expanded)
            {
                return;
            }

            EditorGUI.indentLevel++;

            try
            {
                foreach (TagId child in info.Children)
                {
                    if (catalog.TryGetInfo(child, out TagInfo childInfo))
                    {
                        DrawNode(
                            catalog,
                            childInfo,
                            editableSource,
                            ref tagToRemove);
                    }
                }
            }
            finally
            {
                EditorGUI.indentLevel--;
            }
        }

        private static GUIContent CreateNodeContent(
            TagInfo info,
            string displayName)
        {
            string fullName = info.Tag.Value;

            if (info.Parent.IsNone)
            {
                return new GUIContent(displayName, fullName);
            }

            return new GUIContent(
                $"{displayName}  ({fullName})",
                fullName);
        }

        private static GUIContent CreateSourceContent(TagInfo info)
        {
            if (!info.IsExplicit)
            {
                return new GUIContent("자동 부모", "자식 태그의 계층을 구성하기 위해 생성되었습니다.");
            }

            bool hasCode = false;
            bool hasConfig = false;

            var sourceNames = new List<string>();

            foreach (TagRegistration registration in info.Registrations)
            {
                TagSourceId source = registration.Source;

                hasCode |= source.Kind == TagSourceKind.Code;
                hasConfig |= source.Kind == TagSourceKind.Config;

                sourceNames.Add(source.ToString());
            }

            // 등록 출처가 코드와 설정 모두 있는 경우 "코드 · 설정"으로 표시하고, 하나만 있는 경우 해당 이름을 표시합니다.
            string label = hasCode && hasConfig
                ? "코드 · 설정"
                : hasCode
                    ? "코드"
                    : "설정";

            return new GUIContent(label, string.Join("\n", sourceNames));
        }

        private static bool HasRegistrationFrom(TagInfo info, TagSourceId source)
        {
            foreach (TagRegistration registration in info.Registrations)
            {
                if (registration.Source == source)
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplyRename(ManualTagService service, ITagCatalog catalog)
        {
            try
            {
                string oldValue = tagBeingRenamed.Value;
                string newValue = renamedValue;

                if (!service.TryRename(
                    oldValue,
                    newValue,
                    catalog,
                    out string error))
                {
                    message = error;
                    messageType = MessageType.Error;
                    return;
                }

                message =
                    $"이름 변경과 Redirect를 저장했습니다: " +
                    $"{oldValue} → {newValue}";

                messageType = MessageType.Info;

                tagBeingRenamed = TagId.None;
                renamedValue = string.Empty;
            }
            catch (Exception exception)
            {
                message =
                    "이름 변경 중 오류가 발생했습니다: " +
                    exception.Message;

                messageType = MessageType.Error;
                Debug.LogException(exception);
            }
        }

        private void ApplyChange(
            ManualTagService service,
            ITagCatalog catalog,
            string value,
            bool remove)
        {
            try
            {
                string error;

                bool success = remove
                    ? service.TryRemove(value, catalog, out error)
                    : service.TryAdd(value, catalog, out error);

                if (!success)
                {
                    message = error;
                    messageType = MessageType.Error;
                    return;
                }

                message = remove
                    ? $"수동 등록 삭제를 저장했습니다: {value}"
                    : $"수동 등록을 저장했습니다: {value}";

                messageType = MessageType.Info;

                if (!remove)
                {
                    pendingTag = string.Empty;
                }
            }
            catch (Exception exception)
            {
                message = "설정 변경 중 오류가 발생했습니다: " + exception.Message;

                messageType = MessageType.Error;
                Debug.LogException(exception);
            }
        }
    }
}
