# NoticeAgent.md

## 1) 목적

`PartyWorld` Unity 프로젝트의 **변경 로그, 최근 작업 내역, 공지 사항**을 기록합니다.

**마지막 업데이트**: 2026-07-03

---

## 2) 작업 내역 가이드

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
- **카운트다운 효과:** 카운트다운 숫자 텍스트에 "팝(스케일)" 코루틴 효과 추가(기본 5→1).
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
- **캐릭터 선택 즉시 저장:** 캐릭터 클릭(선택) 시 바로 `selectedCharacterId`를 `DataManager.CurrentUserData` 및 `PlayerPrefs`에 저장 (앱 종료/재실행 시에도 선택 상태 유지).
- **WaitingRoom 프리뷰:** 선택된 캐릭터 프리팹을 `PlayerPreview`에 로드하고 로컬 트랜스폼을 0 기준으로 고정.
- **Lobby/Map 스폰:** `NetworkAuthorityManager` 단일 책임으로 캐릭터 ID 기반 프리팹 스폰 처리.
- **카탈로그 자동화:** `CharacterCatalog` 도입 및 `GenJiTools/Character Catalog/Sync|Validate` 메뉴 추가.
- **에디터 메뉴 정리:** 불필요한 `Tools/PartyWorld` 기반 유틸 일부 제거, APR Player 메뉴를 `GenJiTools` 하위로 이동.

### 2026-03-30: Save/Load 공통 함수 async/await 개선 및 테스트 가이드 추가

- **영향 범위:** Scripts=`DataManager` / Documents=`MYAIAgent/TestGuide.md`

- **async/await 공통 함수 추가:** `DataManager`에 `SaveUserDataToFirebaseAsync()` 및 `LoadUserDataFromFirebaseAsync()` 메서드 추가 (기존 콜백 기반 메서드와 병행 유지).
- **Task<bool> 반환 지원:** 비동기 작업 결과를 `await` 방식으로 처리 가능, 코드 가독성 및 오류 처리 개선.
- **테스트 가이드 추가:** `MYAIAgent/TestGuide.md` 생성 및 3가지 테스트 시나리오(신규/기존/재실행) 정의.

### 2026-04-01: Hex-A-Gone 시스템 보강 및 씬 관리 방식 개편

- **영향 범위:** Scripts=`HexTile`, `HexArenaManager`, `NetworkAuthorityManager`, `GameManager` / Documents=`MYAIAgent/PlanningAgent.md` / Prefab=`HexTile.prefab`

- **HexTile 프리팹 형태 유지:** `SetupFromPrefab()`, `CreateSolidCylinderMesh()` 등 메쉬 조작 코드 제거, 원본 프리팹 형태 그대로 유지.
- **타일 네트워크 동기화:** `tileIndex` 기반 RPC 시스템 (`RPC_TileDamaged`, `RPC_TileSunk`, `RPC_SyncAllTileStates`) 추가.
- **HexTile 컴포넌트 추가:** `SetTileIndex()`, `ApplyNetworkState()`, `NotifySunk()` 메서드 추가.
- **SceneReference 방식 도입:** 씬 이름 타이핑 오류 방지 위해 `SceneReference` 래퍼 클래스 생성 (드래그 앤 드롭 지원).
- **NetworkAuthorityManager 개편:** `_playMapScenes` 리스트로 게임 맵 수동 관리.
- **GameManager 개편:** `_mapScenes` 리스트로 맵 씬 관리.
- **생성된 파일:** `Assets/GameCode/SceneReference.cs`

---

## 작업 후 아래 형식으로 기록합니다.

```md
### YYYY-MM-DD - [유형: Feat/Fix/Refactor/Chore]
- 변경 내용: (핵심 변경 사항 요약)
- 영향 범위: (영향받은 파일/모듈)
- 관련 이슈: (해당되는 경우)
```

---

## 3) 변경 이력

