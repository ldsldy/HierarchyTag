using System;

namespace HierarchyTags.Contracts
{
    /// <summary>
    /// 태그 정의를 제공하는 코드 또는 설정 출처입니다.
    /// </summary>
    public readonly struct TagSourceId : IEquatable<TagSourceId>
    {
        private readonly string value;

        public TagSourceKind Kind { get; }

        public string Value => value ?? string.Empty;

        public bool IsNone =>
            Kind == TagSourceKind.None ||
            string.IsNullOrEmpty(value);

        public TagSourceId(TagSourceKind kind, string value)
        {
            if (kind != TagSourceKind.Code &&
                kind != TagSourceKind.Config)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(kind),
                    kind,
                    "코드 또는 설정 출처를 지정해야 합니다.");
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "출처 식별자는 비어 있을 수 없습니다.",
                    nameof(value));
            }

            Kind = kind;
            this.value = value;
        }

        public bool Equals(TagSourceId other)
        {
            return Kind == other.Kind &&
                StringComparer.Ordinal.Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            return obj is TagSourceId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Kind, StringComparer.Ordinal.GetHashCode(Value));
        }

        public override string ToString()
        {
            return IsNone ? string.Empty : $"{Kind}:{Value}";
        }

        public static bool operator ==(TagSourceId left, TagSourceId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TagSourceId left, TagSourceId right)
        {
            return !left.Equals(right);
        }
    }
}
