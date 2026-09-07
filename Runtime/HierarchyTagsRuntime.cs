using System;
using HierarchyTags.Application;
using HierarchyTags.Contracts;
using UnityEngine;

namespace HierarchyTags
{
    internal static class HierarchyTagsRuntime
    {
        internal const string ResourcePath = "HierarchyTagsInternal/Catalog";

#if !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlayer()
        {
            HierarchyTagsManager.ClearCatalog();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void InitializeForPlayer()
        {
            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);

            if (asset == null)
            {
                throw new InvalidOperationException("HierarchyTags 사전 데이터가 빌드에 없습니다.");
            }

            try
            {
                HierarchyTagCatalogData data = JsonUtility.FromJson<HierarchyTagCatalogData>(asset.text);

                InstallData(data);
            }
            catch
            {
                HierarchyTagsManager.ClearCatalog();
                throw;
            }
            finally
            {
                Resources.UnloadAsset(asset);
            }
        }
#endif

        internal static void InstallData(HierarchyTagCatalogData data)
        {
            TagCatalog catalog = HierarchyTagCatalogDataConverter.Import(data);

            HierarchyTagsManager.InstallCatalog(catalog);
        }
    }
}