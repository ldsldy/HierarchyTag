using System;

namespace Deukyeonglee.HierarchyTags.Contracts
{
    /// <summary>
    /// 이전 태그 이름을 대체할 새 태그 이름입니다.
    /// </summary>
    public sealed class TagRedirect
    {
        public TagId OldTag { get; }

        public TagId NewTag { get; }

        public TagRedirect(TagId oldTag, TagId newTag)
        {
            if (oldTag.IsNone)
            {
                throw new ArgumentException(
                    "이전 태그 이름이 필요합니다.",
                    nameof(oldTag));
            }

            if (newTag.IsNone)
            {
                throw new ArgumentException(
                    "새 태그 이름이 필요합니다.",
                    nameof(newTag));
            }

            if (oldTag == newTag)
            {
                throw new ArgumentException(
                    "같은 태그로 Redirect할 수 없습니다.",
                    nameof(newTag));
            }

            OldTag = oldTag;
            NewTag = newTag;
        }
    }
}