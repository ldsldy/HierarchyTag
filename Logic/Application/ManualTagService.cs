using System;
using System.Collections.Generic;
using HierarchyTags.Contracts;

namespace HierarchyTags.Application
{
    /// <summary>
    /// 설정 출처의 태그 추가, 삭제 및 이름 변경을 처리합니다.
    /// </summary>
    public sealed class ManualTagService
    {
        private readonly IManualTagStore store;

        public TagSourceId Source => store.Source;

        public ManualTagService(IManualTagStore store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));

            if (store.Source.IsNone || store.Source.Kind != TagSourceKind.Config)
            {
                throw new ArgumentException(
                    "수동 태그 저장소에는 설정 출처가 필요합니다.",
                    nameof(store));
            }
        }

        public bool TryAdd(string value, ITagCatalog catalog, out string error)
        {
            if (!TryRequireCatalog(catalog, out error))
            {
                return false;
            }

            if (!TagId.TryParse(value, out TagId tag))
            {
                error =
                    "태그 형식이 올바르지 않습니다. 문자, 숫자, 밑줄과 점을 사용하고 " +
                    "빈 계층은 만들 수 없습니다.";
                return false;
            }

            var values = new List<string>(store.ReadAll());

            if (FindIndex(values, tag.Value) >= 0)
            {
                error = $"이미 수동 등록된 태그입니다: {tag.Value}";
                return false;
            }

            values.Add(tag.Value);
            values.Sort(StringComparer.OrdinalIgnoreCase);
            return TryValidateAndWrite(values, store.ReadRedirects(), catalog, out error);
        }

        public bool TryRemove(string value, ITagCatalog catalog, out string error)
        {
            if (!TryRequireCatalog(catalog, out error))
            {
                return false;
            }

            var values = new List<string>(store.ReadAll());
            int index = FindIndex(values, value);

            if (index < 0)
            {
                error = $"수동 등록을 찾을 수 없습니다: {value}";
                return false;
            }

            values.RemoveAt(index);

            List<TagRegistration> remainingRegistrations = CollectRegistrations(catalog, values);

            TagCatalog remainingCatalog = TagCatalogBuilder.Build(remainingRegistrations);

            List<TagRedirect> remainingRedirects = RemoveRedirectsWithMissingTargets(store.ReadRedirects(), catalog, remainingCatalog);

            return TryValidateAndWrite(values, remainingRedirects, catalog, out error);
        }

        public bool TryRename(
            string oldValue,
            string newValue,
            ITagCatalog catalog,
            out string error)
        {
            if (!TryRequireCatalog(catalog, out error))
            {
                return false;
            }

            if (!TagId.TryParse(oldValue, out TagId oldTag) ||
                !TagId.TryParse(newValue, out TagId newTag))
            {
                error = "태그 형식이 올바르지 않습니다.";
                return false;
            }

            if (oldTag == newTag)
            {
                error = "같은 태그로 이름을 변경할 수 없습니다.";
                return false;
            }

            var values = new List<string>(store.ReadAll());
            int oldIndex = FindIndex(values, oldTag.Value);

            if (oldIndex < 0)
            {
                error = $"수동 등록을 찾을 수 없습니다: {oldTag.Value}";
                return false;
            }

            values.RemoveAt(oldIndex);

            List<TagRegistration> remainingRegistrations =
                CollectRegistrations(catalog, values);

            TagCatalog remaining = TagCatalogBuilder.Build(remainingRegistrations);

            if (remaining.TryGetInfo(newTag, out TagInfo targetInfo))
            {
                newTag = targetInfo.Tag;
            }
            else
            {
                values.Add(newTag.Value);
            }

            values.Sort(StringComparer.OrdinalIgnoreCase);

            var redirects = new List<TagRedirect>(store.ReadRedirects())
            {
                new TagRedirect(oldTag, newTag)
            };

            return TryValidateAndWrite(values, redirects, catalog, out error);
        }

        private bool TryValidateAndWrite(
            IReadOnlyList<string> values,
            IReadOnlyList<TagRedirect> redirects,
            ITagCatalog catalog,
            out string error)
        {
            try
            {
                List<TagRegistration> registrations =
                    CollectRegistrations(catalog, values);

                TagCatalogBuilder.Build(registrations, redirects);
            }
            catch (InvalidOperationException exception)
            {
                error = exception.Message;
                return false;
            }

            store.WriteAll(values, redirects);
            error = string.Empty;
            return true;
        }

        private List<TagRegistration> CollectRegistrations(
            ITagCatalog catalog,
            IReadOnlyList<string> manualValues)
        {
            var result = new List<TagRegistration>();

            foreach (TagInfo info in catalog.Tags)
            {
                foreach (TagRegistration registration in info.Registrations)
                {
                    if (registration.Source != Source)
                    {
                        result.Add(registration);
                    }
                }
            }

            foreach (string value in manualValues)
            {
                result.Add(new TagRegistration(TagId.Parse(value), Source));
            }

            return result;
        }

        private static bool TryRequireCatalog(ITagCatalog catalog, out string error)
        {
            if (catalog == null)
            {
                error = "태그 사전을 사용할 수 없습니다.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static int FindIndex(IReadOnlyList<string> values, string value)
        {
            for (int index = 0; index < values.Count; index++)
            {
                if (string.Equals(
                    values[index],
                    value,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return index;
                }
            }

            return -1;
        }

        private static List<TagRedirect> RemoveRedirectsWithMissingTargets(IReadOnlyList<TagRedirect> redirects, ITagCatalog currentCatalog, ITagCatalog remainingCatalog)
        {
            var result = new List<TagRedirect>(redirects.Count);

            foreach (TagRedirect redirect in redirects)
            {
                // 잘못된 기존 데이터는 여기서 조용히 지우지 않고
                // 기존 검증 과정에서 오류로 처리하게 둡니다.
                if (redirect == null)
                {
                    result.Add(redirect);
                    continue;
                }

                // 현재 Catalog를 이용해 연쇄 Redirect의 최종 대상을 얻습니다.
                // 해석할 수 없는 Redirect는 보존하여 이후 검증에서 오류로 처리합니다.
                if (!currentCatalog.TryResolve(redirect.OldTag.Value, out TagId finalTarget))
                {
                    result.Add(redirect);
                    continue;
                }

                // 삭제 이후에도 최종 대상이 존재하는 Redirect만 보존합니다.
                if (remainingCatalog.Contains(finalTarget))
                {
                    result.Add(redirect);
                }
            }

            return result;
        }
    }
}
