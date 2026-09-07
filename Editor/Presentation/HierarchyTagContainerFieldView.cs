using System;
using System.Collections.Generic;
using Deukyeonglee.HierarchyTags.Contracts;
using UnityEditor;
using UnityEngine;

namespace Deukyeonglee.HierarchyTags.Editor.Presentation
{
    /// <summary>
    /// 컨테이너의 현재 선택을 표시하고 선택 팝업을 엽니다.
    /// </summary>
    internal static class HierarchyTagContainerFieldView
    {
        public static void Draw(
            Rect position,
            SerializedProperty property,
            GUIContent label,
            Func<ITagCatalog> readCatalog,
            string status)
        {
            EditorGUI.BeginProperty(position, label, property);

            try
            {
                SerializedProperty tagsProperty =
                    HierarchyTagSerializedPropertyUtility.FindContainerTagsProperty(property);

                if (tagsProperty == null)
                {
                    EditorGUI.LabelField(
                        position,
                        label.text,
                        "컨테이너 직렬화 값을 찾을 수 없습니다.");

                    return;
                }

                string display = BuildDisplayValue(property, tagsProperty);

                Rect buttonPosition = EditorGUI.PrefixLabel(position, label);

                bool canOpen = readCatalog() != null;

                using (new EditorGUI.DisabledScope(!canOpen))
                {
                    if (EditorGUI.DropdownButton(
                        buttonPosition,
                        new GUIContent(display, status),
                        FocusType.Keyboard))
                    {
                        var popup = new HierarchyTagContainerPopup(
                            property.serializedObject.targetObjects,
                            property.propertyPath,
                            readCatalog);

                        PopupWindow.Show(buttonPosition, popup);
                    }
                }
            }
            finally
            {
                EditorGUI.EndProperty();
            }
        }

        private static string BuildDisplayValue(SerializedProperty containerProperty, SerializedProperty tagsProperty)
        {
            if (containerProperty.hasMultipleDifferentValues)
            {
                return "Mixed Values";
            }

            List<HierarchyTag> tags = HierarchyTagSerializedPropertyUtility.ReadContainerTags(tagsProperty);

            if (tags.Count == 0)
            {
                return "None";
            }

            if (tags.Count == 1)
            {
                return tags[0].Value;
            }

            return $"{tags.Count} Tags";
        }
    }
}