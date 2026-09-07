using System;
using System.Collections.Generic;
using Deukyeonglee.HierarchyTags.Contracts;

namespace Deukyeonglee.HierarchyTags.Application
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
            return TryValidateAndWrite(values, store.ReadRedirects(), catalog, out error);
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
    }
}
