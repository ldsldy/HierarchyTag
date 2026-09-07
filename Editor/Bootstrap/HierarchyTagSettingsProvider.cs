using HierarchyTags.Application;
using HierarchyTags.Contracts;
using HierarchyTags.Editor.Presentation;
using System.Collections.Generic;
using UnityEditor;

namespace HierarchyTags.Editor.Bootstrap
{
    /// <summary>
    /// Project Settings에 태그 설정 화면을 등록하고,
    /// 필요한 서비스를 View에 전달합니다.
    /// </summary>
    internal sealed class HierarchyTagSettingsProvider : SettingsProvider
    {
        private readonly HierarchyTagSettingsView view = new();

        private HierarchyTagSettingsProvider() : base("Project/Hierarchy Tags", SettingsScope.Project) // SettingsProvider(부모) 생성자 호출
        {
            keywords = new HashSet<string> { "Unity", "Tag", "Tags", "HierarchyTag" };
        }

        /// <summary>
        /// HierarchyTag 프로젝트 설정 화면을 등록합니다.
        /// </summary>
        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new HierarchyTagSettingsProvider();
        }

        public override void OnGUI(string searchContext)
        {
            if (!HierarchyTagEditorBootstrap.TryGetManualTagService(out ManualTagService manualTags))
            {
                EditorGUILayout.HelpBox(
                    "태그 편집기를 초기화하고 있습니다.",
                    MessageType.Info);

                return;
            }

            HierarchyTagEditorBootstrap.TryGetCatalog(out ITagCatalog catalog, out string status);

            view.Draw(
                manualTags,
                catalog,
                status,
                HierarchyTagEditorBootstrap.ScheduleRefresh);
        }
    }
}
