using System;
using HierarchyTags.Application;
using HierarchyTags.Editor.Infrastructure;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace HierarchyTags.Editor.Bootstrap
{
    internal sealed class HierarchyTagBuildProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            try
            {
                HierarchyTagEditorBootstrap.EnsureCatalogReady(out var catalog);

                var data =
                    HierarchyTagCatalogDataConverter.Export(catalog, HierarchyTagSettings.instance.ReadRedirects());

                if (!HierarchyTagCatalogPackageWriter.Write(data))
                {
                    throw new BuildFailedException(
                        "HierarchyTags 데이터 패키지의 자동 등록이 " +
                        "아직 완료되지 않았습니다. " +
                        "Editor의 패키지 처리가 완료된 후 다시 빌드하세요.");
                }
            }
            catch (BuildFailedException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new BuildFailedException(
                    "HierarchyTags 플레이어 데이터 준비 실패: " +
                    exception.Message);
            }
        }
    }
}
