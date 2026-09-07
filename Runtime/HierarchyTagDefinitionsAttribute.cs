using System;

namespace HierarchyTags
{
    /// <summary>
    /// HierarchyTag 선언을 제공하는 클래스를 표시합니다.
    /// </summary>
    /// <remarks>
    /// 태그는 public static readonly HierarchyTag 필드로 선언합니다.
    /// 실제 검색과 등록은 Editor의 수집기가 담당합니다.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class HierarchyTagDefinitionsAttribute : Attribute
    {
    }
}
