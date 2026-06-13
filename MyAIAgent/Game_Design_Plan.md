# PartyWorld 게임 기획 — 멀티플레이 플랫폼 파티 게임

> **작성일**: 2026-06-13
> **기반 코드 분석**: MyAIAgent_Analysis_20260613.md

---

## 1. 현재 상태 분석

PartyWorld는 현재 **Unity 모바일 멀티플레이 플랫폼 파티 게임 프로토타입**입니다. 인프라 계층은 완성에 가깝지만, 실제 **게임플레이 규칙은 전무**한 상태입니다.

### 갖춰진 것 (인프라)
| 계층 | 상태 | 설명 |
|------|------|------|
| Firebase 로그인/회원가입 | 완료 | 이메일/비밀번호 인증 |
| 캐릭터 선택 및 저장 | 완료 | PlayerPrefs + Firebase DB |
| Photon PUN2 매치메이킹 | 완료 | QuickPlay → JoinRandomOrCreateRoom |
| 로비/레디/카운트다운 | 완료 | 마스터 클라이언트 권한, 마스터 이탈 처리 |
| 동기화된 로딩 게이트 | 완료 | 전체 플레이어 로딩 완료 후 맵 전환 |
| 맵 랜덤 선택 | 완료 | Scene_Map* 빌드 씬에서 랜덤 |
| 네트워크 캐릭터 스폰 | 완료 | PhotonNetwork.Instantiate |
| 모바일 최적화 | 완료 | PlatformQualityManager, SendRate=20 |
| MVP 패턴 (Model/View/Presenter) | 완료 | PlayerPresenter+PlayerModel+PlayerView |
| 물리 기반 이동/점프 | 완료 | Rigidbody + ConfigurableJoint |
| 래그돌 헤드/물리감 | 완료 | RagdollHeadWeightController |

### 부족한 것 (게임플레이)
| 계층 | 상태 | 설명 |
|------|------|------|
| **미니게임 규칙** | **없음** | 스코어, 승패, 라운드, 목표 조건 전무 |
| **Round 시스템** | **없음** | 라운드 진행, 최종 결과 연출 부재 |
| **결과 화면** | **프로토타입** | Scene_Result 생성, UI 레이아웃(RESULT 타이틀/스코어 영역) 구성, RoundManager의 `_resultSceneName`과 연결됨 |
| **낙사/제거 판정** | **없음** | 플레이어가 맵 밖으로 떨어지는 감지 없음 |
| **결승선/목표 도달** | **없음** | 레이스/서바이벌 종료 조건 없음 |
| **잡기/던지기** | **없음** | Party Animals 핵심 메커닉 미구현 |
| **장애물 시스템** | **없음** | 움직이는 발판, 해머, 공 굴러가는 등 |
| **아이템/파워업** | **없음** | 게임플레이 영향 아이템 없음 |
| **다양한 맵** | **부족** | Scene_Map01 1개만 존재 |
| **관전 모드** | **없음** | 제거된 플레이어의 관전 처리 없음 |
| **코스메틱/상점** | **없음** | Item 데이터 클래스만 있고 동작 없음 |

---

## 2. 게임 비전

### 장르: 모바일 물리 기반 멀티플레이 플랫폼 파티 게임

**영감 게임**:
- **Party Animals**: 물리 기반 잡기/던지기 격투 → **터치 컨트롤에 최적화**
- **Gang Beasts**: 물리 엔진 기반 난투 → **모바일 12인 멀티플레이**

### 타겟 플레이어
- 1라운드 2~3분, 짧은 세션에 집중
- 모바일 친화적 터치 조작 (조이스틱 이동 + 1~2개 액션 버튼)
- 캐주얼 + 경쟁의 적절한 균형
- 12인 로비 → 4인×3팀 또는 개인전

### 핵심 재미
1. **떨어지는 재미**: 플랫폼에서 밀려 떨어지고, 다시 올라가고
2. **잡고 던지는 재미**: 상대를 잡아서 낙사시키기
3. **혼돈의 재미**: 예측 불가능한 물리 상호작용
4. **짧고 굵은 라운드**: 빠른 리스폰과 반복 플레이

---

## 3. 게임 플로우

