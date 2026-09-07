using System;

namespace HierarchyTags.Contracts
{
    /// <summary>
    /// 태그 사전을 전달·저장하기 위한 데이터입니다.
    /// 실행 중인 Catalog와 분리된 가변 데이터입니다.
    /// </summary>
    [Serializable]
    public sealed class HierarchyTagCatalogData
    {
        public const int CurrentVersion = 1;

        // 내보낼 때 명시적으로 설정합니다.
        // 버전 없는 데이터는 복원 시 거부합니다.
        public int version;

        public RegistrationEntry[] registrations;
        public RedirectEntry[] redirects;

        [Serializable]
        public sealed class RegistrationEntry
        {
            public string tag;
            public TagSourceKind sourceKind;
            public string source;
            public string comment;
        }

        [Serializable]
        public sealed class RedirectEntry
        {
            public string oldTag;
            public string newTag;
        }
    }
}