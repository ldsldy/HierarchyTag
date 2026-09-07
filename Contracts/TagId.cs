using System;

namespace Deukyeonglee.HierarchyTags.Contracts
{
    /// <summary>
    /// Unity에 의존하지 않는 태그 식별자입니다.
    /// </summary>
    /// <remarks>
    /// 생성은 이름의 형식만 검사합니다.
    /// 사전 등록 여부는 Catalog에서 별도로 확인해야 합니다.
    /// </remarks>
    public readonly struct TagId : IEquatable<TagId>, IComparable<TagId>
    {
        private readonly string value;

        public static TagId None => default;

        public string Value => value ?? string.Empty;

        public bool IsNone => string.IsNullOrEmpty(value);

        private TagId(string value)
        {
            this.value = value;
        }

        /// <summary>
        /// 형식이 올바른 이름으로 식별자를 생성합니다.
        /// </summary>
        public static TagId Parse(string value)
        {
            if (!TryParse(value, out TagId tag))
            {
                throw new ArgumentException(
                    "태그는 문자, 숫자, 밑줄과 계층 구분용 점을 " +
                    "사용해야 하며 빈 계층을 포함할 수 없습니다.",
                    nameof(value));
            }

            return tag;
        }

        public static bool TryParse(string value, out TagId tag)
        {
            if (!HasValidFormat(value))
            {
                tag = None;
                return false;
            }

            tag = new TagId(value);
            return true;
        }

        public bool Equals(TagId other)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(
                Value,
                other.Value);
        }

        public override bool Equals(object obj)
        {
            return obj is TagId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return IsNone
                ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
        }

        public int CompareTo(TagId other)
        {
            return StringComparer.OrdinalIgnoreCase.Compare(Value, other.Value);
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(TagId left, TagId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TagId left, TagId right)
        {
            return !left.Equals(right);
        }

        private static bool HasValidFormat(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            bool previousWasSeparator = true;

            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];

                if (character == '.')
                {
                    if (previousWasSeparator ||
                        index == value.Length - 1)
                    {
                        return false;
                    }

                    previousWasSeparator = true;
                    continue;
                }

                if (!char.IsLetterOrDigit(character) &&
                    character != '_')
                {
                    return false;
                }

                previousWasSeparator = false;
            }

            return !previousWasSeparator;
        }
    }
}