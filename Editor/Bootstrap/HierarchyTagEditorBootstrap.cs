using System;
using HierarchyTags.Application;
using HierarchyTags.Contracts;
using HierarchyTags.Editor.Infrastructure;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HierarchyTags.Editor.Bootstrap
{
    /// <summary>
    /// Editor 세션을 관리하는 Bootstrap
    /// </summary>
    [InitializeOnLoad]
    internal static class HierarchyTagEditorBootstrap
    {
        private static HierarchyTagSettings settings;
        private static TagCatalogService service;
        private static ManualTagService manualTags;
        private static bool refreshPending;
        private static bool printAfterRefresh;

        private static string errorMessage = string.Empty;

        static HierarchyTagEditorBootstrap()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            AssemblyReloadEvents.beforeAssemblyReload += Shutdown;
            EditorApplication.quitting += Shutdown;

            ScheduleRefresh();
        }

        internal static bool TryGetCatalog(out ITagCatalog catalog, out string message)
        {
            catalog = null;

            if (refreshPending)
            {
                message = "태그 사전을 갱신하고 있습니다.";
                return false;
            }

            if (!string.IsNullOrEmpty(errorMessage))
            {
                message = errorMessage;
                return false;
            }

            if (service == null || !service.IsInitialized)
            {
                message = "태그 사전이 초기화되지 않았습니다.";
                return false;
            }

            catalog = service.Current;
            message = string.Empty;
            return true;
        }

        [MenuItem("Tools/Hierarchy Tags/Refresh Catalog")]
        internal static void ScheduleRefresh()
        {
            refreshPending = true;

            EditorApplication.delayCall -= RefreshWhenReady;
            EditorApplication.delayCall += RefreshWhenReady;
        }

        internal static void RefreshAndPrint()
        {
            printAfterRefresh = true;
            ScheduleRefresh();
        }

        private static void RefreshWhenReady()
        {
            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating)
            {
                ScheduleRefresh();
                return;
            }

            bool shouldPrint = printAfterRefresh;
            printAfterRefresh = false;

            try
            {
                if (!EnsureCatalogAndRedirectsReady(out ITagCatalog catalog))
                {
                    errorMessage =
                        "Redirect 조회 코드를 갱신했습니다. " +
                        "Unity의 컴파일 완료를 기다리고 있습니다.";

                    return;
                }

                errorMessage = string.Empty;

                if (shouldPrint)
                {
                    HierarchyTagCatalogCommands.Print(catalog);
                }
            }
            catch (Exception exception)
            {
                errorMessage =
                    "태그 사전 갱신에 실패했습니다. " +
                    exception.Message;

                Debug.LogException(exception);
            }
            finally
            {
                refreshPending = false;
            }
        }

        private static void EnsureService()
        {
            if (service != null)
            {
                return;
            }

            settings = HierarchyTagSettings.instance;
            manualTags = new ManualTagService(settings);

            service = new TagCatalogService(
                new ITagRegistrationSource[]
                {
                    new CodeTagRegistrationSource(),
                    new SettingsTagRegistrationSource(settings)
                });

            settings.Changed -= ScheduleRefresh;
            settings.Changed += ScheduleRefresh;
        }

        private static void OnPlayModeStateChanged(
            PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                try
                {
                    if (EditorApplication.isCompiling ||
                        !EnsureCatalogAndRedirectsReady(out _))
                    {
                        EditorApplication.isPlaying = false;
                        Debug.LogWarning(
                            "Redirect 조회 코드의 컴파일이 필요합니다. " +
                            "컴파일 완료 후 Play를 다시 실행하세요.");
                    }
                }
                catch (Exception exception)
                {
                    EditorApplication.isPlaying = false;
                    Debug.LogException(exception);
                }

                return;
            }

            if (state == PlayModeStateChange.EnteredEditMode ||
                state == PlayModeStateChange.EnteredPlayMode)
            {
                ScheduleRefresh();
            }
        }

        private static void Shutdown()
        {
            EditorApplication.delayCall -= RefreshWhenReady;

            EditorApplication.playModeStateChanged -=
                OnPlayModeStateChanged;

            AssemblyReloadEvents.beforeAssemblyReload -= Shutdown;
            EditorApplication.quitting -= Shutdown;

            if (settings != null)
            {
                settings.Changed -= ScheduleRefresh;
            }
        }

        /// <summary>Bootstrap이 소유한 수동 편집 서비스를 반환합니다.</summary>
        internal static bool TryGetManualTagService(out ManualTagService result)
        {
            result = manualTags;
            return result != null;
        }

        internal static bool EnsureCatalogAndRedirectsReady(out ITagCatalog catalog)
        {
            EnsureService();

            IReadOnlyList<TagRedirect> redirects =
                settings.ReadRedirects();

            catalog = service.Refresh(redirects);

            string expectedRevision =
                HierarchyTagRedirectCodeGenerator.GenerateIfChanged(catalog, redirects);

            return string.Equals(
                HierarchyTagRedirects.Revision,
                expectedRevision,
                StringComparison.Ordinal);
        }
    }
}
