using HierarchyTags.Contracts;
using HierarchyTags.Editor.Presentation;
using UnityEditor;
using UnityEngine;

namespace HierarchyTags.Editor.Bootstrap
{
    [CustomPropertyDrawer(typeof(HierarchyTagContainer))]
    internal sealed class HierarchyTagContainerPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            HierarchyTagEditorBootstrap.TryGetCatalog(out _, out string status);

            HierarchyTagContainerFieldView.Draw(
                position,
                property,
                label,
                ReadCatalog,
                status);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        private static ITagCatalog ReadCatalog()
        {
            HierarchyTagEditorBootstrap.TryGetCatalog(out ITagCatalog catalog, out _);

            return catalog;
        }
    }
}
