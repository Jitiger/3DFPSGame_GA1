# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

Unity 6000.3.22f1로 만드는 3D FPS 학습용 게임이다. 좀비를 총과 폭탄으로 처치하는 1인칭 슈터이며, 수업 진도에 맞춰 기능을 하나씩 덧붙이는 단계로 진행한다.

**답변과 주석, 커밋 메시지는 한국어로 작성한다.**

## 작업 지침

- 요청받지 않았다면 코드를 직접 변경하거나 커밋하지 않는다.
- 요청 범위와 관련 없는 파일은 수정하지 않는다.
- 변경된 코드의 의도와 주변 코드를 함께 확인한다.
- Unity 에디터로 실행 결과를 확인할 수 없으므로, 실행하지 않은 것을 실행했다고 표현하지 않는다.
- 코드 리뷰 시에는 `Docs/CodeReviewGuide.md`의 기준(중요도 분류, 결과 형식, Unity/C# 확인 항목)을 따른다.

## 명령어

이 저장소에는 테스트 프로젝트가 없다. 빌드와 플레이 확인은 Unity 에디터에서 직접 한다.

```bash
# 커밋 전 포매팅 검사 (pre-commit 훅이 실행하는 것과 동일)
dotnet format 3DFPSGame_GA1.sln --verify-no-changes --severity warn --include Assets/02.Scripts/

# 위반 사항 자동 수정
dotnet format 3DFPSGame_GA1.sln --severity warn --include Assets/02.Scripts/

# husky 훅 설치 (저장소를 새로 클론했을 때)
dotnet tool restore && dotnet husky install
```

`.csproj`와 `.sln`은 Unity가 생성하므로 직접 편집하지 않는다.

## 커밋 규칙

commit-msg 훅이 `type: 설명` 형식을 강제한다. 허용 타입은 다음과 같다.

`asset` `build` `chore` `ci` `docs` `feat` `fix` `perf` `refactor` `revert` `style` `test`

예: `feat: 미니맵 카메라`, `asset: 폰트 추가`

## 코드 규칙

`.editorconfig`가 `Assets/02.Scripts/**.cs`에만 적용되며, 위반 시 pre-commit에서 커밋이 막힌다.

- private 필드는 `_camelCase`, private const와 private static readonly는 `PascalCase`, 지역변수는 `camelCase`
- 여는 중괄호는 항상 다음 줄
- 한 줄짜리 조기 리턴은 중괄호 생략 가능, 여러 줄이면 중괄호 필수
- 매직 넘버는 `[SerializeField]` 필드나 `const`로 분리한다. 이 프로젝트는 Inspector에서 조정할 스탯은 `[Header("스탯")]`를 붙인 `[SerializeField] private` 필드로, 바뀌지 않는 값은 `const`로 두는 관례를 쓴다 (`PlayerMove`의 `Gravity`, `MaxJumps` 참고).

## 아키텍처

### 대미지 전달

전투는 전부 `Core/IDamageable` 하나로 연결된다. 공격하는 쪽은 상대가 무엇인지 모른 채 `DamageInfo`(대미지 값, 맞은 지점, 표면 법선)를 넘긴다.

```csharp
hitInfo.collider.GetComponent<IDamageable>()?.TakeDamage(damageInfo);
```

`Player`, `Zombie`, `Drum`, `Box`가 이 인터페이스를 구현한다. `HitPoint`와 `HitNormal`은 피격 측에서 혈흔·폭발 이펙트를 맞은 자리에 맞는 각도로 생성하는 데 쓴다. 새로운 공격 수단이나 피격 대상을 추가할 때 이 경로를 벗어나지 않도록 한다.

`Drum`은 `Physics.OverlapSphere`로 주변의 `Drum`, `Zombie` 레이어를 찾아 연쇄 폭발을 일으킨다. 폭발에 반응해야 할 오브젝트를 추가하려면 해당 LayerMask도 함께 수정해야 한다.

### 게임 상태

`GameManager`는 `Instance` 싱글턴이며 `GameState`(Ready / Playing / GameOver)를 들고 있다. 플레이어 입력과 좀비 AI는 각자 `Update()` 첫 줄에서 다음을 검사해 Ready와 GameOver 동안 멈춘다.

```csharp
if (GameManager.Instance.State != GameState.Playing) return;
```

새로 만드는 런타임 동작에도 이 가드를 붙인다. 다만 `PlayerGunFire`에는 아직 이 검사가 없다(`TODO.md` 참고).

### 플레이어 구성

플레이어는 역할별 컴포넌트로 나뉘어 한 GameObject에 붙는다.

| 컴포넌트 | 역할 |
| --- | --- |
| `PlayerMove` | `CharacterController` 이동, 중력, 2단 점프, 스태미나 소비·회복 |
| `PlayerRotate` | 마우스 좌우 회전 |
| `PlayerGunFire` | 레이캐스트 사격, 연사 간격, 코루틴 재장전 |
| `PlayerBombFire` | 우클릭 차징 투척, 폭탄 오브젝트 풀 |
| `Player` | 체력과 사망 처리만 담당 |

스태미나는 `PlayerMove`가, 체력은 `Player`가 소유한다. UI는 두 컴포넌트를 모두 참조해서 읽는다.

### UI

`UI_*` 컴포넌트는 상태를 소유하지 않고 `Update()`/`LateUpdate()`에서 게임 오브젝트의 값을 읽어 표시만 한다(`UI_PlayerState`, `ZombieHealthBar`). 월드 공간 UI는 `ZombieHealthBar`처럼 카메라 방향으로 회전시키는 빌보드 방식을 쓴다.

미니맵은 `MinimapCamera`가 플레이어 머리 위에서 내려다보며 RenderTexture에 그리고, 마스크로 둥글게 잘라 표시한다.

### 좀비 AI

`Zombie`는 `ZombieState` 열거형(Idle / Trace / Comeback / Attack / Hit / Death / Patrol)과 `Update()`의 `switch`로 구성된 FSM이다. 상태 전이는 `_state`에 대입하는 방식이고, 전이할 때마다 `Debug.Log($"{_state} -> 새상태")`를 남기는 관례가 있다. Hit과 Death는 코루틴으로 처리하며, `TakeDamage()`에서 `StopAllCoroutines()`를 호출해 진행 중이던 코루틴을 끊는다.

### 로깅

`GameLogger.Log()`는 `[Conditional("ENABLE_LOG")]`가 걸려 있어 해당 심볼이 정의된 빌드에서만 출력된다. 다만 현재 코드 대부분은 아직 `Debug.Log`를 직접 쓰고 있다.

## 디렉터리

- `Assets/02.Scripts/` — 게임 코드. 포매팅과 네이밍 규칙이 적용되는 유일한 범위
- `Assets/02.Scripts/Test/` — 수업용 C# 문법 예제(코루틴, 제네릭, 컬렉션). 게임 로직과 무관하므로 리팩터링 대상에 넣지 않는다
- `Docs/CodeReviewGuide.md` — 코드 리뷰 기준
- `TODO.md` — 남은 작업 목록