### 2026-06-13 - [Chore: MyAIAgent 문서 체계 정리]
- **변경 내용**:
  - 프로젝트 방향성을 Unity + 모바일로 확정
  - 웹 프로젝트(BIM/GIS Building Editor) 관련 문서 10개 삭제
  - `README.md` 재작성 (Unity + 모바일 중심)
  - `RiskAgent.md` 재작성 (Unity/모바일 리스크 중심)
  - `MyAIAgent_Analysis_20260613.md` 종합 분석 문서 추가
- **영향 범위**: MyAIAgent/ 문서 전반
- **테스트 완료**: 해당 없음 (문서 작업)

---

### 2026-06-18 - [Refactor: Controller.cs 분리 (JoyStick/CameraStick)]
- **변경 내용**:
  - `Controller.cs` 하나로 두 스틱(BackGround_JoyStick, BackGround_CameraStick)을 처리하던 구조를 분리
  - `JoyStickController.cs` 생성: 이동 전용, 버튼 참조 제거
  - `CameraStickController.cs` 생성: eventData.delta 기반 카메라 제어, sensitivity 필드
  - `PlayerPresenter.cs` 수정: Controller → JoyStickController/CameraStickController 타입 변경, 이동 방향 Camera-Relative로 수정
  - `UI_PlayerController.prefab` 수정: 새 스크립트 할당
- **영향 범위**: Controller.cs, JoyStickController.cs, CameraStickController.cs, PlayerPresenter.cs, UI_PlayerController.prefab
- **관련 이슈**: 두 스틱 방향 상충 문제 해결, 이동 방향이 카메라 시점 기준으로 동작하도록 개선
### 2026-04-13 - [Fix: 워크플로우 스텝 건너뛰기] ← 레거시 기록 (이전 프로젝트 컨텍스트)

과거 BIM/GIS 웹 에디터 프로젝트의 작업 기록입니다. Unity 프로젝트와 무관하며 참고용으로 보관합니다.

---

## 4) 현재 작업 상태

| 우선순위 | 작업 | 상태 |
|| 🔴 P0 | MyAIAgent 문서 체계 Unity/모바일로 정리 | ✅ 완료 |
|| 🟡 P1 | UnityAgent.md 업데이트 (최신 코드 반영) | ⏳ 대기 |
|| 🟡 P1 | 모바일 최적화 (성능/해상도/터치 입력) | ⏳ 계획 수립 전 |
|| 🟢 P2 | 네트워크 안정성 개선 | ⏳ 계획 수립 전 |
|| 🟡 P1 | Controller.cs 분리 리팩터링 (JoyStick/CameraStick + Camera-Relative) | ✅ 완료 |

### 2026-07-03 - [Fix: ReflectorNet 5.1.2 업데이트 + JSON Schema $defs URI 오류 수정]

---
#### 1) 진단 요약 (GPT 재접속 시 꼭 읽을 것)

| 항목 | 내용 |
|------|------|
| **증상** | GPT가 "MCP 도구 사용 불가" 오류 발생. `tool-list`는 뜨지만 실제 도구 호출 시 schema validation 실패 |
| **원인** | NuGet auto-update: `ReflectorNet 5.0.0` → `5.1.1`. 5.1.1부터 `System.String[]` 타입을 JSON Schema `$defs` 키로 사용 →
|
`#/$defs/System.String[]`는 유효한 uri-reference가 아님 (`[` `]` 문자 포함) → MCP 스키마 전체 손상
| **해결** | `ReflectorNet 5.1.2` (NuGet.org) DLL을 다운로드해 5.1.1 폴더에 덮어쓰기
| **5.1.2 fix** | `"#77: sanitize JSON Schema $defs keys to produce valid $ref URI references"` — 정확히 이 버그 수정
| **검증** | Unity 컴파일 성공, `tool-list`에 MCP 도구 60개 전부 정상 등록 |

