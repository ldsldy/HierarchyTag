using System;

namespace Deukyeonglee.HierarchyTags.Contracts
{
    /// <summary>
    /// 특정 출처가 명시적으로 제공하는 태그 정의입니다.
    /// </summary>
    public sealed class TagRegistration
    {
        public TagId Tag { get; }

        public TagSourceId Source { get; }

        public string Comment { get; }

        public TagRegistration(TagId tag, TagSourceId source, string comment = "")
        {
            if (tag.IsNone)
            {
                throw new ArgumentException(
                    "빈 태그는 등록할 수 없습니다.",
                    nameof(tag));
            }

            if (source.IsNone)
            {
                throw new ArgumentException(
                    "유효한 출처 식별자가 필요합니다.",
                    nameof(source));
            }

            Tag = tag;
            Source = source;
            Comment = comment ?? string.Empty;
        }
    }
}