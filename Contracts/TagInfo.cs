using System;
using System.Collections.Generic;

namespace HierarchyTags.Contracts
{
    /// <summary>
    /// 태그 사전이 외부에 제공하는 읽기 전용 정보입니다.
    /// </summary>
    public sealed class TagInfo
    {
        public TagId Tag { get; }

        public TagId Parent { get; }

        public IReadOnlyList<TagId> Children { get; }

        public IReadOnlyList<TagRegistration> Registrations { get; }

        public bool IsExplicit => Registrations.Count > 0;

        public TagInfo(TagId tag, TagId parent, IEnumerable<TagId> children, IEnumerable<TagRegistration> registrations)
        {
            if (tag.IsNone)
            {
                throw new ArgumentException("빈 태그의 정보를 만들 수 없습니다.", nameof(tag));
            }

            if (children == null)
            {
                throw new ArgumentNullException(nameof(children));
            }

            if (registrations == null)
            {
                throw new ArgumentNullException(nameof(registrations));
            }

            Tag = tag;
            Parent = parent;

            // 호출자가 원본 목록을 변경해도 영향을 받지 않습니다.
            Children = new List<TagId>(children).AsReadOnly();
            Registrations = new List<TagRegistration>(registrations).AsReadOnly();
        }
    }
}
