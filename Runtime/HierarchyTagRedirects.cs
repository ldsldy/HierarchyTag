using System;
using System.Collections.Generic;

namespace Deukyeonglee.HierarchyTags
{
    // 프로젝트 생성 파일을 .asmref로 같은 어셈블리에 포함합니다.
    // 생성 전에도 컴파일할 수 있으며, 역직렬화에서 Unity API 없이 조회합니다.
    internal static partial class HierarchyTagRedirects
    {
        private static readonly Dictionary<string, string> targets;

        internal static string Revision { get; }

        static HierarchyTagRedirects()
        {
            targets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string revision = string.Empty;
            PopulateTargets(targets, ref revision);
            Revision = revision;
        }

        static partial void PopulateTargets(Dictionary<string, string> mappings, ref string revision);

        internal static string Resolve(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return targets.TryGetValue(value, out string target) ? target : value;
        }
    }
}
