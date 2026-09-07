using System;
using System.Collections.Generic;
using HierarchyTags.Application;
using HierarchyTags.Contracts;

namespace HierarchyTags.Editor.Infrastructure
{
    /// <summary>
    /// Config 혹은 에디터 설정에서 등록된 태그를 수집하여 TagRegistration에 등록하는 역할을 수행하는 클래스입니다.
    /// </summary>
    internal sealed class SettingsTagRegistrationSource : ITagRegistrationSource
    {
        private readonly IManualTagStore store;

        public SettingsTagRegistrationSource(IManualTagStore store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public IReadOnlyList<TagRegistration> Collect()
        {
            IReadOnlyList<string> values = store.ReadAll();
            TagSourceId sourceId = store.Source;

            var registrations = new List<TagRegistration>(values.Count);

            var seen = new HashSet<TagId>();

            for (int index = 0; index < values.Count; index++)
            {
                string value = values[index];

                if (!TagId.TryParse(value, out TagId tag))
                {
                    throw new InvalidOperationException($"{sourceId.Value}: registeredTags[{index}]에 잘못된 태그가 있습니다. 값: '{value}'");
                }

                if (!seen.Add(tag))
                {
                    throw new InvalidOperationException($"{sourceId.Value}: 수동 목록에 중복 태그가 있습니다. 태그: '{value}'");
                }

                registrations.Add(new TagRegistration(tag, sourceId));
            }

            return registrations.AsReadOnly();
        }
    }
}
