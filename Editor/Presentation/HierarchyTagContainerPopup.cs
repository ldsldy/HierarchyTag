using System.Collections.Generic;
using System;
using UnityEditor;
using HierarchyTags.Contracts;
using UnityEngine;

namespace HierarchyTags.Editor.Presentation
{
    internal sealed class HierarchyTagContainerPopup : PopupWindowContent
    {
        private enum SelectionState
        {
            None,      // 어느 오브젝트에도 이 태그가 없는 상태
            Exact,     // 모든 오브젝트가 이 태그를 직접 가진 상태
            Inherited, // 모든 오브젝트가 자식 태그를 통해 이 태그와 일치하는 상태
            Mixed      // 오브젝트마다 직접 보유 또는 계층 일치 상태가 다른 상태
        }

        #region UI Padding and Size
        private const float LeftPadding = 8f;
        private const float IndentWidth = 16f;
        private const float MinimumTreeWidth = 420f;
        #endregion

        private readonly UnityEngine.Object[] targetObjects;
        private readonly string containerPropertyPath;

        private readonly Func<ITagCatalog> readCatalog;

        private ITagCatalog catalog;

        private IReadOnlyList<TagInfo> rootNodes = Array.Empty<TagInfo>();

        private readonly Dictionary<string, bool> foldoutStates = new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<HierarchyTag, int> exactTagCounts = new();

        private readonly Dictionary<HierarchyTag, int> matchingTagCounts = new();

        private Vector2 scrollPosition;
        private int validTargetCount;

        public HierarchyTagContainerPopup(
            UnityEngine.Object[] targetObjects, string containerPropertyPath, Func<ITagCatalog> readCatalog)
        {
            this.targetObjects = targetObjects ?? throw new ArgumentNullException(nameof(targetObjects));

            if (string.IsNullOrEmpty(containerPropertyPath))
            {
                throw new ArgumentException("Container Property 경로가 필요합니다.", nameof(containerPropertyPath));
            }

            this.containerPropertyPath = containerPropertyPath;

            this.readCatalog = readCatalog ?? throw new ArgumentNullException(nameof(readCatalog));

            RefreshSelectionStates();
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(360f, 420f);
        }

        public override void OnGUI(Rect rect)
        {
            if (!RefreshCatalog())
            {
                EditorGUILayout.HelpBox(
                    "태그 사전을 사용할 수 없습니다. " +
                    "갱신 상태와 Console을 확인하세요.",
                    MessageType.Warning);

                return;
            }

            EditorGUILayout.LabelField("Hierarchy Tags", EditorStyles.boldLabel);
            EditorGUILayout.Space(2f);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, true, true);

            EditorGUILayout.BeginVertical(GUILayout.MinWidth(Mathf.Max(MinimumTreeWidth, rect.width - 20f)));

            for (int index = 0; index < rootNodes.Count; ++index)
            {
                DrawTagNode(rootNodes[index], 0);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(4f);

            using(new EditorGUI.DisabledScope(exactTagCounts.Count == 0))
            {
                /* 선택된 태그가 하나도 없으면 Clear All 버튼을 비활성화합니다. */
                if (GUILayout.Button("전부 제거"))
                {
                    ClearAllTags();
                }
            }
        }

        private bool RefreshCatalog()
        {
            ITagCatalog next = readCatalog();

            if (next == null)
            {
                return false;
            }

            if (ReferenceEquals(catalog, next))
            {
                return true;
            }

            var roots = new List<TagInfo>();

            foreach (TagInfo info in next.Tags)
            {
                if (info.Parent.IsNone)
                {
                    roots.Add(info);
                }
            }

            catalog = next;
            rootNodes = roots.AsReadOnly();

            return true;
        }

        private void DrawTagNode(TagInfo node, int depth)
        {
            bool hasChildren = node.Children.Count > 0;

            var tag = new HierarchyTag(node.Tag.Value);

            string value = node.Tag.Value;
            string displayName = value.Substring(value.LastIndexOf('.') + 1);

            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(LeftPadding + IndentWidth * depth);

            SelectionState selectionState = GetSelectionState(tag);
            bool isSelected = selectionState == SelectionState.Exact;

            bool previousMixedValue = EditorGUI.showMixedValue;

            EditorGUI.BeginChangeCheck();

            EditorGUI.showMixedValue =
                selectionState == SelectionState.Inherited ||
                selectionState == SelectionState.Mixed;

            bool newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(18f));

