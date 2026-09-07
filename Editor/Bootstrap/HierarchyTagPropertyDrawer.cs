using HierarchyTags.Contracts;
using HierarchyTags.Editor.Presentation;
using UnityEditor;
using UnityEngine;

namespace HierarchyTags.Editor.Bootstrap
{
    [CustomPropertyDrawer(typeof(HierarchyTag))]
    internal sealed class HierarchyTagPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            HierarchyTagEditorBootstrap.TryGetCatalog(out ITagCatalog catalog, out string message);

            HierarchyTagFieldView.Draw(
                position,
                property,
                label,
                catalog,
                message);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
