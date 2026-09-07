namespace HierarchyTags.Samples
{
    /// <summary>
    /// 코드에서 계층형 태그를 선언하는 예제입니다.
    /// </summary>
    [HierarchyTagDefinitions]
    public static class ExampleTags
    {
        public static readonly HierarchyTag State_Alive =
            new HierarchyTag("State.Alive");

        public static readonly HierarchyTag State_Dead =
            new HierarchyTag("State.Dead");

        public static readonly HierarchyTag Widget_Modal =
            new HierarchyTag("Widget.Modal");
    }
}
