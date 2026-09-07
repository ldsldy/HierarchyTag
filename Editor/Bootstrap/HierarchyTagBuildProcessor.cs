using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Deukyeonglee.HierarchyTags.Editor.Bootstrap
{
    internal sealed class HierarchyTagBuildProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (EditorApplication.isCompiling ||
                !HierarchyTagEditorBootstrap.EnsureCatalogAndRedirectsReady(out _))
            {
                throw new BuildFailedException(
                    "Redirect 조회 코드의 컴파일이 필요합니다. " +
                    "컴파일 완료 후 다시 빌드하세요.");
            }
        }
    }
}
