using System;
using System.Threading;

namespace HierarchyTags
{
    /// <summary>
    /// 역직렬화한 문자열을 최초 사용 시 해석하고 결과를 고정합니다.
    /// HierarchyTag 구조체가 복사되어도 같은 해석 결과를 공유합니다.
    /// </summary>
    internal sealed class SerializedTagResolution
    {
        private readonly string original;
        private string resolved;

        internal SerializedTagResolution(string value)
        {
            original = value ?? string.Empty;

            if (original.Length == 0)
            {
                resolved = string.Empty;
            }
        }

        internal string Value
        {
            get
            {
                string cached = Volatile.Read(ref resolved);

                if (cached != null)
                {
                    return cached;
                }

                if (!HierarchyTagsManager.TryResolveSerializedValue(original, out string candidate))
                {
                    throw new InvalidOperationException(
                        "HierarchyTags 사전 준비 전에 " +
                        "역직렬화된 태그를 사용했습니다.");
                }

                // 여러 스레드가 동시에 해석해도 최초 결과만 채택합니다.
                string existing = Interlocked.CompareExchange(ref resolved, candidate, null);

                return existing ?? candidate;
            }
        }

        internal bool TryGetResolved(out string value)
        {
            value = Volatile.Read(ref resolved);
            return value != null;
        }

        // 명시적으로 전달받은 Catalog로 조회할 때 사용합니다.
        // 중앙 관리자 초기화를 요구하지 않습니다.
        internal string ValueForExplicitResolution => Volatile.Read(ref resolved) ?? original;
    }
}