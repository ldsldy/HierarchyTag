using System;
using Deukyeonglee.HierarchyTags.Contracts;
using UnityEditor;
using UnityEngine;

namespace Deukyeonglee.HierarchyTags.Editor.Presentation
{
    internal static class HierarchyTagFieldView
    {
        public static void Draw(
            Rect position,
            SerializedProperty property,
            GUIContent label,
            ITagCatalog catalog,
            string statusMessage)
        {
            EditorGUI.BeginProperty(position, label, property);

            bool previousMixedValue = EditorGUI.showMixedValue;

            try
            {
                SerializedProperty valueProperty = HierarchyTagSerializedPropertyUtility.FindTagValueProperty(property);

                if (valueProperty == null)
                {
                    EditorGUI.LabelField(position, label.text, "HierarchyTag 직렬화 값을 찾을 수 없습니다.");

                    return;
                }

                Rect buttonPosition = EditorGUI.PrefixLabel(position, label);

                string currentValue = valueProperty.stringValue;
                bool hasMixedValues = valueProperty.hasMultipleDifferentValues;

                string displayValue = hasMixedValues
                    ? "Mixed Values" : string.IsNullOrEmpty(currentValue)
                        ? "None" : currentValue;

                EditorGUI.showMixedValue = hasMixedValues;

                var content = new GUIContent(displayValue, statusMessage);

                using (new EditorGUI.DisabledScope(catalog == null))
                {
                    if (EditorGUI.DropdownButton(buttonPosition, content, FocusType.Keyboard))
                    {
                        ShowMenu(property, currentValue, hasMixedValues, catalog);
                    }
                }
            }
            finally
            {
                EditorGUI.showMixedValue = previousMixedValue;
                EditorGUI.EndProperty();
            }
        }

        private static void ShowMenu(SerializedProperty property, string currentValue, bool hasMixedValues, ITagCatalog catalog)
        {
            UnityEngine.Object[] targets = property.serializedObject.targetObjects;

            string propertyPath = property.propertyPath;

            var menu = new GenericMenu();

            AddItem(
                menu,
                targets,
                propertyPath,
                "선택 해제 (None)",
                string.Empty,
                !hasMixedValues &&
                string.IsNullOrEmpty(currentValue));

            menu.AddSeparator(string.Empty);

            foreach (TagInfo info in catalog.Tags)
            {
                string tagValue = info.Tag.Value;
                string menuPath = tagValue.Replace('.', '/');

                // 부모 태그도 선택할 수 있도록
                // 하위 메뉴 안에 자신의 선택 항목을 둡니다.
                if (info.Children.Count > 0)
                {
                    menuPath += "/(이 태그 선택)";
                }

                bool selected =
                    !hasMixedValues &&
                    string.Equals(currentValue, tagValue, StringComparison.OrdinalIgnoreCase);

                AddItem(
                    menu,
                    targets,
                    propertyPath,
                    menuPath,
                    tagValue,
                    selected);
            }

            menu.ShowAsContext();
        }

        private static void AddItem(
            GenericMenu menu,
            UnityEngine.Object[] targets,
            string propertyPath,
            string menuPath,
            string tagValue,
            bool selected)
        {
            menu.AddItem(
                new GUIContent(menuPath),
                selected,
                () => HierarchyTagSerializedPropertyUtility.SetTagValue(
                    targets,
                    propertyPath,
                    tagValue));
        }
    }
}