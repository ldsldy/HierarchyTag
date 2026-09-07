using System;
using System.Collections.Generic;
using HierarchyTags.Contracts;

namespace HierarchyTags.Application
{
    /// <summary>
    /// 등록 정보를 수집하고 현재 태그 사전을 교체합니다.
    /// </summary>
    /// <remarks>
    /// Source: 외부 데이터를 읽어 등록 정보로 변환합니다.
    /// Builder: 중복을 병합하고 부모 계층을 구성합니다.
    /// Service: 수집과 구성을 실행하고 현재 Catalog를 소유합니다.
    /// </remarks>
    public sealed class TagCatalogService
    {
        private readonly ITagRegistrationSource[] sources;

        private ITagCatalog current;

        public bool IsInitialized => current != null;

        public ITagCatalog Current
        {
            get
            {
                if (current == null)
                {
                    throw new InvalidOperationException("태그 사전이 초기화되지 않았습니다. Refresh를 먼저 호출해야 합니다.");
                }

                return current;
            }
        }

        public TagCatalogService(IEnumerable<ITagRegistrationSource> sources)
        {
            if (sources == null)
            {
                throw new ArgumentNullException(nameof(sources));
            }

            var sourceList = new List<ITagRegistrationSource>(sources);

            foreach (ITagRegistrationSource source in sourceList)
            {
                if (source == null)
                {
                    throw new ArgumentException("등록 정보 공급자에 null 항목이 있습니다.", nameof(sources));
                }
            }

            this.sources = sourceList.ToArray();
        }

        /// <summary>
        /// Redirect 없이 새 Catalog를 구성합니다.
        /// </summary>
        public ITagCatalog Refresh()
        {
            return Refresh(Array.Empty<TagRedirect>());
        }

        /// <summary>
        /// 전달한 Redirect를 포함하여 새 Catalog를 구성합니다.
        /// </summary>
        public ITagCatalog Refresh(IEnumerable<TagRedirect> redirects)
        {
            if (redirects == null)
            {
                throw new ArgumentNullException(nameof(redirects));
            }

            var registrations = new List<TagRegistration>();

            foreach (ITagRegistrationSource source in sources)
            {
                IReadOnlyList<TagRegistration> collected = source.Collect();

                if (collected == null)
                {
                    throw new InvalidOperationException(
                        $"{source.GetType().Name}: 수집 결과가 null입니다.");
                }

                registrations.AddRange(collected);
            }

            ITagCatalog next = TagCatalogBuilder.Build(
                registrations,
                redirects);

            // 등록 정보와 Redirect 구성이 모두 성공했을 때 교체합니다.
            current = next;

            return current;
        }
    }
}