```
[로그인] → [캐릭터 선택] → [대기실] → [퀵 매치]
                                              ↓
                                         [로비/레디]
                                              ↓
                                     [로딩 게이트 (전체 동기)]
                                              ↓
                                     ┌─────────────────────┐
                                     │  라운드 1                │ ← 4라운드 반복
                                     │  └ 결과/스코어 누적  │
                                     │  라운드 2                │
                                     │  └ 결과/스코어 누적  │
                                     │  라운드 3                │
                                     │  └ 결과/스코어 누적  │
                                     │  라운드 4 (파이널)   │
                                     │  └ 최종 순위 결정    │
                                     └─────────────────────┘
                                              ↓
                                         [결과 화면]
                                              ↓
                                     [로비 → 다음 게임]
```

---

## 4. 라운드 시스템 인프라 (Phase 1)

라운드 진행을 위한 프레임워크. 실제 게임 로직의 기본 골격.

### 4.1 RoundManager

**책임**: 라운드 생명주기 관리 (RoundManager.cs)

| 메서드 | 설명 |
|--------|------|
| `StartRound()` | 라운드 시작, 타이머 가동, 참가자 초기화 |
| `EndRound()` | 라운드 종료, 스코어 계산, 다음 라운드로 |
| `OnPlayerEliminated(Player)` | 플레이어 제거 처리 (서바이벌/팀전) |
| `OnPlayerFinished(Player)` | 플레이어 골인 처리 (레이스) |
| `GetRoundResults()` | 현재 라운드 순위/스코어 반환 |
| `IsRoundOver()` | 라운드 종료 조건 확인 |
| `GetRemainingTime()` | 남은 시간 |

**RoundState 열거형**: `Intro`, `Playing`, `Countdown`, `Finished`, `Intermission`

**설계 방향**:
- 마스터 클라이언트가 RoundOwner
- `PhotonView.RPC`로 라운드 시작/종료 동기화
- 각 플레이어 로컬에서 종료 조건 감지 → 마스터에게 RPC 전송

### 4.2 Objective/WinCondition 시스템

```csharp
public abstract class WinCondition : MonoBehaviour
{
    public abstract bool CheckCompleted(PlayerModel player);
    public abstract int CalculateScore(PlayerModel player);
    public abstract PlayerRanking[] GetRankings();
}
```

**구체적인 WinCondition 구현체**:
| 클래스 | 게임 모드 | 설명 |
|--------|-----------|------|
| `RaceWinCondition` | 레이스 | 결승선 도달 순서대로 순위 |
| `SurvivalWinCondition` | 서바이벌 | 마지막까지 생존한 플레이어 |
| `ScoreWinCondition` | 포인트전 | 가장 높은 점수 획득한 플레이어 |
| `LastManStanding` | 제거전 | 최후의 1인 |
| `TeamScoreWinCondition` | 팀전 | 팀 총점이 높은 팀 승리 |

### 4.3 낙사/제거 시스템 (FallDamageDetector)

- 맵 아래에 `DeathZone` 트리거 콜라이더 배치
- 플레이어가 `DeathZone`에 닿으면:
  1. 서바이벌: 해당 플레이어 제거 (Eliminated)
  2. 레이스: 리스폰 + 시간 패널티
  3. 포인트전: 리스폰 + 점수 차감
- 낙사 이벤트는 RPC로 마스터에게 전송 → 순위 갱신

### 4.4 결승선 시스템 (FinishLineDetector)

- `Trigger` 콜라이더로 결승선 구현
- 통과 순서 기록 (마스터가 수집)
- 1등부터 순위 결정
- 모든 플레이어가 통과하거나 타이머 종료 시 라운드 종료

### 4.5 결과 화면 (ResultScene)

- GameState.Result에 연결할 씬
- 요소:
  - 순위 표시 (1~12위)
  - 스코어 변화 애니메이션
  - "다음 게임" 버튼 (마스터가 로비로 복귀)
  - 하이라이트 리플레이 (옵션)

### 4.6 라운드 진행 데이터 구조

```csharp
[Serializable]
public class RoundData
{
    public int roundNumber;
    public string gameMode;              // "Race", "Survival", "Score", "Team"
    public string mapSceneName;
    public string[] participantIds;       // 라운드 시작 시 참가자
    public float timeLimit;
    public WinConditionConfig winConfig;
}

[Serializable]
public class PlayerRoundResult
{
    public string userId;
    public int rank;
    public int score;
    public bool eliminated;
    public float finishTime;
}

[Serializable]
public class GameSessionData
{
    public int totalRounds = 4;
    public List<RoundData> rounds = new();
    public Dictionary<string, int> cumulativeScores;  // 누적 점수
}
```

