using System;
using System.Collections.Generic;
using Deukyeonglee.HierarchyTags.Contracts;

namespace Deukyeonglee.HierarchyTags.Application
{
    /// <summary>
    /// 부모 계층과 등록 출처를 포함하는 읽기 전용 태그 사전입니다.
    /// </summary>
    public sealed class TagCatalog : ITagCatalog
    {
        private readonly Dictionary<TagId, TagInfo> tagsById;
        private readonly Dictionary<TagId, TagId> redirectTargets;

        public IReadOnlyList<TagInfo> Tags { get; }

        internal TagCatalog(IEnumerable<TagInfo> tagInfos, IEnumerable<TagRedirect> redirects)
        {
            if (tagInfos == null)
            {
                throw new ArgumentNullException(nameof(tagInfos));
            }

            var sortedTags = new List<TagInfo>(tagInfos);

            sortedTags.Sort((left, right) => left.Tag.CompareTo(right.Tag));

            tagsById = new Dictionary<TagId, TagInfo>();

            foreach (TagInfo info in sortedTags)
            {
                tagsById.Add(info.Tag, info);
            }

            Tags = sortedTags.AsReadOnly();

            redirectTargets = TagRedirectMapBuilder.Build(redirects, tagsById);
        }

        public bool Contains(TagId tag)
        {
            return !tag.IsNone && tagsById.ContainsKey(tag);
        }

        public bool TryGetInfo(TagId tag, out TagInfo info)
        {
            if (tag.IsNone)
            {
                info = null;
                return false;
            }

            return tagsById.TryGetValue(tag, out info);
        }

        public bool TryResolve(string value, out TagId tag)
        {
            tag = TagId.None;

            if (!TagId.TryParse(value, out TagId requestedTag))
            {
                return false;
            }

            if (redirectTargets.TryGetValue(requestedTag, out TagId redirectedTag))
            {
                requestedTag = redirectedTag;
            }

            if (!tagsById.TryGetValue(requestedTag, out TagInfo info))
            {
                return false;
            }

            tag = info.Tag;
            return true;
        }
    }
}