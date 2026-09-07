# Changelog

형식은 [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)와
[Semantic Versioning](https://semver.org/spec/v2.0.0.html)을 따릅니다.

## [0.5.0] - 2026-09-07

### Added

- 중앙 `HierarchyTagsManager` 기반 태그 사전 조회
- 프로젝트별 Catalog 데이터 자동 생성 및 Player 자동 로드
- 역직렬화된 태그의 지연 Redirect 해석

### Changed

- 생성 C# Redirect 파일을 숨김 데이터 패키지의 Catalog JSON으로 대체
- 태그와 컨테이너가 최초 사용 시 Redirect 결과를 확정하도록 변경
- 패키지 네임스페이스와 샘플을 `HierarchyTags`로 통일

### Fixed

- 최종 대상이 삭제된 Redirect가 남는 문제
- 데이터 패키지 매니페스트의 줄바꿈 차이 문제
- 데이터 패키지 자동 등록 실패 후 재시도 문제
- 기존 패키지 파일 GUID 호환성

## [0.4.0] - 2026-09-07

### Changed

- 패키지 ID를 `com.deukyeonglee.hierarchytags`로 변경
- 어셈블리와 네임스페이스를 `Deukyeonglee.HierarchyTags` 계열로 변경
- 설정 파일을 `ProjectSettings/HierarchyTagSettings.asset`로 변경
- 프로젝트 코드와 생성 코드의 참조를 새 이름으로 통일

## [0.3.0] - 2026-09-07

### Changed

- 공개 태그 API와 Editor 표시를 `HierarchyTag` 계열 이름으로 변경
- `FName`의 이름 저장·비교·해시 기능을 `HierarchyTag`로 통합
- 동일성 및 Dictionary 해시는 `OrdinalIgnoreCase` 사용, 기존 FNV 해시는 `StableHash`로 제공
- 기존 `name.value` 저장 구조와 스크립트 GUID를 보존하고 `MovedFrom` 지정
- Redirect 생성물은 프로젝트의 `Assets/HierarchyTags.Generated`에 저장하고 `.asmref`로 Runtime에 포함
- 컨테이너는 읽기 전용 뷰를 제공하고 Editor와 Runtime의 목록 정리 규칙을 공유
- 내부 Editor 타입의 공개 범위, 메서드 이름, 코드 포맷 정리
- 기존 이름 저장 형식, Inspector 다중 편집, 읽기 전용 뷰와 생성 코드 회귀 테스트 추가

### Removed

- 독립 `FName` 타입과 중복 테스트
- 패키지 소스 내부의 프로젝트별 Redirect 생성 파일
- 패키지의 필수 Unity Test Framework 의존성

## [0.2.0] - 2026-09-07

### Added

- `UnityTagDefinitions` 코드 선언 수집
- 출처별 등록을 병합하는 읽기 전용 Tag Catalog
- 직접 등록과 계산된 부모 구분
- `UnityTagContainer` 및 계층/정확 일치 조회
- 수동 태그 이름 변경과 Redirect 저장
- 역직렬화 시 Redirect 자동 적용과 컨테이너 중복 정리
- Play 및 Player 빌드 전 Redirect 생성 코드 최신 여부 검사

### Changed

- Editor UI 전체가 코드와 설정을 합친 Catalog를 사용
- 수동 추가·삭제·이름 변경은 다음 Catalog를 검증한 뒤 일괄 저장
- Runtime, Application, Infrastructure, Presentation, Bootstrap 경계를 명시
- 테스트를 Runtime과 Application 책임별로 정리
- 최소 Unity 버전을 실제 검증한 6000.4.7f1로 수정

### Removed

- 코드 전용 `UnityTagRegistry`
- 별도 `UnityTagTree`
- 중복된 Editor 수집·표시 구현
- 프로젝트 설정에 의존하는 테스트와 의미 없는 Smoke 테스트

## [0.1.0] - 2026-08-20

### Added

- 직렬화 가능한 대소문자 비구분 `FName`
- 점으로 계층을 구분하는 `UnityTag`
- `UnityTag.None`, 생성, 비교와 부모 검색
- Project Settings 저장과 Inspector 태그 선택
