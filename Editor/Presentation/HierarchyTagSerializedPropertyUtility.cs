using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HierarchyTags.Editor.Presentation
{
    /// <summary>
    /// HierarchyTag 및 HierarchyTagContainer의 직렬화 데이터를 다루는 유틸리티 클래스입니다.
    /// </summary>
    /// <remarks>
    /// SerializedProperty를 전달받아 값을 찾고 읽고 쓰는 하위 레벨 헬퍼와,
    /// UnityEngine.Object를 직접 받아 SerializedObject 생성부터 ApplyModifiedProperties까지
    /// 전체 반영 과정을 수행하는 상위 레벨 메서드를 함께 제공합니다.
    /// </remarks>
    internal static class HierarchyTagSerializedPropertyUtility
    {
        /// <summary>
        /// tag 직렬화 속성에서 value 직렬화 속성을 찾습니다.
        /// </summary>
        internal static SerializedProperty FindTagValueProperty(SerializedProperty tagProperty)
        {
            if (tagProperty == null)
            {
                return null;
            }

            SerializedProperty nameProperty = tagProperty.FindPropertyRelative("name");

            return nameProperty?.FindPropertyRelative("value");
        }

        /// <summary>
        /// container 속성에서 tags 직렬화 속성을 찾습니다.
        /// </summary>
        internal static SerializedProperty FindContainerTagsProperty(SerializedProperty containerProperty)
        {
            return containerProperty?.FindPropertyRelative("tags");
        }

        /// <summary>
        /// tag 직렬화 속성에서 value 값을 가져옵니다.
        /// </summary>
        internal static string GetTagValue(SerializedProperty tagProperty)
        {
            SerializedProperty valueProperty = FindTagValueProperty(tagProperty);
            return valueProperty?.stringValue ?? string.Empty;
        }

        /// <summary>
        /// tag 직렬화 속성에서 value 값을 설정합니다.
        /// </summary>
        internal static bool SetTagValue(SerializedProperty tagProperty, string newValue)
        {
            SerializedProperty valueProperty = FindTagValueProperty(tagProperty);

            if (valueProperty == null)
            {
                return false;
            }

            valueProperty.stringValue = newValue ?? string.Empty;

            return true;
        }

        /// <summary>
        /// 대상 오브젝트들의 단일 태그 필드에 값을 반영합니다.
        /// </summary>
        internal static void SetTagValue(UnityEngine.Object[] targets, string propertyPath, string tagValue)
        {
            foreach (UnityEngine.Object target in targets)
            {
                if (target == null)
                {
                    continue;
                }

                using var serializedObject = new SerializedObject(target);

                serializedObject.Update();

                SerializedProperty tagProperty = serializedObject.FindProperty(propertyPath);

                bool changed = SetTagValue(tagProperty, tagValue);

                if (changed)
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        /// <summary>
        /// 태그 컨테이너 직렬화 속성에서 모든 태그를 읽어 HierarchyTag 리스트로 반환합니다.
        /// </summary>
        internal static List<HierarchyTag> ReadContainerTags(SerializedProperty tagsProperty)
        {
            var result = new List<HierarchyTag>();

            if (tagsProperty == null
                || !tagsProperty.isArray)
            {
                return result;
            }

            for (int i = 0; i < tagsProperty.arraySize; i++)
            {
                SerializedProperty tagProperty = tagsProperty.GetArrayElementAtIndex(i);

                string tagValue = GetTagValue(tagProperty);

                if (!HierarchyTag.TryCreate(tagValue, out HierarchyTag tag))
                {
                    continue;
                }

                result.Add(tag);
            }

            return HierarchyTagContainer.NormalizeTags(result);
        }

        /// <summary>
        /// 태그 컨테이너 직렬화 속성에 HierarchyTag 리스트를 작성합니다.
        /// </summary>
        internal static void WriteContainerTags(SerializedProperty tagsProperty, IReadOnlyList<HierarchyTag> tags)
        {
            if (tagsProperty == null
                || !tagsProperty.isArray)
            {
                return;
            }

            tagsProperty.arraySize = tags.Count;

            for (int index = 0; index < tags.Count; index++)
            {
                SerializedProperty tagProperty = tagsProperty.GetArrayElementAtIndex(index);

                SetTagValue(tagProperty, tags[index].Value);
            }
        }

        /// <summary>
        /// 대상 오브젝트(들)의 태그 컨테이너 필드에 HierarchyTag를 직접 추가하거나 제거하고,
        /// SerializedObject를 통해 변경사항을 즉시 적용합니다(Undo 등록 포함).
        /// 태그 체크박스가 호출할 핵심 메서드입니다.
        /// </summary>
        /// <example>
        /// 체크 On -> selected = true, 체크 Off -> selected = false
        /// </example>
        internal static void SetContainerTag(UnityEngine.Object[] targetObjects, string containerPropertyPath, HierarchyTag tag, bool selected)
        {
            if (targetObjects == null)
            {
                throw new ArgumentNullException(nameof(targetObjects));
            }

            if (string.IsNullOrEmpty(containerPropertyPath))
            {
                throw new ArgumentException("Container Property의 경로가 필요합니다.", nameof(containerPropertyPath));
            }

            if (!tag.IsValid)
            {
                throw new ArgumentException("유효하지 않은 HierarchyTag입니다.", nameof(tag));
            }

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
                SerializedProperty tagsProperty = FindContainerTagsProperty(containerProperty);

                if (containerProperty == null)
                {
                    Debug.LogWarning($"[{nameof(HierarchyTagSerializedPropertyUtility)}] {targetObject.name}에서 '{containerPropertyPath}' 속성을 찾을 수 없습니다.");
                    continue;
                }
                
                if (tagsProperty == null)
                {
                    Debug.LogWarning($"[{nameof(HierarchyTagSerializedPropertyUtility)}] {targetObject.name}에서 '{containerPropertyPath}.tags' 속성을 찾을 수 없습니다.");
                    continue;
                }

                var container = new HierarchyTagContainer(ReadContainerTags(tagsProperty));
                bool changed;
                if (selected)
                {
                    changed = container.Add(tag);
                }
                else
                {
                    changed = container.Remove(tag);
                }

                if (changed)
                {
                    WriteContainerTags(tagsProperty, container.Tags);
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        /// <summary>
        /// 대상 오브젝트(들)의 태그 컨테이너 필드에서 모든 태그를 직접 제거하고,
        /// SerializedObject를 통해 변경사항을 즉시 적용합니다.
        /// </summary>
        internal static void ClearContainer(UnityEngine.Object[] targetObjects, string containerPropertyPath)
        {
            if (targetObjects == null)
            {
                throw new ArgumentNullException(nameof(targetObjects));
            }

            for (int index = 0; index < targetObjects.Length; index++)
            {
                UnityEngine.Object target = targetObjects[index];

                if (target == null)
                {
                    continue;
                }

                using var serializedObject = new SerializedObject(target);

                serializedObject.Update();

                SerializedProperty containerProperty = serializedObject.FindProperty(containerPropertyPath);

                SerializedProperty tagsProperty = FindContainerTagsProperty(containerProperty);

                if (tagsProperty == null)
                {
                    continue;
                }

                tagsProperty.arraySize = 0;

                serializedObject.ApplyModifiedProperties();
            }
        }

    }
}
