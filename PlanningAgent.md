# PlanningAgent.md

## 1) 목적

`PartyWorld` 프로젝트(참고: Party Animals / Fall Guys)의 **8인(추후 확장) 3D 1인 생존** 파티 플랫폼 게임을 기준으로,
현재 코드/씬 구조를 바탕으로 한 **플로우(시나리오) + 구현 우선순위 + 리스크**를 정리합니다.

**마지막 업데이트**: 2026-03-13

---

## 2) 현재 확인된 프로젝트 컨텍스트(요약)

### 2.1 엔진/패키지

- Unity Editor: `6000.3.10f1` (`ProjectSettings/ProjectVersion.txt`)
- 멀티플레이: Photon PUN 2 사용 흔적 다수 (`Assets/GameCode/...`)

### 2.2 빌드 씬 흐름(현재 등록)

`Scene_Title&Login → Scene_WaitingRoom → Scene_Lobby → Scene_Loading → Scene_MapXX`

(`ProjectSettings/EditorBuildSettings.asset`)

### 2.3 코드 구조(관찰)

- 매니저: `Assets/GameCode/Code_Manager/*`
- 단일 네트워크 권위 매니저(신규): `Assets/GameCode/Code_Manager/NetworkAuthorityManager.cs`
- 플레이어(이동/동기화): `Assets/GameCode/Code_Player/PlayerPresenter.cs` (PUN `IPunObservable`로 위치/회전 동기화)
- 로그인: `Assets/GameCode/Code_System/LogInSystem.cs` + `FirebaseAuthManager`(비 MonoBehaviour 싱글톤)

### 2.4 최근 코드 정리(요약)

- Photon 연결/매칭/씬 전환을 `NetworkAuthorityManager`로 단일화
- `ChatManager`는 더 이상 Photon 접속/룸 입장을 트리거하지 않고, 룸 내에서 채팅 표시/RPC만 담당
- `PhotonManager`, `PlayerCheckingManager`는 레거시 플로우로 격리(`_useLegacyFlow=false` 기본)
- `GameManager`는 씬 기반으로 상태를 동기화하도록 변경(`SetGameState` 공개, 자동 `StartGame()` 제거)
- `Assets/GameCode/Code_System/RagDollEditor.cs`는 `#if UNITY_EDITOR`로 에디터 전용 가드 추가(빌드 안정성)
- 랜덤 맵은 룸 프로퍼티 `selectedMap`으로 고정 후 로드(`Scene_Map*` 자동 수집 또는 인스펙터 지정 리스트)
- 디버그 매칭 옵션 추가(파이어베이스 없이 테스트): `Tools/PartyWorld/Debug/*`

---

## 3) 목표(단기/중기)

### 3.1 단기 목표(MVP: “8인이 한 라운드를 끝까지 완주”)

- 로그인 → 로비 → 매칭(룸 입장) → 로딩 게이트(동기화) → 맵 시작 → 탈락 판정 → 1명 생존 → 결과 → 로비 복귀
- 네트워크/상태 흐름이 “한 군데”에서 결정되고, UI/기능은 구독 형태로 동작

### 3.2 중기 목표(리텐션/확장)

- 라운드 모디파이어(매판 1개)로 리플레이성 확보
- 관전/투표/짧은 메타 진행(매치 한정 패시브/토큰 등)
- 스트레스 테스트 후 8→16/32 확장 검토

---

## 4) 권장 씬/상태 시나리오(Planning)

아래는 “**권위(Authority) 1개**” 원칙으로 정리한 추천 플로우입니다.

### 4.1 Title&Login (`Scene_Title&Login`)

1. Firebase 로그인/회원가입 처리 (`LogInSystem`, `FirebaseAuthManager`)
2. 로그인 성공 시:
   - `PhotonNetwork.NickName`에 사용자 식별자(이메일/닉네임)를 설정
   - `Scene_WaitingRoom`로 이동

