# NoticeAgent.md

## 1) 목적

`PartyWorld` 프로젝트의 **최근 작업 내역/변경 로그(공지용 요약)**를 누적 관리합니다.

**마지막 업데이트**: 2026-03-27

---

## 2) 최근 작업 내역 (Recent Work History)

### 2026-03-19: 매니저 아키텍처 리팩토링

- **영향 범위:** Scenes=`Scene_Title&Login`, `Scene_Lobby` / Scripts=`DataManager`, `AudioManager`, `UIManager` 외 매니저 계층 / Prefab=전역 매니저 오브젝트(검토)

- **핵심 매니저 구조 표준화:** `DataManager` 및 `AudioManager`를 `GameManager`와 동일한, 자동 생성 및 중복 방지를 보장하는 안정적인 싱글톤 패턴으로 통일하여 구조적 일관성과 안정성을 확보했습니다.
- **기존 우수 구조 확인:** `GameManager` 및 `NetworkAuthorityManager`가 이미 표준적인 싱글톤 패턴을 따르고 있음을 확인하고 별도로 수정하지 않았습니다.
- **레거시 코드 식별 및 처리:** `PhotonManager`, `PlayerCheckingManager`, `PlayManager`를 기능이 중복되거나 비어있는 레거시 코드로 식별하고, 프로젝트의 혼란 방지를 위해 제거를 권장했습니다.
- **씬 전용 매니저 검토:** `UIManager`, `ChatManager`, `UserListManager`를 검토하여 씬에 종속적인 역할에 맞게 설계되었는지 확인했습니다. 이 과정에서 `UIManager`의 코드 안정성을 소폭 개선했습니다.

### 2026-03-24: 로그인/닉네임/채팅/로비 UI 안정화

- **영향 범위:** Scenes=`Scene_Title&Login`, `Scene_WaitingRoom`, `Scene_Lobby` / Scripts=`FirebaseAuthManager`, `LogInSystem`, `NetworkAuthorityManager`, `LobbyUIController` / Prefab=로비 UI 텍스트 구성

- **Auth/로그인 UI 안정화:** `FirebaseAuthManager` 콜백 메인스레드 디스패치, `LogInSystem`에서 Login/Join 페이지 전환 + 입력 검증 + Join Exit 버튼 처리.
- **닉네임 저장/로드:** 회원가입 시 Firebase Auth 프로필 `DisplayName` 저장, 로그인 시 `DataManager.CurrentUserData.nickname`로 로드 → Photon `NickName`/채팅/유저리스트에 반영.
- **WaitingRoom 채팅 룸:** `Room_WaitingRoomChat` 자동 입장으로 WaitingRoom에서도 채팅 가능, QuickPlay 시작 시 `NetworkAuthorityManager`가 기존 룸 Leave 후 매칭 Join.
- **Lobby UI 보강:** `Scene_Lobby`에 플레이어 리스트/인원수 UI 텍스트 추가 및 `LobbyUIController`에서 갱신.
- **카운트다운 효과:** 카운트다운 숫자 텍스트에 “팝(스케일)” 코루틴 효과 추가(기본 5→1).
- **DOTween 예외 제거:** 씬 전환/비활성화 시 UI Text Tween `Kill/DOKill` 처리로 SafeMode 에러 방지.

### 2026-03-25: Plan/Build 진단 기반 문서 보정

- **영향 범위:** Scenes=`Scene_Title&Login`, `Scene_CharacterCreation`, `Scene_WaitingRoom`, `Scene_Map*` / Scripts=`NetworkAuthorityManager`, `GameManager`(정책 기준), 스폰 책임 관련 매니저 / Prefab=플레이어 스폰 오브젝트(정책 확정 예정)

- **빌드 플로우 정합성 보정:** Build Settings 기준으로 `Scene_CharacterCreation` 경유 가능성을 문서 플로우에 반영.
- **우선순위 재정렬:** Result 플로우 완결, 스폰 책임 단일화, 다중 맵 상태 동기화 항목을 P0으로 승격.
- **리스크 확장:** 다중 맵 `GameState` 동기화 누락, 스폰 책임 불명확, 문서/빌드 플로우 불일치 리스크를 신규 등록.
- **의사결정 항목 추가:** CharacterCreation 진입 조건, 스폰 책임 주체를 오픈 질문으로 추가.

### 2026-03-27: 계정/캐릭터 데이터 플로우 및 스폰 파이프라인 보강

- **영향 범위:** Scenes=`Scene_Title&Login`, `Scene_CharacterCreation`, `Scene_WaitingRoom`, `Scene_Lobby`, `Scene_Map*` / Scripts=`LogInSystem`, `FirebaseAuthManager`, `DataManager`, `NetworkAuthorityManager`, `WaitingRoomCharacterPreview`, `LobbyPlayerSpawner` / Prefab=`Assets/Resources/Characters/*`, `CharacterCatalog.asset`

- **가입/로그인 분기 정리:** 로그인 완료 시 `selectedCharacterId` 유무로 `CharacterCreation`/`WaitingRoom` 분기 고정.
- **Firebase 연동 강화:** `users/{uid}/profile` 경로 기준 Save/Load 공통화 및 닉네임 저장 타이밍 보정.
- **닉네임 품질 가드:** `NewPlayer`/빈 닉네임 상태에서 저장 차단 로직 추가.
- **캐릭터 선택 저장:** CharacterCreation 확인 시 `selectedCharacterId`를 UserData/DB에 즉시 반영.
- **WaitingRoom 프리뷰:** 선택된 캐릭터 프리팹을 `PlayerPreview`에 로드하고 로컬 트랜스폼을 0 기준으로 고정.
- **Lobby/Map 스폰:** `NetworkAuthorityManager` 단일 책임으로 캐릭터 ID 기반 프리팹 스폰 처리.
- **카탈로그 자동화:** `CharacterCatalog` 도입 및 `GenJiTools/Character Catalog/Sync|Validate` 메뉴 추가.
- **에디터 메뉴 정리:** 불필요한 `Tools/PartyWorld` 기반 유틸 일부 제거, APR Player 메뉴를 `GenJiTools` 하위로 이동.

---

## 3) 기록 템플릿(권장)

```md
### YYYY-MM-DD: 작업 제목
- 영향 범위: Scenes=`...` / Scripts=`...` / Prefab=`...`
- 핵심 변경 1
- 핵심 변경 2
- 회귀 체크 포인트
```
