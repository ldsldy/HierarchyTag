using System;
using System.Collections.Generic;
using HierarchyTags.Contracts;

namespace HierarchyTags.Application
{
    /// <summary>
    /// Redirect를 검증하고 이전 이름별 최종 대상을 구성합니다.
    /// </summary>
    internal static class TagRedirectMapBuilder
    {
        internal static Dictionary<TagId, TagId> Build(
            IEnumerable<TagRedirect> redirects,
            IReadOnlyDictionary<TagId, TagInfo> registeredTags)
        {
            if (redirects == null)
            {
                throw new ArgumentNullException(nameof(redirects));
            }

            if (registeredTags == null)
            {
                throw new ArgumentNullException(nameof(registeredTags));
            }

            var directTargets = new Dictionary<TagId, TagId>();

            foreach (TagRedirect redirect in redirects)
            {
                if (redirect == null)
                {
                    throw new ArgumentException("Redirect 목록에 null 항목이 있습니다.", nameof(redirects));
                }

                if (registeredTags.ContainsKey(redirect.OldTag))
                {
                    throw new InvalidOperationException($"Redirect의 이전 이름이 현재 사전에 존재합니다: " + redirect.OldTag.Value);
                }

                if (directTargets.TryGetValue(redirect.OldTag, out TagId existingTarget))
                {
                    if (existingTarget != redirect.NewTag)
                    {
                        throw new InvalidOperationException(
                            $"Redirect 대상이 충돌합니다: " +
                            $"{redirect.OldTag.Value} → " +
                            $"{existingTarget.Value}, " +
                            redirect.NewTag.Value);
                    }

                    continue;
                }

                directTargets.Add(redirect.OldTag, redirect.NewTag);
            }

            var resolvedTargets = new Dictionary<TagId, TagId>();

            var oldTags = new List<TagId>(directTargets.Keys);
            oldTags.Sort();

            foreach (TagId oldTag in oldTags)
            {
                var visited = new HashSet<TagId>();
                var path = new List<TagId>();

                TagId current = oldTag;

                while (directTargets.TryGetValue(current, out TagId next))
                {
                    path.Add(current);

                    if (!visited.Add(current))
                    {
                        throw new InvalidOperationException(
                            "Redirect 순환이 발견되었습니다: " + string.Join(" → ", path));
                    }

                    current = next;
                }

                if (!registeredTags.TryGetValue(
                    current,
                    out TagInfo targetInfo))
                {
                    throw new InvalidOperationException(
                        "Redirect의 최종 대상이 사전에 없습니다: " + $"{oldTag.Value} → {current.Value}");
                }

                // 현재 사전에 저장된 표기를 사용합니다.
                resolvedTargets.Add(oldTag, targetInfo.Tag);
            }

            return resolvedTargets;
        }
    }
}
