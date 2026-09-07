using System;
using System.Collections.Generic;
using HierarchyTags.Contracts;

namespace HierarchyTags.Application
{
    /// <summary>
    /// 출처별 등록을 병합하고 태그 계층을 구성합니다.
    /// </summary>
    public static class TagCatalogBuilder
    {
        public static TagCatalog Build(IEnumerable<TagRegistration> registrations)
        {
            return Build(registrations, Array.Empty<TagRedirect>());
        }

        public static TagCatalog Build(IEnumerable<TagRegistration> registrations, IEnumerable<TagRedirect> redirects)
        {
            if (redirects == null)
            {
                throw new ArgumentNullException(nameof(redirects));
            }

            if (registrations == null)
            {
                throw new ArgumentNullException(nameof(registrations));
            }

            var sortedRegistrations = new List<TagRegistration>(registrations);

            foreach (TagRegistration registration in sortedRegistrations)
            {
                if (registration == null)
                {
                    throw new ArgumentException("등록 목록에 null 항목이 있습니다.", nameof(registrations));
                }
            }

            // 입력 순서가 달라도 결과를 일정하게 구성합니다.
            sortedRegistrations.Sort(CompareRegistrations);

            var nodes = new Dictionary<TagId, NodeBuilder>();

            foreach (TagRegistration registration in sortedRegistrations)
            {
                NodeBuilder node = GetOrCreateHierarchy(registration.Tag, nodes);

                // 동일 태그·동일 출처의 중복은 한 번만 보관합니다.
                node.Registrations.TryAdd(registration.Source, registration);
            }

            var tagInfos = new List<TagInfo>(nodes.Count);

            foreach (NodeBuilder node in nodes.Values)
            {
                node.Children.Sort();

                var directRegistrations = new List<TagRegistration>(node.Registrations.Values);

                directRegistrations.Sort(CompareRegistrations);

                tagInfos.Add(new TagInfo(
                    node.Tag,
                    node.Parent,
                    node.Children,
                    directRegistrations));
            }

            return new TagCatalog(tagInfos, redirects);
        }

        private static NodeBuilder GetOrCreateHierarchy(TagId tag, Dictionary<TagId, NodeBuilder> nodes)
        {
            string value = tag.Value;
            NodeBuilder parent = null;

            for (int index = 1; index <= value.Length; index++)
            {
                bool isEnd = index == value.Length;

                if (!isEnd && value[index] != '.')
                {
                    continue;
                }

                TagId currentTag = TagId.Parse(value.Substring(0, index));

                // 이미 존재하는 노드가 있으면 재사용하고, 없으면 새로 만듭니다.
                if (!nodes.TryGetValue(currentTag, out NodeBuilder current))
                {
                    current = new NodeBuilder(currentTag, parent == null ? TagId.None : parent.Tag);

                    nodes.Add(currentTag, current);

                    // 부모 노드가 있으면 자식 목록에 자신을 추가합니다.
                    if (parent != null)
                    {
                        parent.Children.Add(currentTag);
                    }
                }

                parent = current;
            }

            return parent;
        }

        private static int CompareRegistrations(TagRegistration left, TagRegistration right)
        {
            int comparison = left.Tag.CompareTo(right.Tag);

            if (comparison != 0)
            {
                return comparison;
            }

            comparison = left.Source.Kind.CompareTo(right.Source.Kind);

            if (comparison != 0)
            {
                return comparison;
            }

            comparison = string.CompareOrdinal(left.Source.Value, right.Source.Value);

            if (comparison != 0)
            {
                return comparison;
            }

            // 같은 태그·출처에 설명이 여러 개면
            // 비어 있지 않은 설명을 우선합니다.
            comparison = string.IsNullOrEmpty(left.Comment).CompareTo(string.IsNullOrEmpty(right.Comment));

            if (comparison != 0)
            {
                return comparison;
            }

            comparison = string.CompareOrdinal(left.Comment, right.Comment);

            if (comparison != 0)
            {
                return comparison;
            }

            // 대소문자 표기만 다른 경우에도 선택을 일정하게 합니다.
            return string.CompareOrdinal(left.Tag.Value, right.Tag.Value);
        }

        /// <summary>
        /// 사전을 만드는 동안에만 사용하는 가변 작업 데이터입니다.
        /// </summary>
        private sealed class NodeBuilder
        {
            public TagId Tag { get; }

            public TagId Parent { get; }

            public List<TagId> Children { get; } = new();

            public Dictionary<TagSourceId, TagRegistration> Registrations { get; } = new();

            public NodeBuilder(TagId tag, TagId parent)
            {
                Tag = tag;
                Parent = parent;
            }
        }
    }
}