---

## 5. 피지컬 메카닉 (Phase 2)

### 5.1 잡기/던지기 (Grab System)

**핵심 설계**: 모바일 싱글 버튼으로 잡기/풀기 전환 + 방향 입력

```
잡기 버튼 터치 → 근처 상대/오브젝트 탐색
  ├─ 없음 → (아무 일도 안 일어남)
  ├─ 상대 → Grabber.Grab(Victim) → 잡힌 상태
  └─ 오브젝트 → Grabber.GrabObject(Object) → 들기

잡힌 상태에서 방향 입력 + 잡기 버튼 떼기 → Throw 방향으로 던짐
```

**구현 클래스**:
| 클래스 | 설명 |
|--------|------|
| `GrabDetector` | 근처 잡기 가능 대상 탐색 (SphereCast) |
| `Grabber` | 잡기/풀기/던지기 상태 머신 |
| `Grabble` | 잡기 가능한 대상 표시 (Player/오브젝트용 인터페이스) |
| `GrabJoint` | ConfigurableJoint로 잡힌 상태 물리 연결 |

**기술적 고려사항**:
- 잡힌 플레이어는 일정 시간 후 자동 풀림 (3초)
- 잡는 쪽에서 잡기 버튼을 떼면 상대를 발사 (방향 적용)
- 점프 중에는 잡기 불가
- 낙사 구역 근처에서 잡기/던지기가 핵심 재미

### 5.2 넉백/스턴 시스템

- 강한 충돌, 잡혔다 풀림, 특정 장애물 → 넉백 상태
- 넉백 = 일정 시간 입력 무시 + Ragdoll 활성화
- 회복: 바닥에 닿으면 즉시 복귀
- 머리 위 찍기: 점프 중 잡힌 상대 위로 → 스턴

### 5.3 물리 상호작용 오브젝트

| 오브젝트 | 동작 |
|----------|------|
| `PushBlock` | 밀 수 있는 상자, 여러 명이 함께 밀 수 있음 |
| `SpringPlatform` | 밟으면 위로 튕겨짐 |
| `BreakableFloor` | 일정 시간/무게 지나면 부서지는 발판 |
| `ConveyorBelt` | 방향 이동 강제 |
| `Fan` | 바람으로 플레이어 밀어냄 |

## 6. 플랫폼 레벨 디자인 (Phase 2)

### 6.1 레이스 레벨 — 장애물 코스

레이스 레벨은 결승선 도달 순서로 순위 결정:
- 12명 중 결승선 도달 순서로 순위 결정
- 낙사 → 리스폰 (선두에서 약간 뒤)
- 시간 제한 3분 → 이후 도달 못한 인원은 하위 순위
- 물리 오브젝트와 장애물을 활용한 코스 디자인

### 6.2 서바이벌 레벨 — 마지막까지 살아남기

서바이벌 레벨은 낙사/제거가 핵심:
- 낙사 = 제거 (Eliminated)
- 마지막까지 생존한 플레이어가 승리
- 맵이 점점 작아지거나 위험 요소 증가
- 잡기/던지기를 활용한 제거 전략

### 6.3 팀전 레벨 — 협력/경쟁

팀전은 팀 단위 목표 달성:
- 팀 구성 랜덤 (라운드마다 변경 옵션)
- 승리한 팀 전원 +1 포인트
- 목표: 팀별 스코어/깃발/특정 조건

### 6.4 파이널 라운드

4라운드는 항상 **파이널**:
1. 최상위 6명만 참가 (누적 점수 기준)
2. 최후의 1인 = **우승자**
3. 나머지는 순위 결정
---

## 7. 완성도 및 편의 기능 (Phase 3)

### 7.1 퀵 이모트/이모지

- 우측 상단 이모트 버튼 → 이모지 4개 선택
- 이모트 전송 (RaiseEvent 권장)
- 화면 상단에 표시 (2초 페이드)

### 7.2 관전 모드 (Spectator)

- 제거된 플레이어는 생존자 중 1명 관전
- 카메라 전환 버튼으로 관전 대상 변경
- 3인칭 따라가기 카메라

### 7.3 코스메틱 시스템

- `Item` 데이터 클래스 확장
- 카테고리: 모자, 의상, 신발, 색상
- 장착 → PlayerView.PresentItem()
- 보상: 게임 종료 후 랜덤 코스메틱 획득
- Firebase DB에 인벤토리 저장

