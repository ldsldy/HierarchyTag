using System;
using HierarchyTags.Application;
using HierarchyTags.Contracts;
using HierarchyTags.Editor.Bootstrap;
using UnityEditor;
using UnityEngine;

namespace HierarchyTags.Editor.Infrastructure
{
    /// <summary>
    /// Editor가 준비된 후 프로젝트 사전 데이터를 자동 생성합니다.
    /// 패키지 최초 등록이 끝나면 임포트까지 다시 확인합니다.
    /// </summary>
    [InitializeOnLoad]
    internal static class HierarchyTagCatalogAutoPublisher
    {
        private const string ResolvePendingKey = "HierarchyTags.CatalogPackageResolvePending";

        private const double TimeoutSeconds = 120.0;

        private static bool pending;
        private static double deadline;

        static HierarchyTagCatalogAutoPublisher()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Stop;
            EditorApplication.quitting += Stop;

            Request();
        }

        internal static void Request()
        {
            deadline = EditorApplication.timeSinceStartup + TimeoutSeconds;

            if (pending)
            {
                return;
            }

            pending = true;

            EditorApplication.update -= Update;
            EditorApplication.update += Update;
        }

        private static void Update()
        {
            // 컴파일, 임포트, Play, 빌드 중에는 파일을 갱신하지 않습니다.
            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode ||
                BuildPipeline.isBuildingPlayer)
            {
                deadline = EditorApplication.timeSinceStartup + TimeoutSeconds;
                return;
            }

            if (EditorApplication.timeSinceStartup > deadline)
            {
                Stop();

                Debug.LogError("HierarchyTags 데이터 자동 준비가 시간 내에 " +
                    "완료되지 않았습니다. 태그 사전 오류와 " +
                    "Package Manager 상태를 확인하세요.");

                return;
            }

            // 사전 수집은 기존 Bootstrap이 담당합니다.
            if (!HierarchyTagEditorBootstrap.TryGetCatalog(out ITagCatalog catalog, out _))
            {
                return;
            }

            try
            {
                HierarchyTagCatalogData data =
                    HierarchyTagCatalogDataConverter.Export(catalog, HierarchyTagSettings.instance.ReadRedirects());

                if (HierarchyTagCatalogPackageWriter.Write(data))
                {
                    SessionState.EraseBool(ResolvePendingKey);
                    Stop();
                    return;
                }

                // 최초 등록 요청은 한 번만 보냅니다.
                // 도메인 리로드가 발생해도 요청 여부를 유지합니다.
                if (!SessionState.GetBool(ResolvePendingKey, false))
                {
                    SessionState.SetBool(ResolvePendingKey, true);

                    try
                    {
                        UnityEditor.PackageManager.Client.Resolve();
                    }
                    catch
                    {
                        SessionState.EraseBool(ResolvePendingKey);
                        throw;
                    }
                }

                // 등록 완료까지 다음 Editor update에서 재확인합니다.
            }
            catch (Exception exception)
            {
                Stop();

                Debug.LogError("HierarchyTags 데이터 자동 준비에 실패했습니다.");
                Debug.LogException(exception);
            }
        }

        // Package Manager 오류를 해결한 뒤 수동 재시도할 때 사용합니다.
        internal static void Retry()
        {
            SessionState.EraseBool(ResolvePendingKey);
            Request();
        }

        private static void Stop()
        {
            pending = false;
            EditorApplication.update -= Update;
        }
    }
}