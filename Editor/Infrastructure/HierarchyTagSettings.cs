using System;
using System.Collections.Generic;
using HierarchyTags.Application;
using HierarchyTags.Contracts;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace HierarchyTags.Editor
{
    /// <summary>
    /// 수동 태그 등록과 Redirect 설정을 저장합니다.
    /// </summary>
    /// <remarks>
    /// RedirectEntry : Unity가 파일에 저장하는 문자열 쌍
    /// TagRedirect : Application에서 사용하는 Redirect 값
    /// </remarks>
    [FilePath("ProjectSettings/HierarchyTagSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    [MovedFrom(true, "Deukyeonglee.UnityTags.Editor", "Deukyeonglee.UnityTags.Editor", "UnityTagSettings")]
    internal sealed class HierarchyTagSettings : ScriptableSingleton<HierarchyTagSettings>, IManualTagStore
    {
        [SerializeField]
        private List<string> registeredTags = new();

        [SerializeField]
        private List<RedirectEntry> redirects = new();

        public TagSourceId Source { get; } = new TagSourceId(
            TagSourceKind.Config,
            "ProjectSettings/HierarchyTagSettings.asset");

        public event Action Changed;

        public IReadOnlyList<string> ReadAll()
        {
            registeredTags ??= new List<string>();

            return new List<string>(registeredTags).AsReadOnly();
        }

        public void WriteAll(IReadOnlyList<string> values, IReadOnlyList<TagRedirect> redirectValues)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (redirectValues == null)
            {
                throw new ArgumentNullException(nameof(redirectValues));
            }

            var nextTags = new List<string>(values);
            var nextRedirects = new List<RedirectEntry>(
                redirectValues.Count);

            for (int i = 0; i < redirectValues.Count; i++)
            {
                TagRedirect redirect = redirectValues[i];

                if (redirect == null)
                {
                    throw new ArgumentException(
                        $"Redirect {i + 1}번 항목이 null입니다.",
                        nameof(redirectValues));
                }

                nextRedirects.Add(new RedirectEntry(redirect));
            }

            List<string> previousTags = registeredTags;
            List<RedirectEntry> previousRedirects = redirects;

            registeredTags = nextTags;
            redirects = nextRedirects;

            try
            {
                Save(true);
            }
            catch
            {
                registeredTags = previousTags;
                redirects = previousRedirects;
                throw;
            }

            Changed?.Invoke();
        }

        /// <summary>
        /// 저장 데이터를 TagRedirect 목록으로 변환하여 반환합니다.
        /// </summary>
        public IReadOnlyList<TagRedirect> ReadRedirects()
        {
            redirects ??= new List<RedirectEntry>();

            var result = new List<TagRedirect>(redirects.Count);

            for (int i = 0; i < redirects.Count; i++)
            {
                RedirectEntry entry = redirects[i];

                if (entry == null)
                {
                    throw new InvalidOperationException(
                        $"{Source.Value}: Redirect {i + 1}번 항목이 null입니다.");
                }

                try
                {
                    result.Add(entry.ToRedirect());
                }
                catch (ArgumentException exception)
                {
                    throw new InvalidOperationException(
                        $"{Source.Value}: Redirect {i + 1}번 항목이 " +
                        $"잘못되었습니다. {exception.Message}",
                        exception);
                }
            }

            return result.AsReadOnly();
        }

        [Serializable]
        private sealed class RedirectEntry
        {
            [SerializeField]
            private string oldTag;

            [SerializeField]
            private string newTag;

            public RedirectEntry(TagRedirect redirect)
            {
                oldTag = redirect.OldTag.Value;
                newTag = redirect.NewTag.Value;
            }

            public TagRedirect ToRedirect()
            {
                return new TagRedirect(TagId.Parse(oldTag), TagId.Parse(newTag));
            }
        }
    }
}
