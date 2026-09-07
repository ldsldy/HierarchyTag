using System.Collections.Generic;
using Deukyeonglee.HierarchyTags.Contracts;

namespace Deukyeonglee.HierarchyTags.Application
{
    /// <summary>
    /// 수동 태그와 관련 Redirect 설정을 읽고 저장합니다.
    /// </summary>
    public interface IManualTagStore
    {
        TagSourceId Source { get; }

        IReadOnlyList<string> ReadAll();

        IReadOnlyList<TagRedirect> ReadRedirects();

        /// <summary>
        /// 태그를 먼저 저장하고 Redirect를 나중에 저장하면
        /// 중간에 실패하거나 갱신이 실행될 때 두 값이 일관되지 않을 수 있습니다.
        /// 두 목록을 한번의 작업으로 저장하는 것이 안전합니다.
        /// </summary>
        void WriteAll(IReadOnlyList<string> values, IReadOnlyList<TagRedirect> redirects);
    }
}
