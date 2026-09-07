using System;
using System.Collections.Generic;
using HierarchyTags.Contracts;

namespace HierarchyTags.Application
{
    /// <summary>
    /// Catalog와 저장용 데이터 사이의 변환을 담당합니다.
    /// UnityEditor, 파일 시스템, JSON 라이브러리에 의존하지 않습니다.
    /// </summary>
    public static class HierarchyTagCatalogDataConverter
    {
        public static HierarchyTagCatalogData Export(ITagCatalog catalog, IReadOnlyList<TagRedirect> redirects)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            if (redirects == null)
            {
                throw new ArgumentNullException(nameof(redirects));
            }

            var registrations = new List<TagRegistration>();

            foreach (TagInfo info in catalog.Tags)
            {
                registrations.AddRange(info.Registrations);
            }

            // 전달된 등록 정보와 Redirect를 함께 검증합니다.
            // 정렬·중복 정리도 기존 Builder 규칙을 사용합니다.
            TagCatalog validated = TagCatalogBuilder.Build(registrations, redirects);

            var registrationEntries = new List<HierarchyTagCatalogData.RegistrationEntry>();

            foreach (TagInfo info in validated.Tags)
            {
                foreach (TagRegistration registration in info.Registrations)
                {
                    registrationEntries.Add(
                        new HierarchyTagCatalogData.RegistrationEntry
                        {
                            tag = registration.Tag.Value,
                            sourceKind = registration.Source.Kind,
                            source = registration.Source.Value,
                            comment = registration.Comment
                        });
                }
            }

            // 실행용 데이터에는 체인의 최종 대상만 저장합니다.
            var resolvedRedirects = new SortedDictionary<string, string>(StringComparer.Ordinal);

            foreach (TagRedirect redirect in redirects)
            {
                if (!validated.TryResolve(redirect.OldTag.Value, out TagId finalTarget))
                {
                    throw new InvalidOperationException($"Redirect 해석 실패: {redirect.OldTag.Value}");
                }

                // 태그의 동일성은 대소문자를 구분하지 않습니다.
                // 이전 이름을 정규화해 출력 순서와 중복 처리를 고정합니다.
                string oldName = redirect.OldTag.Value.ToUpperInvariant();

                resolvedRedirects[oldName] = finalTarget.Value;
            }

            var redirectEntries = new List<HierarchyTagCatalogData.RedirectEntry>();

            foreach (var pair in resolvedRedirects)
            {
                redirectEntries.Add(
                    new HierarchyTagCatalogData.RedirectEntry
                    {
                        oldTag = pair.Key,
                        newTag = pair.Value
                    });
            }

            return new HierarchyTagCatalogData
            {
                version = HierarchyTagCatalogData.CurrentVersion,
                registrations = registrationEntries.ToArray(),
                redirects = redirectEntries.ToArray()
            };
        }

        public static TagCatalog Import(HierarchyTagCatalogData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (data.version != HierarchyTagCatalogData.CurrentVersion)
            {
                throw new InvalidOperationException($"지원하지 않는 태그 데이터 버전입니다: {data.version}");
            }

            if (data.registrations == null || data.redirects == null)
            {
                throw new InvalidOperationException("태그 데이터의 registrations 또는 redirects가 없습니다.");
            }

            var registrations = new List<TagRegistration>(data.registrations.Length);

            for (int i = 0; i < data.registrations.Length; i++)
            {
                var entry = data.registrations[i];

                if (entry == null)
                {
                    throw new InvalidOperationException($"등록 데이터 {i}번 항목이 null입니다.");
                }

                try
                {
                    registrations.Add(
                        new TagRegistration(
                            TagId.Parse(entry.tag),
                            new TagSourceId(
                                entry.sourceKind,
                                entry.source),
                            entry.comment));
                }
                catch (ArgumentException exception)
                {
                    throw new InvalidOperationException($"등록 데이터 {i}번 항목이 올바르지 않습니다.", exception);
                }
            }

            var redirects = new List<TagRedirect>(data.redirects.Length);

            for (int i = 0; i < data.redirects.Length; i++)
            {
                var entry = data.redirects[i];

                if (entry == null)
                {
                    throw new InvalidOperationException($"Redirect 데이터 {i}번 항목이 null입니다.");
                }

                try
                {
                    redirects.Add(
                        new TagRedirect(
                            TagId.Parse(entry.oldTag),
                            TagId.Parse(entry.newTag)));
                }
                catch (ArgumentException exception)
                {
                    throw new InvalidOperationException($"Redirect 데이터 {i}번 항목이 올바르지 않습니다.", exception);
                }
            }

            // 부모 계층, 출처 병합, Redirect 충돌·순환·대상 존재 여부는
            // 기존 Builder에서 동일하게 검증합니다.
            return TagCatalogBuilder.Build(registrations, redirects);
        }
    }
}