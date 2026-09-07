using Deukyeonglee.HierarchyTags.Contracts;
using UnityEditor;
using UnityEngine;

namespace Deukyeonglee.HierarchyTags.Editor.Bootstrap
{
    /// <summary>
    /// 이 메뉴는 실행할 때마다 서비스를 생성하는 일회성 확인 도구입니다.
    /// </summary>
    internal static class HierarchyTagCatalogCommands
    {
        [MenuItem("Tools/Hierarchy Tags/Print Combined Catalog")]
        private static void PrintCombinedCatalog()
        {
            HierarchyTagEditorBootstrap.RefreshAndPrint();
        }

        internal static void Print(ITagCatalog catalog)
        {
            foreach (TagInfo info in catalog.Tags)
            {
                Debug.Log(
                    $"{info.Tag.Value} | " +
                    $"직접 등록: {info.IsExplicit} | " +
                    $"출처 수: {info.Registrations.Count}");

                foreach (TagRegistration registration
                         in info.Registrations)
                {
                    Debug.Log(
                        $"  {registration.Source.Kind}: " +
                        registration.Source.Value);
                }
            }

            Debug.Log(
                $"통합 사전의 태그 수: {catalog.Tags.Count}");
        }
    }
}