주의:
- Photon 접속은 Title에서 “선택적으로” 시작 가능하지만, 연결 책임 주체는 단일 매니저로 고정(아래 5장).

### 4.2 WaitingRoom (`Scene_WaitingRoom`)

목표: 글로벌 로비(메뉴/파티/퀵플레이 진입점). “룸에 들어가기 전” 공간.

권장 동작:
1. 로그인 후 이 씬으로 이동
2. `Play(QuickPlay)` 클릭 시 랜덤 매칭을 시작
   - `JoinRandomRoom` → 실패 시 `CreateRoom`
3. 룸에 들어가면 자동으로 `Scene_Lobby`로 이동(룸 로비로 합류)

### 4.3 Lobby (`Scene_Lobby`)

로비 목표: **룸 로비 + 미니게임(점프/장난)** + Ready + 카운트다운 + 게임 시작 합의.

권장 동작:
1. 룸 참가 완료 상태에서 시작(“단일 네트워크 매니저”가 관리)
2. 로비 씬은 “대기 공간”으로 동작:
   - 플레이어는 점프/이동/장난(연습) 가능
   - UI는 현재 인원/Ready 상태/채팅/유저리스트를 표시
3. Ready 버튼(권장: 토글):
   - 각 플레이어가 `Player.CustomProperties`에 `ready=true/false` 설정
   - 마스터가 모두 Ready면 **카운트다운(예: 3초)** 시작
   - 누군가 Ready 해제/퇴장 시 카운트다운 취소
4. 카운트다운 완료 시:
   - 마스터가 랜덤 맵을 1개 선택해 룸 프로퍼티에 고정(`selectedMap`)
   - 필요 시 룸을 닫음(`IsOpen=false`)으로 난입 방지
   - `Scene_Loading`으로 전환

현재 연결된 프로토타입 경로:
- `Scene_WaitingRoom`의 `UIPlay` 버튼은 `NetworkAuthorityManager.StartQuickPlay()`로 매칭을 시작
- 룸 입장 후 `Scene_Lobby`로 이동 → Ready/카운트다운/로딩 전환 UX는 후속 작업

### 4.4 Loading (`Scene_Loading`)

로딩 목표: 씬 전환의 “동기화 게이트” (실제 로딩 UI + 모든 플레이어 준비 완료 확인).

권장 동작:
1. 로딩 UI 표시(텍스트 타이핑, 진행률 등)
2. 마스터가 Ready 조건 만족 시:
   - `PhotonNetwork.AutomaticallySyncScene=true` 전제하에
   - 모든 플레이어가 Loading 씬에 도착하면, 선택된 맵(`selectedMap`)으로 `PhotonNetwork.LoadLevel(...)` 호출

### 4.5 MapXX (`Scene_MapXX`) — 라운드(1인 생존)

라운드 목표: “재밌는 물리 이동 + 간단 명확한 승패”.

권장 동작:
1. 스폰:
   - SpawnPoint 목록 기반으로 겹침 방지
   - 플레이어 프리팹 생성은 네트워크 매니저 또는 룸 입장 콜백에서 일원화
2. 진행:
   - 라운드 타이머(선택)
   - 탈락 판정(예: Y < threshold, 지정 트리거, 물/용암 등)
3. 종료:
   - 생존자 수가 1명 → 결과(승자 표시)
   - 다음 행동은 2안 중 택1
     - (권장) 같은 룸 유지 → `Scene_Lobby`로 복귀 → Ready로 다음 판
     - 룸 해산/나가기 → `Scene_WaitingRoom`로 복귀
4. 관전:
   - 탈락자는 즉시 관전 카메라로 전환(다음 라운드 투표/응원 기능 확장 포인트)

---

## 5) 핵심 시스템 설계 원칙(권장)

### 5.1 네트워크 권위(Authority) “단일화”

현재 코드에서는 여러 컴포넌트가 각각 `ConnectUsingSettings()`/룸 입장을 시도할 여지가 있습니다.
권장 원칙:

