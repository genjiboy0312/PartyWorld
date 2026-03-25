# RiskAgent.md

## 1) 목적

`PartyWorld` 프로젝트의 **알려진 리스크/회귀 포인트/릴리즈 전 체크리스트**를 단일 문서로 관리합니다.

**마지막 업데이트**: 2026-03-25

---

## 2) 알려진 리스크 & 체크리스트

- [x] 런타임 코드에서 `UnityEditor` 의존성 제거(빌드 안정성, `RagDollEditor` 가드)
- [x] `ConnectUsingSettings()` 중복 호출 제거(권위 매니저로 단일화)
- [x] 로딩→맵 전환은 멀티 상황에서 `PhotonNetwork.LoadLevel`로 통일
- [x] `Scene_WaitingRoom`을 Build Settings에 추가
- [x] Ready 토글 + 카운트다운 “시작/취소” 프로토타입 추가(`LobbyUIBootstrapper`, `NetworkAuthorityManager`)
- [x] 카운트다운 완료 시 룸 닫기(`IsOpen=false`) 적용
- [x] **전역 매니저 아키텍처 표준화 (안정성 강화)**
- [x] Firebase Auth 콜백 메인스레드 디스패치 + 로그인/회원가입 UI 페이지 전환(LogIn/Join) 정리
- [x] 닉네임 저장/로드: Firebase Auth `DisplayName` 저장/로드 → `DataManager.nickname` + Photon/채팅 표시 반영
- [x] WaitingRoom 채팅: `Room_WaitingRoomChat` JoinOrCreate + QuickPlay 시 LeaveRoom 후 매칭 룸 진입
- [x] Lobby UI: 플레이어 리스트 + 인원수(현재/최대) 텍스트 추가 및 `LobbyUIController` 연동
- [x] Lobby 카운트다운 숫자 팝(스케일) 코루틴 효과 추가(기본 5→1)
- [x] DOTween: 씬 전환 시 destroyed Text 접근 에러 방지(`UITextEffect`, `UITextTypingMotion` Kill 처리)
- [ ] 결과(Result) 화면/로비 복귀 플로우 추가

## 3) 리스크 매트릭스(운영용)

| ID | 항목 | Severity | Trigger(발생 조건) | Mitigation(대응) | Owner | 상태 |
|:--|:--|:--|:--|:--|:--|:--|
| R-001 | 결과 화면/로비 복귀 미완성 | High | 라운드 종료 후 다음 상태 전환 실패 | Result Phase 명시 구현 + 타임아웃 폴백(`Scene_Lobby` 강제 복귀) | TBD | Open |
| R-002 | 마스터 이탈 시 흐름 중단 | High | 카운트다운/로딩 중 MasterClient 변경 | `OnMasterClientSwitched`에서 상태 재평가 및 타이머 재기동 | TBD | Monitoring |
| R-003 | 로딩 게이트 지연/교착 | Medium | 일부 클라 로딩 완료 신호 누락 | 로딩 타임아웃 + 미도착 인원 표시 + 재동기화 | TBD | Monitoring |
| R-004 | 닉네임/표시 불일치 | Medium | Auth DisplayName, DataManager, Photon NickName 불일치 | 로그인 직후 단일 소스(DataManager) 기준 재동기화 | TBD | Monitoring |
| R-005 | 다중 맵에서 GameState 동기화 누락 가능성 | High | 랜덤 맵이 `_mapSceneName` 외 이름으로 로드됨 | `Scene_Map*` 패턴 기반으로 `Playing` 상태 매핑 통일 | TBD | Open |
| R-006 | 플레이어 스폰 책임 주체 불명확 | High | 룸 입장/씬 전환 시 스폰 호출 분산 또는 누락 | `NetworkAuthorityManager` 단일 스폰 정책 문서화 + 코드 고정 | TBD | Open |
| R-007 | 실제 빌드 플로우와 문서 플로우 불일치 | Medium | `Scene_CharacterCreation` 포함 여부가 문서/구현에서 다르게 해석 | 신규/기존 유저 분기 규칙을 Planning에 명시 | TBD | Open |

## 4) Known Issue 재현 템플릿

아래 템플릿으로 이슈를 누적하면 회귀 확인이 쉬워집니다.

```md
### [ISSUE-ID] 제목
- 발견일: YYYY-MM-DD
- Severity: High / Medium / Low
- 환경: Editor/Build, 플랫폼, 브랜치, 커밋(선택)
- 재현 스텝:
  1) ...
  2) ...
  3) ...
- 기대 결과: ...
- 실제 결과: ...
- 로그 위치: `Player.log`, Unity Console, Photon 로그 등
- 임시 우회: ...
- 원인 가설: ...
- 해결 상태: Open / Fixing / Resolved
```
