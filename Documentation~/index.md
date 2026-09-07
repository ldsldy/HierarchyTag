# Deukyeonglee Hierarchy Tags

점으로 구분한 계층형 태그를 코드 또는 Project Settings에서 선언하고,
Inspector에서 선택할 수 있는 Unity 패키지입니다.

## 빠른 시작

```csharp
[HierarchyTagDefinitions]
public static class GameplayTags
{
    public static readonly HierarchyTag State_Dead =
        new HierarchyTag("State.Dead");
}
```

```csharp
public sealed class TaggedObject : MonoBehaviour
{
    [SerializeField] private HierarchyTag state;
    [SerializeField] private HierarchyTagContainer tags;
}
```

수동 등록과 이름 변경은 `Edit > Project Settings > Hierarchy Tags`에서 수행합니다.
코드와 설정에서 같은 ID를 선언하면 하나의 태그로 합치면서 출처 정보는 유지합니다.
이름 변경은 Redirect로 저장되며 직렬화된 이전 이름은 역직렬화할 때 변환됩니다.

설치, 이름 규칙, 비교 API, 폴더 구조와 제한 사항은
[패키지 README](../README.md)를 참고하십시오.