- 연결/로비/룸 입장/씬 전환은 **오직 `NetworkAuthorityManager`** 에서만 수행
- `ChatManager`, `UserListManager`, 로딩 UI 등은
  - “연결/룸 상태(이미 존재)”만 가정하고
  - 구독/표시/RPC 호출만 수행

### 5.2 Ready 체크는 “카운트”보다 “속성 기반”

로딩/준비는 `LocalActorNumber` 인덱스 기반 리스트 + 로컬 카운트 증감 방식보다,
`Player.CustomProperties(ready)` 기반으로 판단하는 편이 안전합니다.

- 플레이어 입/퇴장, 재접속, 지연에 강함
- 마스터 기준으로 항상 “현재 상태 스냅샷” 계산 가능

### 5.3 씬 전환 방식 통일

- 멀티플레이 중 씬 전환은 `PhotonNetwork.LoadLevel(...)`로 통일(자동 씬 동기화 전제)
- 싱글 씬 로드(`SceneManager.LoadScene`)는 로컬 UI/싱글 상황에 한정

### 5.4 라운드 모디파이어(리플레이성)

매 라운드 시작 시 마스터가 하나를 선택하고 룸 프로퍼티로 배포:
- 저중력 / 강풍 / 미끄럼 / 점프 증가 / 시야 제한 등

적용 방식 예:
- 룸 프로퍼티: `rule=LowGravity`
- 각 클라이언트: 라운드 시작 시 룰 값을 읽고 `PlayerPresenter` 이동 파라미터에 반영

---

## 6) 구현 우선순위(추천 백로그)

### P0 (완주 가능한 MVP)

1. Photon 연결/룸/씬 전환 책임을 “단일 매니저”로 정리(중복 접속 제거)
2. Lobby Ready → Loading Gate → Map01 Start까지의 흐름을 속성 기반으로 확정
3. Map01에서 탈락 판정/승자 결정/결과 처리

### P1 (재미 강화)

1. 라운드 모디파이어 5종(맵 재사용 가치 증가)
2. 관전 모드 + 다음 라운드 모디파이어 투표
3. 간단한 역전 장치(소프트 밸런스): 뒤처짐 토큰/선두 위험 증가 등

### P2 (운영/확장)

1. 스트레스 테스트(8→16/32) + SendRate/SerializationRate 튜닝
2. 리플레이/하이라이트(짧은 클립 저장 또는 서버 로그 기반)
3. 래그/물리 악용 방지(입력 제한, 잡기/충돌 스팸 완화 등)

---

## 7) 알려진 리스크 & 체크리스트

- [x] 런타임 코드에서 `UnityEditor` 의존성 제거(빌드 안정성, `RagDollEditor` 가드)
- [x] `ConnectUsingSettings()` 중복 호출 제거(권위 매니저로 단일화)
- [x] 로딩→맵 전환은 멀티 상황에서 `PhotonNetwork.LoadLevel`로 통일
- [x] `Scene_WaitingRoom`을 Build Settings에 추가
- [x] Ready 토글 + 카운트다운 “시작/취소” 프로토타입 추가(`LobbyUIBootstrapper`, `NetworkAuthorityManager`)
- [x] 카운트다운 완료 시 룸 닫기(`IsOpen=false`) 적용
- [ ] 결과(Result) 화면/로비 복귀 플로우 추가

---

## 8) 오픈 질문(결정되면 계획 업데이트)

1. 라운드 기본 장르는 무엇에 더 가깝나?
   - 장애물 레이싱 중심 / 물리 격투(잡기) 중심 / 혼합
2. 승리 조건:
   - 마지막 1인 생존 고정? 또는 점수제 라운드(복수 라운드)도 계획?
3. 모바일 조작/카메라 UX:
   - 조이스틱 + 버튼(점프/다이브/잡기) 고정? 자동 카메라 보조 필요?
