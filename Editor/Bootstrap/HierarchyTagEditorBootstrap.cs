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

            if (service == null ||
                !service.IsInitialized ||
                !HierarchyTagsManager.IsReady)
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

            HierarchyTagsManager.ClearCatalog();

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
                EnsureCatalogReady(out ITagCatalog catalog);

                // 갱신이 성공한 사전을 플레이어 데이터에도 반영합니다.
                HierarchyTagCatalogAutoPublisher.Request();

                if (shouldPrint)
                {
                    HierarchyTagCatalogCommands.Print(catalog);
                }
            }
            catch (Exception exception)
            {
                HierarchyTagsManager.ClearCatalog();

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

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                try
                {
                    if (EditorApplication.isCompiling ||
                         EditorApplication.isUpdating)
                    {
                        EditorApplication.isPlaying = false;

                        Debug.LogWarning(
                            "Unity의 컴파일 또는 에셋 임포트가 진행 중입니다. " +
                            "완료 후 Play를 다시 실행하세요.");

                        return;
                    }

                    EnsureCatalogReady(out _);
                }
                catch (Exception exception)
                {
                    HierarchyTagsManager.ClearCatalog();

                    errorMessage =
                        "Play 시작 전 태그 사전 준비에 실패했습니다. " + exception.Message;

                    EditorApplication.isPlaying = false;
                    Debug.LogException(exception);
                }

                return;
            }

            if (state == PlayModeStateChange.EnteredEditMode)
            {
                ScheduleRefresh();
            }
        }

        private static void Shutdown()
        {
            HierarchyTagsManager.ClearCatalog();

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

        internal static void EnsureCatalogReady(out ITagCatalog catalog)
        {
            catalog = null;

            try
            {
                EnsureService();

                IReadOnlyList<TagRedirect> redirects = settings.ReadRedirects();

                ITagCatalog nextCatalog = service.Refresh(redirects);

                HierarchyTagsManager.InstallCatalog(nextCatalog);

                catalog = nextCatalog;
                errorMessage = string.Empty;

            }
            catch
            {
                HierarchyTagsManager.ClearCatalog();
                throw;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void InitializeForEditorPlay()
        {
            // Domain Reload 이후에도 새 도메인에서 사전을 설치합니다.
            // Domain Reload를 꺼도 각 Play 시작 시 다시 구성합니다.
            EditorApplication.delayCall -= RefreshWhenReady;

            try
            {
                EnsureCatalogReady(out _);
            }
            catch (Exception exception)
            {
                HierarchyTagsManager.ClearCatalog();

                errorMessage =
                    "Editor Play의 태그 사전 초기화에 실패했습니다. " +
                    exception.Message;

                EditorApplication.isPlaying = false;
                Debug.LogException(exception);
            }
            finally
            {
                refreshPending = false;
            }
        }

        [MenuItem("Tools/Hierarchy Tags/Diagnostics/Check Catalog Data")]
        private static void CheckCatalogData()
        {
            if (!TryGetCatalog(out ITagCatalog original, out string message))
            {
                Debug.LogWarning(message);
                return;
            }

            try
            {
                IReadOnlyList<TagRedirect> redirects = settings.ReadRedirects();

                HierarchyTagCatalogData exported = HierarchyTagCatalogDataConverter.Export(original, redirects);

                // 실제 직렬화·역직렬화도 검증합니다.
                string json = JsonUtility.ToJson(exported);

                HierarchyTagCatalogData decoded = JsonUtility.FromJson<HierarchyTagCatalogData>(json);

                TagCatalog restored = HierarchyTagCatalogDataConverter.Import(decoded);

                if (original.Tags.Count != restored.Tags.Count)
                {
                    throw new InvalidOperationException("복원 전후 태그 수가 다릅니다.");
                }

                foreach (TagInfo before in original.Tags)
                {
                    if (!restored.TryGetInfo(before.Tag, out TagInfo after))
                    {
                        throw new InvalidOperationException($"복원 후 태그가 없습니다: {before.Tag.Value}");
                    }

                    if (!string.Equals(before.Tag.Value, after.Tag.Value, StringComparison.Ordinal) ||
                        before.Parent != after.Parent ||
                        before.Children.Count != after.Children.Count ||
                        before.Registrations.Count != after.Registrations.Count)
                    {
                        throw new InvalidOperationException($"태그 정보가 달라졌습니다: {before.Tag.Value}");
                    }

                    for (int i = 0; i < before.Children.Count; i++)
                    {
                        if (before.Children[i] != after.Children[i])
                        {
                            throw new InvalidOperationException($"자식 목록이 달라졌습니다: {before.Tag.Value}");
                        }
                    }

                    for (int i = 0; i < before.Registrations.Count; i++)
                    {
                        TagRegistration left = before.Registrations[i];
                        TagRegistration right = after.Registrations[i];

                        if (left.Tag != right.Tag ||
                            left.Source != right.Source ||
                            !string.Equals(left.Comment, right.Comment, StringComparison.Ordinal))
                        {
                            throw new InvalidOperationException($"등록 정보가 달라졌습니다: {before.Tag.Value}");
                        }
                    }
                }

                foreach (TagRedirect redirect in redirects)
                {
                    if (!original.TryResolve(redirect.OldTag.Value, out TagId expected) ||
                        !restored.TryResolve(redirect.OldTag.Value, out TagId actual) ||
                        !string.Equals(expected.Value, actual.Value, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException($"Redirect 결과가 달라졌습니다: " + redirect.OldTag.Value);
                    }
                }

                Debug.Log(
                    $"Catalog 데이터 왕복 검증 완료. " +
                    $"태그: {restored.Tags.Count}, " +
                    $"등록: {decoded.registrations.Length}, " +
                    $"Redirect: {decoded.redirects.Length}");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [MenuItem("Tools/Hierarchy Tags/Diagnostics/Check Manager")]
        private static void CheckManager()
        {
            if (!TryGetCatalog(out ITagCatalog catalog, out string message))
            {
                Debug.LogWarning(message);
                return;
            }

            foreach (TagInfo info in catalog.Tags)
            {
                if (!HierarchyTagsManager.TryGetTag(info.Tag.Value, out HierarchyTag tag) ||
                    !string.Equals(tag.Value, info.Tag.Value, StringComparison.Ordinal))
                {
                    Debug.LogError($"중앙 관리자 조회 실패: {info.Tag.Value}");

                    return;
                }
            }

            foreach (TagRedirect redirect in settings.ReadRedirects())
            {
                if (!catalog.TryResolve(redirect.OldTag.Value, out TagId expected) ||
                    !HierarchyTagsManager.TryGetTag(redirect.OldTag.Value, out HierarchyTag actual) ||
                    !string.Equals(actual.Value, expected.Value, StringComparison.Ordinal))
                {
                    Debug.LogError($"중앙 관리자 Redirect 조회 실패: " + redirect.OldTag.Value);
                    return;
                }
            }

            Debug.Log($"HierarchyTags 중앙 관리자 확인 완료. " + $"태그 수: {catalog.Tags.Count}");
        }

        [MenuItem("Tools/Hierarchy Tags/Retry Player Data Generation")]
        private static void RetryPlayerDataGeneration()
        {
            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode ||
                BuildPipeline.isBuildingPlayer)
            {
                Debug.LogWarning(
                    "Play와 빌드가 종료되고 " +
                    "컴파일·임포트가 끝난 뒤 재시도하세요.");
                return;
            }

            ScheduleRefresh();
            HierarchyTagCatalogAutoPublisher.Retry();

            Debug.Log("HierarchyTags 데이터 자동 준비를 다시 예약했습니다.");
        }

        [MenuItem("Tools/Hierarchy Tags/Diagnostics/Check Deferred Tags")]
        private static void CheckDeferredTags()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                BuildPipeline.isBuildingPlayer)
            {
                Debug.LogWarning("Editor가 대기 상태일 때 실행하세요.");
                return;
            }

            if (!TryGetCatalog(out ITagCatalog original, out string message))
            {
                Debug.LogWarning(message);
                return;
            }

            try
            {
                var source = new TagSourceId(TagSourceKind.Config, "HierarchyTags.Diagnostics");

                TagCatalog CreateCatalog(string target)
                {
                    return TagCatalogBuilder.Build(
                        new[]
                        {
                            new TagRegistration(TagId.Parse(target), source)
                        },
                        new[]
                        {
                            new TagRedirect(
                                TagId.Parse("Test.Old"),
                                TagId.Parse(target))
                        });
                }

                HierarchyTagsManager.ClearCatalog();

                var tag = new HierarchyTag("Test.Old");
                tag.OnAfterDeserialize();

                var container = new HierarchyTagContainer(new[]
                {
                    new HierarchyTag("Test.Old"),
                    new HierarchyTag("Test.New")
                });

                // 사전이 없어도 역직렬화 콜백 자체는 성공해야 합니다.
                container.OnAfterDeserialize();

                bool rejectedEarlyRead = false;

                try
                {
                    _ = tag.Value;
                }
                catch (InvalidOperationException)
                {
                    rejectedEarlyRead = true;
                }

                if (!rejectedEarlyRead)
                {
                    throw new InvalidOperationException("초기화 전 값 사용이 거부되지 않았습니다.");
                }

                HierarchyTagsManager.InstallCatalog(CreateCatalog("Test.New"));

                if (tag.Value != "Test.New" ||
                    container.Count != 1 ||
                    container.Tags[0].Value != "Test.New")
                {
                    throw new InvalidOperationException("지연 Redirect 또는 컨테이너 중복 제거 실패");
                }

                var dictionary = new Dictionary<HierarchyTag, string>
                {
                    [tag] = "saved"
                };

                int hashBefore = tag.GetHashCode();

                // 사전 변경 후에도 이미 사용한 값은 고정되어야 합니다.
                HierarchyTagsManager.InstallCatalog(CreateCatalog("Test.Other"));

                if (tag.Value != "Test.New" ||
                    tag.GetHashCode() != hashBefore ||
                    !dictionary.ContainsKey(tag) ||
                    !dictionary.ContainsKey(new HierarchyTag("Test.New")))
                {
                    throw new InvalidOperationException("사전 변경으로 기존 태그 값 또는 해시가 변했습니다.");
                }

                var fresh = new HierarchyTag("Test.Old");
                fresh.OnAfterDeserialize();

                if (fresh.Value != "Test.Other")
                {
                    throw new InvalidOperationException("새로 역직렬화한 태그에 새 사전이 적용되지 않았습니다.");
                }

                Debug.Log("지연 해석·컨테이너 정규화·해시 안정성 검증 완료.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                HierarchyTagsManager.InstallCatalog(original);
            }
        }
    }
}
