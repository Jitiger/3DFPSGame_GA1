# TODO

## 미니맵
- [ ] `MinimapCamera`: 확대/축소를 `ZoomIn()` / `ZoomOut()` 함수로 분리하기 (`Assets/02.Scripts/Camera/MinimapCamera.cs`)
- [ ] `MinimapCamera`: `orthographicSize` 최소/최대값 제한하기 (지금은 0 이하로도 줄어듦)
- [ ] `UI_Minimap`: +/- 버튼을 클릭하면 `ZoomIn()` / `ZoomOut()`을 호출하도록 연결하기 (`Assets/02.Scripts/UI/UI_Minimap.cs`)
- [ ] 미니맵에 플레이어와 좀비 아이콘 표시하기

## 플레이어 UI
- [ ] `UI_PlayerState`: `ZombieHealthBar` 방식을 참고해서 체력과 스태미나 UI 정리하기 (`Assets/02.Scripts/UI/UI_PlayerState.cs`)
- [ ] 남은 총알 수와 재장전 상태 UI 추가하기
- [ ] 남은 폭탄 개수와 폭탄 던지는 힘(차징) UI 추가하기

## 총 (`PlayerGunFire`)
- [ ] `GameManager.State`가 `Playing`일 때만 발사와 재장전이 되도록 하기 (지금은 준비/게임오버 중에도 발사됨)
- [ ] 쓰지 않는 `_bloodEffectPrefab`을 좀비에 맞았을 때 쓰거나 지우기
- [ ] `_maxBullet`, `_currentBullet`을 `int`로 바꾸기
- [ ] 총알을 다 쓰면 자동으로 재장전하기
- [ ] 생성한 히트 이펙트가 끝난 뒤 제거되는지 확인하기 (Stop Action: Destroy 또는 풀링)

## 폭탄 (`PlayerBombFire`)
- [ ] 던지는 힘을 `15f` 하드코딩 대신 처음 설정값으로 되돌리기
- [ ] 던지는 힘의 최대값 제한하기

## 좀비 (`Zombie`)
- [ ] `Attack()`: Trace로 전이한 뒤 `return`이 없어서 같은 프레임에 공격까지 하는 문제 고치기
- [ ] Idle / Run / Attack 애니메이션 연결하기 (코드의 `Todo` 주석)
- [ ] 좀비 스포너 만들기 (일정 시간마다 생성, 최대 마리 수 제한)

## 게임 흐름 (`GameManager`)
- [ ] 게임오버 뒤 재시작 기능 만들기
- [ ] 게임오버 때 커서 보이기와 플레이어 입력 막기
- [ ] 점수(좀비 처치 수) 기능 추가하기

## 정리
- [ ] `Assets/02.Scripts/Test/` 예제 스크립트들을 빌드에서 빼거나 지우기
- [ ] `Debug.Log`를 `GameLogger`로 모으거나 정리하기