### 7.4 결과 화면 (ResultScene)

- 순위 표시 (1~12위)
- 스코어 변화 애니메이션
- "다음 게임" 버튼 (마스터가 로비로 복귀)
- 현황판 UI 개선 (GameManager.GameState.Result)

## 8. 기술 아키텍처 권장사항

### 8.1 신규 클래스 구조

```
Code_System/
├── RoundManager.cs            ← 라운드 생명주기
├── RoundData.cs               ← 라운드 데이터 모델
├── ScoreManager.cs            ← 스코어/순위 집계
└── WinCondition/
    ├── RaceWinCondition.cs
    ├── SurvivalWinCondition.cs
    └── ScoreWinCondition.cs

Code_Object/
├── DeathZone.cs               ← 낙사 감지 트리거
├── FinishLine.cs              ← 결승선 감지 트리거
├── GrabDetector.cs            ← 잡기 감지
├── Grabber.cs                 ← 잡기/던지기 상태머신
└── IGrabble.cs                ← 잡기 가능 인터페이스

Code_Map/                     ← 신규 폴더 (맵별 스크립트)
├── MovingPlatform.cs
├── RotatingHammer.cs
├── BreakableFloor.cs
├── ConveyorBelt.cs
└── FanWindZone.cs
```

### 8.2 데이터 동기화 전략

| 데이터 | 동기 방식 | 설명 |
|--------|-----------|------|
| 라운드 상태 | RPC | 마스터 → 전체 브로드캐스트 |
| 플레이어 순위 | RPC + 로컬 | 마스터가 최종 순위 전파 |
| 플레이어 제거 | RPC | 마스터에게 제거 알림 → 결과 집계 |
| 잡기 상태 | PhotonView (Owner) | 잡는 쪽에서 동기화 |
| 이동 | IPunObservable | 지속 스트리밍 (기존 유지) |
| 오브젝트 물리 | PhotonTransformView | 움직이는 장애물 동기화 |

### 8.3 모바일 최적화 주의사항

- 물리 장애물은 최대 20개로 제한
- 동시 플레이어 12명 제한 (Photon MaxPlayers=12)
- 파티클/이펙트는 LOD 시스템 적용
- Net SendRate=20 유지
- Ragdoll은 제거 시에만 활성화 (상시 비활성)

---

## 9. 단계별 로드맵

### Stage 1: 라운드 인프라 (1-2주)
- [ ] RoundManager 구현 (라운드/타이머/종료)
- [ ] WinCondition 추상 클래스 + RaceWinCondition
- [ ] DeathZone + FinishLine 트리거
- [ ] 게임 세션 데이터 (라운드 정보, 누적 스코어)
- [x] GameState.Result 씬 연결

### Stage 2: 물리 메카닉 (2주)
- [ ] Grab/Grabble 시스템 (잡기/풀기/던지기)
- [ ] 넉백/스턴 처리
- [ ] 물리 상호작용 오브젝트 (PushBlock, SpringPlatform)
- [ ] 잡기 상태 네트워크 동기화

### Stage 3: 완성도 (2주)
- [ ] 관전 모드
- [ ] 이모트/이모지
- [ ] 결과 화면 애니메이션
- [ ] 코스메틱 장착/표시
- [ ] 종합 QA / 성능 최적화
---

## 10. 우선순위 요약

### 꼭 필요한 것 (MVP)
```
RoundManager → WinCondition → DeathZone → FinishLine →
Race/Survival 레벨 2개 → Grab 기본 → 결과 화면
```
이것만 있어도 **플레이 가능한 게임**이 됩니다.

### 있으면 좋은 것
```
팀전 레벨 → 관전 → 이모트 → 코스메틱
```

---

## 11. 리스크 분석

| 리스크 | 영향 | 대응 |
|--------|------|------|
| 모바일 네트워크 지연 | 물리 동기화 불일치 | 소유권 기반 물리, 상태 보간 강화 |
| 12인 동시 플레이 성능 | 프레임 드롭 | PlatformQualityManager 활용, LOD |
| 잡기 물리 동기화 | 어긋난 잡기 | 잡는 쪽 Authority, 타겟은 따라가기 |
| 코드 복잡도 증가 | 유지보수 어려움 | MVP 패턴 유지, 단일 책임 원칙 |
| 맵 제작 시간 | 콘텐츠 부족 | 점진적 출시, 시즌제 운영 |