            EditorGUI.showMixedValue = previousMixedValue;

            if (EditorGUI.EndChangeCheck())
            {
                SetTagSelected(tag, newSelected);
            }

            GUILayout.Space(2f);

            bool expanded = false;

            if (hasChildren)
            {
                expanded = GetFoldoutState(value);

                expanded = EditorGUILayout.Foldout(
                    expanded,
                    displayName,
                    true);

                foldoutStates[value] = expanded;
            }
            else
            {
                GUILayout.Space(14f);
                EditorGUILayout.LabelField(displayName);
            }

            EditorGUILayout.EndHorizontal();

            if (!hasChildren || !expanded)
            {
                return;
            }

            foreach (TagId child in node.Children)
            {
                if (catalog.TryGetInfo(child, out TagInfo childInfo))
                {
                    DrawTagNode(childInfo, depth + 1);
                }
            }
        }

        private bool GetFoldoutState(string tagValue)
        {
            if (foldoutStates.TryGetValue(tagValue, out bool expanded))
            {
                /* 이미 저장된 Foldout 상태가 있는 경우, 해당 상태를 반환합니다. */
                return expanded;
            }

            /* 저장된 Foldout 상태가 없는 경우, 기본적으로 닫힌 상태(false)로 초기화하고 반환합니다. */
            foldoutStates.Add(tagValue, false);

            return false;
        }

        private SelectionState GetSelectionState(HierarchyTag tag)
        {
            exactTagCounts.TryGetValue(tag, out int exactCount);
            matchingTagCounts.TryGetValue(tag, out int matchingCount);

            if (exactCount == 0 && matchingCount == 0)
            {
                return SelectionState.None;
            }

            if (validTargetCount > 0
                && exactCount == validTargetCount)
            {
                return SelectionState.Exact;
            }

            if (validTargetCount > 0
                && exactCount == 0
                && matchingCount == validTargetCount)
            {
                return SelectionState.Inherited;
            }

            return SelectionState.Mixed;
        }

        private void SetTagSelected(HierarchyTag tag, bool newSelected)
        {
            /* 직렬화 유틸리티를 통해 선택된 태그를 업데이트합니다. */
            HierarchyTagSerializedPropertyUtility.SetContainerTag(
                targetObjects,
                containerPropertyPath,
                tag,
                newSelected);

            RefreshSelectionStates();

            editorWindow?.Repaint();
        }

        private void ClearAllTags()
        {
            HierarchyTagSerializedPropertyUtility.ClearContainer(targetObjects, containerPropertyPath);

            RefreshSelectionStates();

            editorWindow?.Repaint();
        }

        private void RefreshSelectionStates()
        {
            exactTagCounts.Clear();
            matchingTagCounts.Clear();
            validTargetCount = 0;

            for (int targetIndex = 0; targetIndex < targetObjects.Length; targetIndex++)
            {
                UnityEngine.Object targetObject = targetObjects[targetIndex];

                if (targetObject == null)
                {
                    continue;
                }
                using var serializedObject = new SerializedObject(targetObject);
                serializedObject.Update();

                SerializedProperty containerProperty = serializedObject.FindProperty(containerPropertyPath);
                SerializedProperty tagsProperty = HierarchyTagSerializedPropertyUtility.FindContainerTagsProperty(containerProperty);

                if (tagsProperty == null)
                {
                    continue;
                }

                validTargetCount++;

                List<HierarchyTag> currentTags = HierarchyTagSerializedPropertyUtility.ReadContainerTags(tagsProperty);

                var matchingTags = new HashSet<HierarchyTag>();

                for (int tagIndex = 0; tagIndex < currentTags.Count; tagIndex++)
                {
                    HierarchyTag tag = currentTags[tagIndex];

                    exactTagCounts.TryGetValue(tag, out int exactCount);

                    exactTagCounts[tag] = exactCount + 1;

                    HierarchyTag matchingTag = tag;

                    while (true)
                    {
                        matchingTags.Add(matchingTag);

                        if (!matchingTag.TryGetParent(
                                out HierarchyTag parentTag))
                        {
                            break;
                        }

                        matchingTag = parentTag;
                    }
                }

                foreach (HierarchyTag matchingTag in matchingTags)
                {
                    matchingTagCounts.TryGetValue(
                        matchingTag,
                        out int matchingCount);

                    matchingTagCounts[matchingTag] =
                        matchingCount + 1;
                }
            }
        }
    }
}
