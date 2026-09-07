using System;
using System.IO;
using System.Text;
using HierarchyTags.Contracts;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace HierarchyTags.Editor.Infrastructure
{
    internal static class HierarchyTagCatalogPackageWriter
    {
        internal const string PackageDirectory =
            "Packages/com.deukyeonglee.hierarchytags.data";

        internal const string AssetPath =
            PackageDirectory +
            "/Resources/HierarchyTagsInternal/Catalog.json";

        private const string Manifest =
            "{\n" +
            "  \"name\": \"com.deukyeonglee.hierarchytags.data\",\n" +
            "  \"version\": \"1.0.0\",\n" +
            "  \"displayName\": \"HierarchyTags Project Data\",\n" +
            "  \"description\": \"Generated project tag catalog. Do not edit manually.\",\n" +
            "  \"hideInEditor\": true\n" +
            "}\n";

        internal static bool Write(HierarchyTagCatalogData data)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));

            string directory = Path.Combine(projectRoot, PackageDirectory);

            string manifestPath = Path.Combine(directory, "package.json");

            // 이 생성기가 관리하는 매니페스트인지 확인합니다.
            // 같은 이름의 다른 패키지를 덮어쓰지 않습니다.
            if (Directory.Exists(directory))
            {
                if (!File.Exists(manifestPath) ||
                    File.ReadAllText(manifestPath) != Manifest)
                {
                    throw new InvalidOperationException(
                        "같은 이름의 데이터 패키지가 있지만 이 생성기의 관리 대상과 일치하지 않습니다: " + PackageDirectory);
                }
            }
            else
            {
                Directory.CreateDirectory(directory);

                File.WriteAllText(manifestPath, Manifest, new UTF8Encoding(false));
            }

            string filePath = Path.Combine(projectRoot, AssetPath);
            string json = JsonUtility.ToJson(data, true);

            bool changed =
                !File.Exists(filePath) ||
                !string.Equals(File.ReadAllText(filePath), json, StringComparison.Ordinal);

            if (changed)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                File.WriteAllText(filePath, json, new UTF8Encoding(false));
            }

            // 최초 패키지 등록은 비동기입니다.
            // 아직 등록되지 않았다면 호출자에게 알립니다.
            if (UnityEditor.PackageManager.PackageInfo.FindForAssetPath(AssetPath) == null)
            {
                return false;
            }

            TextAsset imported =
                AssetDatabase.LoadAssetAtPath<TextAsset>(AssetPath);

            if (changed || imported == null || imported.text != json)
            {
                AssetDatabase.ImportAsset(
                    AssetPath,
                    ImportAssetOptions.ForceUpdate |
                    ImportAssetOptions.ForceSynchronousImport);

                imported = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetPath);
            }

            if (imported == null || imported.text != json)
            {
                throw new InvalidOperationException("HierarchyTags 사전 데이터의 임포트가 완료되지 않았습니다.");
            }

            return true;
        }
    }
}