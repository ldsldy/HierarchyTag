using System;
using HierarchyTags.Contracts;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace HierarchyTags
{
    /// <summary>대소문자를 구분하지 않는 직렬화 가능한 계층형 태그 식별자입니다.</summary>
    [Serializable]
    [MovedFrom(true, "Deukyeonglee.UnityTags", "Deukyeonglee.UnityTags.Runtime", "UnityTag")]
    public struct HierarchyTag : IEquatable<HierarchyTag>, IComparable<HierarchyTag>, ISerializationCallbackReceiver
    {
        // 기존 씬·프리팹의 name.value 저장 경로만 보존합니다. 모든 동작은 HierarchyTag가 소유합니다.
        [SerializeField]
        private SerializedName name;

        [NonSerialized]
        private SerializedTagResolution resolution;

        [Serializable]
        private struct SerializedName
        {
            public string value;
        }

        public static HierarchyTag None => default;

        public readonly string Value => resolution != null ? resolution.Value : name.value ?? string.Empty;

        public readonly bool IsNone => string.IsNullOrEmpty(name.value);

        /// <summary>형식만 검사합니다. Catalog 등록 여부와는 별개입니다.</summary>
        public readonly bool IsValid => TagId.TryParse(Value, out _);

        /// <summary>
        /// 기존 FName의 대문자 UTF-16 FNV-1a 64비트 해시입니다.
        /// 요청할 때 계산하며, 동일성 비교나 Dictionary 해시로 사용하지 않습니다.
        /// </summary>
        public readonly ulong StableHash
        {
            get
            {
                if (IsNone)
                {
                    return 0;
                }

                unchecked
                {
                    ulong hash = 14695981039346656037UL;
                    foreach (char character in Value)
                    {
                        char normalized = char.ToUpperInvariant(character);
                        hash = (hash ^ (byte)(normalized & 0xFF)) * 1099511628211UL;
                        hash = (hash ^ (byte)(normalized >> 8)) * 1099511628211UL;
                    }

                    return hash;
                }
            }
        }

        public HierarchyTag(string value)
        {
            if (!TagId.TryParse(value, out _))
            {
                throw new ArgumentException(
                    "태그는 문자, 숫자, 밑줄과 계층 구분용 점을 사용해야 하며 빈 계층을 포함할 수 없습니다.",
                    nameof(value));
            }

            name = new SerializedName { value = value };
            resolution = null;
        }

        public static bool TryCreate(string value, out HierarchyTag tag)
        {
            tag = None;
            if (!TagId.TryParse(value, out _))
            {
                return false;
            }

            tag.name.value = value;
            return true;
        }

        /// <summary>Catalog의 등록과 Redirect로 해석한 결과를 반환하며 원본은 변경하지 않습니다.</summary>
        public readonly bool TryResolve(ITagCatalog catalog, out HierarchyTag resolvedTag)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            resolvedTag = None;
            if (IsNone)
            {
                return true;
            }

            string value = resolution != null
                ? resolution.ValueForExplicitResolution
                : name.value ?? string.Empty;


            if (!catalog.TryResolve(value, out TagId resolvedId))
            {
                return false;
            }

            resolvedTag = new HierarchyTag(resolvedId.Value);
            return true;
        }

        /// <summary>현재 태그가 지정한 태그와 같거나 그 하위인지 확인합니다.</summary>
        public readonly bool MatchesTag(HierarchyTag tag)
        {
            if (!IsValid || !tag.IsValid)
            {
                return false;
            }

            if (Equals(tag))
            {
                return true;
            }

            string parent = tag.Value;
            return Value.Length > parent.Length
                && Value.StartsWith(parent, StringComparison.OrdinalIgnoreCase)
                && Value[parent.Length] == '.';
        }

        public readonly bool TryGetParent(out HierarchyTag parent)
        {
            parent = None;
            if (!IsValid)
            {
                return false;
            }

            int separator = Value.LastIndexOf('.');
            if (separator <= 0)
            {
                return false;
            }

            parent = new HierarchyTag(Value.Substring(0, separator));
            return true;
        }

        public readonly bool Equals(HierarchyTag other) =>
            StringComparer.OrdinalIgnoreCase.Equals(Value, other.Value);

        public override readonly bool Equals(object obj) => obj is HierarchyTag other && Equals(other);

        public override readonly int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

        public readonly int CompareTo(HierarchyTag other) =>
            StringComparer.OrdinalIgnoreCase.Compare(Value, other.Value);

        public override readonly string ToString() => Value;

        public static bool operator ==(HierarchyTag left, HierarchyTag right) => left.Equals(right);

        public static bool operator !=(HierarchyTag left, HierarchyTag right) => !left.Equals(right);

        public void OnBeforeSerialize()
        {
            // 이미 해석한 값만 저장합니다.
            // 직렬화 중에 초기화나 사전 조회를 시작하지 않습니다.
            if (resolution != null &&
                resolution.TryGetResolved(out string resolved))
            {
                name.value = resolved;
            }
        }

        public void OnAfterDeserialize()
        {
            // 여기서는 조회, 파일 로딩, 비교, 정렬을 하지 않습니다.
            resolution = new SerializedTagResolution(name.value);
        }

        internal void ApplySerializedRedirect()
        {
            // 컨테이너 정규화에서 사용하는 명시적인 확정 처리입니다.
            resolution ??= new SerializedTagResolution(name.value);

            string resolved = resolution.Value;

            name.value = resolved;
            resolution = null;
        }
    }
}
