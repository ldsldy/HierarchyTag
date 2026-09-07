using System;
using System.Threading;
using HierarchyTags.Contracts;

namespace HierarchyTags
{
    /// <summary>
    /// 현재 태그 사전에 접근하는 공개 진입점입니다.
    /// 사전 준비와 교체는 패키지 내부에서 담당합니다.
    /// </summary>
    public static class HierarchyTagsManager
    {
        private static ITagCatalog current;

        /// <summary>
        /// 조회 가능한 사전이 설치되어 있는지 확인합니다.
        /// </summary>
        public static bool IsReady =>
            Volatile.Read(ref current) != null;

        /// <summary>
        /// 이름을 조회하고 Redirect를 적용한 태그를 반환합니다.
        /// 미등록 이름이나 잘못된 형식이면 false를 반환합니다.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// 사전이 준비되지 않았습니다.
        /// </exception>
        public static bool TryGetTag(string value, out HierarchyTag tag)
        {
            ITagCatalog catalog = RequireCatalog();

            tag = HierarchyTag.None;

            if (!catalog.TryResolve(value, out TagId resolvedId))
            {
                return false;
            }

            tag = new HierarchyTag(resolvedId.Value);
            return true;
        }

        /// <summary>
        /// 반드시 존재해야 하는 태그를 요청합니다.
        /// Redirect를 적용하며, 찾지 못하면 예외를 발생시킵니다.
        /// </summary>
        public static HierarchyTag GetTag(string value)
        {
            if (TryGetTag(value, out HierarchyTag tag))
            {
                return tag;
            }

            throw new ArgumentException($"태그 사전에서 이름을 찾을 수 없습니다: '{value}'", nameof(value));
        }

        /// <summary>
        /// 해당 이름이 현재 사전에 직접 존재하는지 확인합니다.
        /// Redirect는 적용하지 않습니다.
        /// 자동으로 구성된 부모 태그도 포함합니다.
        /// </summary>
        public static bool ContainsTag(string value)
        {
            ITagCatalog catalog = RequireCatalog();

            return TagId.TryParse(value, out TagId tagId)
                && catalog.Contains(tagId);
        }

        /// <summary>
        /// 이름을 해석한 후 태그의 계층·등록 정보를 반환합니다.
        /// Redirect를 적용합니다.
        /// </summary>
        public static bool TryGetInfo(
            string value,
            out TagInfo info)
        {
            ITagCatalog catalog = RequireCatalog();

            info = null;

            return catalog.TryResolve(value, out TagId tagId)
                && catalog.TryGetInfo(tagId, out info);
        }

        /// <summary>
        /// 구성이 완료된 읽기 전용 사전을 게시합니다.
        /// 게시한 사전은 이후 수정하지 않아야 합니다.
        /// </summary>
        internal static void InstallCatalog(ITagCatalog catalog)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            Volatile.Write(ref current, catalog);
        }

        /// <summary>
        /// 저장된 값을 해석합니다.
        /// false는 사전이 아직 준비되지 않았다는 뜻입니다.
        /// 미등록 값은 원문을 보존합니다.
        /// </summary>
        internal static bool TryResolveSerializedValue(string value, out string resolved)
        {
            resolved = value ?? string.Empty;

            if (resolved.Length == 0)
            {
                return true;
            }

            ITagCatalog catalog = Volatile.Read(ref current);

            if (catalog == null)
            {
                return false;
            }

            if (catalog.TryResolve(resolved, out TagId tag))
            {
                resolved = tag.Value;
            }

            return true;
        }

        /// <summary>
        /// 세션 종료 또는 초기화 실패 시 사전을 해제합니다.
        /// </summary>
        internal static void ClearCatalog()
        {
            Volatile.Write(ref current, null);
        }

        private static ITagCatalog RequireCatalog()
        {
            ITagCatalog catalog = Volatile.Read(ref current);

            if (catalog == null)
            {
                throw new InvalidOperationException(
                    "HierarchyTags 사전이 준비되지 않았습니다.");
            }

            return catalog;
        }
    }
}