---
#### 2) ReflectorNet 버전별 행동 (GPT가 판단할 때 반드시 알아야 할 사실)

| 버전 | 상태 | 이유 |
|------|------|------|
| ❌ **5.0.0** | 사용 불가 | `ViewQuery` 클래스 없음 → McpPlugin 6.1.3 소스코드 6개 컴파일 에러 |
| ❌ **5.1.1** | 사용 불가 | JSON Schema `$defs` 키에 `[]` 포함 → uri-reference 오류 |
| ✅ **5.1.2** | 현재 사용 중 | ViewQuery 있음 + `$defs` sanitize 있음 → 정상 작동 |
| ✅ **5.3.1** | 최신 버전 | NuGet에서 확인됨, 업그레이드 가능하나 현재로서는 불필요 |

---
#### 3) 절대 하지 말것 (GPT Constraint)

1. **MCP 소스 코드를 건드리지 말것** (`Packages/com.ivanmurzak.unity.mcp/`)",
   - 사용자: "너 mcp 업데이트 건드리지마"
   - MCP 코드는 문제없음. 문제는 ReflectorNet NuGet 패키지 버전임
2. **ReflectorNet을 5.0.0으로 rollback 하지 말것**
   - 롤백 시도했으나 ViewQuery 미존재로 McpPlugin 6.1.3이 컴파일 불가
   - 정답: 5.1.2+ 로 업데이트 (5.0.0과 5.1.2 사이만 피하면 됨)
3. **`.bak` 파일을 Unity가 임포트하게 두지 말것**
   - NuGet 폴더 내 `.bak` 파일이 있으면 Unity가 어셈블리로 인식해 임포트 시도
   - 발견 즉시 삭제할 것

---
#### 4) 재발생 시 대처법 (같은 문제가 다시 나타나면)

```
1. Unity Editor Console 로그 확인 → JSON Schema validation 에러 검색
2. Assets/Plugins/NuGet/com.IvanMurzak.ReflectorNet.5.1.1/ReflectorNet.dll 의 Assembly Version 확인
   (5.1.2여야 정상. 만약 NuGet이 다시 auto-update해서 5.1.1로 바뀌었으면 재발)
3. NuGet.org에서 com.IvanMurzak.ReflectorNet 최신 버전 확인
4. 최신 버전 DLL을 위 경로에 덮어쓰거나, 5.1.2 이상으로 유지
5. Unity 재컴파일 후 tool-list로 60개 도구 정상 등록 확인
```

---
#### 5) 중요 파일 경로 (GPT가 다음 세션에서 찾을 위치)

| 파일 | 경로 | 비고 |
|------|------|------|
| ReflectorNet DLL | `Assets/Plugins/NuGet/com.IvanMurzak.ReflectorNet.5.1.1/ReflectorNet.dll` | 폴더명은 5.1.1이지만 DLL Assembly Version은 5.1.2 |
| DLL meta | `Assets/Plugins/NuGet/com.IvanMurzak.ReflectorNet.5.1.1/ReflectorNet.dll.meta` | 변경 없음 (594 bytes) |
| .gitignore | `.gitignore` | `Assets/_Recovery/` 추가됨 |

---
#### 6) Git 커밋 내역 (2026-07-03)

| 커밋 | 설명 | 파일 |
|------|------|------|
| `022948e` | `chore: opencode 스킬 문서 CRLF 및 설명 업데이트` | 35 skill files |
| `45b0983` | `feat: 게임 네트워크/UI 코드 업데이트 및 신규 스크립트 추가` | 7 game code files |
| `5683a16` | `feat: Scene_Lobby, Scene_WaitingRoom 씬 업데이트` | 2 scenes |
| `bce361e` | `fix: ReflectorNet 5.1.2 update - JSON Schema $defs key sanitize` | 1 DLL |
| `05b2400` | `chore: .gitignore에 Assets/_Recovery/ 추가` | 1 .gitignore |