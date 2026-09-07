using System.Collections.Generic;
using HierarchyTags.Contracts;

namespace HierarchyTags.Application
{
    /// <summary>
    /// 현재 제공할 수 있는 직접 등록 정보를 수집합니다.
    /// </summary>
    /// <remarks>
    /// CodeTagRegistrationSource	코드에 선언된 태그
    /// SettingsTagRegistrationSource 수동 설정에 저장된 태그
    /// </remarks>
    public interface ITagRegistrationSource
    {
        IReadOnlyList<TagRegistration> Collect();
    }
}
