using System;
using System.Collections.Generic;
using HierarchyTags.Contracts;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace HierarchyTags
{
    /// <summary>
    /// 하나 이상의 HierarchyTag를 중복 없이 보관하는 컨테이너 클래스입니다.
    /// </summary>
    /// <remarks>
    /// 정확한 태그 비교와 부모 계층을 포함한 비교를 지원합니다.
    /// Unity 직렬화 과정에서 비어 있거나 중복된 태그는 제거됩니다.
    /// </remarks>
    [Serializable]
    [MovedFrom(true, "Deukyeonglee.UnityTags", "Deukyeonglee.UnityTags.Runtime", "UnityTagContainer")]
    public sealed class HierarchyTagContainer : ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<HierarchyTag> tags = new();

        [NonSerialized]
        private IReadOnlyList<HierarchyTag> readOnlyTags;

        [NonSerialized]
        private bool normalizationPending;

        /// <summary>
        /// 컨테이너에 포함된 태그의 수를 반환합니다.
        /// </summary>
        public int Count
        {
            get
            {
                EnsureCollection();
                return tags.Count;
            }
        }
        
        /// <summary>
        /// 컨테이너에 포함된 태그들을 리스트 형태로 반환합니다.
        /// </summary>
        public IReadOnlyList<HierarchyTag> Tags
        {
            get
            {
                EnsureCollection();
                return readOnlyTags ??= tags.AsReadOnly();
            }
        }

        public HierarchyTagContainer() { }

        /// <summary>
        /// 주어진 태그들로 컨테이너를 초기화합니다.
        /// </summary>
        public HierarchyTagContainer(IEnumerable<HierarchyTag> initialTags)
        {
            if (initialTags == null)
            {
                throw new ArgumentNullException(nameof(initialTags)); 
            }

            var values = new List<HierarchyTag>(initialTags);
            foreach (HierarchyTag tag in values)
            {
                if (!tag.IsValid)
                {
                    throw new ArgumentException("유효하지 않은 태그입니다.", nameof(initialTags));
                }
            }

            tags = NormalizeTags(values);
        }

        /// <summary>
        /// 태그를 컨테이너에 추가합니다.
        /// </summary>
        public bool Add(HierarchyTag tag)
        {
            EnsureCollection();

            if (!tag.IsValid)
            {
                throw new ArgumentException("유효하지 않은 HierarchyTag는 추가할 수 없습니다.", nameof(tag));
            }

            if (HasTagExact(tag))
            {
                return false;
            }

            tags.Add(tag);
            tags.Sort();

            return true;
        }

        /// <summary>
        /// 정확히 일치하는 명시 태그를 제거합니다.
        /// </summary>
        public bool Remove(HierarchyTag tag)
        {
            EnsureCollection();

            if (!tag.IsValid)
            {
                return false;
            }

            for (int index = 0; index < tags.Count; index++)
            {
                if (tags[index] != tag)
                {
                    continue;
                }

                tags.RemoveAt(index);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 모든 태그를 Catalog로 해석한 새 컨테이너를 반환합니다.
        /// 하나라도 해석할 수 없으면 실패하며 원본은 변경하지 않습니다.
        /// </summary>
        public bool TryResolve(ITagCatalog catalog, out HierarchyTagContainer resolvedContainer)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            resolvedContainer = null;

            var resolvedTags = new List<HierarchyTag>(
                tags?.Count ?? 0);

            if (tags != null)
            {
                foreach (HierarchyTag tag in tags)
                {
                    if (!tag.TryResolve(catalog, out HierarchyTag resolvedTag))
                    {
                        return false;
                    }

                    if (!resolvedTag.IsNone)
                    {
                        resolvedTags.Add(resolvedTag);
                    }
                }
            }

            // 기존 생성자의 중복 제거와 정렬을 재사용합니다.
            resolvedContainer = new HierarchyTagContainer(resolvedTags);
            return true;
        }

        /// <summary>
        /// 정확히 일치하는 태그가 있는지 확인합니다.
        /// </summary>
        public bool HasTagExact(HierarchyTag tagToCheck)
        {
            EnsureCollection();

            if (!tagToCheck.IsValid)
            {
                return false;
            }

            for (int index = 0; index < tags.Count; index++)
            {
                if (tags[index] == tagToCheck)
                {
                    return true;
                }
            }

            return false;   
        }

        /// <summary>
        /// 컨테이너에 지정한 태그와 같거나 하위인 태그가 포함되어 있는지 확인합니다.
        /// </summary>
        /// <example>
        /// State.OnOff.On을 보관하고 있으면
        /// State 및 State.OnOff 검사도 true를 반환합니다.
        /// </example>
        public bool HasTag(HierarchyTag tagToCheck)
        {
            EnsureCollection();

            if (!tagToCheck.IsValid)
            {
                return false;
            }

            for (int index = 0; index < tags.Count; index++)
            {
                if (tags[index].MatchesTag(tagToCheck))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 전달된 태그 중 하나 이상을 포함하고 있는지 확인합니다.
        /// </summary>
        public bool HasAny(IEnumerable<HierarchyTag> tagsToCheck)
        {
            if (tagsToCheck == null)
            {
                throw new ArgumentNullException(nameof(tagsToCheck));
            }

            foreach (HierarchyTag tag in tagsToCheck)
            {
                if (HasTag(tag))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 전달된 모든 태그를 포함하고 있는지 확인합니다.
        /// </summary>
        public bool HasAll(IEnumerable<HierarchyTag> tagsToCheck)
        {
            if (tagsToCheck == null)
            {
                throw new ArgumentNullException(nameof(tagsToCheck));
            }

            foreach (HierarchyTag tag in tagsToCheck)
            {
                if (!HasTag(tag))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 전달된 태그 중 정확히 일치하는 태그가 하나 이상 포함되어 있는지 확인합니다.
        /// </summary>
        public bool HasAnyExact(IEnumerable<HierarchyTag> tagsToCheck)
        {
            if (tagsToCheck == null)
            {
                throw new ArgumentNullException(nameof(tagsToCheck));
            }

            foreach (HierarchyTag tag in tagsToCheck)
            {
                if (HasTagExact(tag))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 전달된 모든 태그가 정확히 일치하는지 확인합니다.
        /// </summary>
        public bool HasAllExact(IEnumerable<HierarchyTag> tagsToCheck)
        {
            if (tagsToCheck == null)
            {
                throw new ArgumentNullException(nameof(tagsToCheck));
            }
            foreach (HierarchyTag tag in tagsToCheck)
            {
                if (!HasTagExact(tag))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 모든 태그를 제거합니다.
        /// </summary>
        public void Clear()
        {
            EnsureCollection();
            tags.Clear();
        }

        public void OnBeforeSerialize() { }

        /// <summary>
        /// Inspector 또는 JSON을 통해 들어온 태그들을 정규화합니다.
        /// </summary>
        public void OnAfterDeserialize()
        {
            tags ??= new List<HierarchyTag>();

            // 개별 태그 콜백의 호출 순서에 의존하지 않도록
            // 저장값을 해석 대기 상태로 만듭니다.
            for (int i = 0; i < tags.Count; i++)
            {
                HierarchyTag tag = tags[i];
                tag.OnAfterDeserialize();
                tags[i] = tag;
            }

            normalizationPending = true;
            readOnlyTags = null;
        }

        /// <summary>
        /// 역직렬화된 목록은 최초 사용 시 Redirect 적용·중복 제거·정렬합니다.
        /// </summary>
        /// <param name="values">정규화할 태그 목록</param>
        /// <param name="applyRedirects">Redirect를 적용할지 여부</param>
        /// <returns>정규화된 태그 목록</returns>
        internal static List<HierarchyTag> NormalizeTags(IEnumerable<HierarchyTag> values, bool applyRedirects = false)
        {
            var normalizedTags = new List<HierarchyTag>();

            var uniqueTags = new HashSet<HierarchyTag>();

            foreach (HierarchyTag value in values)
            {
                HierarchyTag tag = value;
                if (applyRedirects)
                {
                    // 개별 태그 콜백의 호출 순서에 의존하지 않습니다.
                    tag.ApplySerializedRedirect();
                }

                if (!tag.IsValid)
                {
                    continue;
                }

                if (!uniqueTags.Add(tag))
                {
                    continue;
                }

                normalizedTags.Add(tag);
            }

            normalizedTags.Sort();
            return normalizedTags;
        }

        private void EnsureCollection()
        {
            if (tags == null)
            {
                tags = new List<HierarchyTag>();
                readOnlyTags = null;
            }

            if (!normalizationPending)
            {
                return;
            }

            // 실패하면 기존 목록과 pending 상태를 유지합니다.
            // 사전 준비 후 다시 접근하면 재시도할 수 있습니다.
            List<HierarchyTag> normalized = NormalizeTags(tags, applyRedirects: true);

            tags = normalized;
            readOnlyTags = null;
            normalizationPending = false;
        }
    }
}
