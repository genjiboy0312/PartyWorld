# UnityAgent.md

## 1) 목적

`PartyWorld`의 Unity/C# 개발 품질을 높이기 위한 전용 에이전트 운영 기준입니다.
씬/프리팹/스크립트/네트워크(Photon) 작업에서 발생하기 쉬운 회귀를 줄이고,
확장 가능한 구조로 유지하는 것을 목표로 합니다.

**마지막 업데이트**: 2026-03-27

---

## 2) 역할 범위

- Unity 씬 구조 점검(`.unity`, Build Settings, 씬 전환)
- C# 스크립트 구현/리팩토링(매니저, UI, 플레이어, 네트워크)
- 프리팹 연결 검증(참조 누락, 컴포넌트 불일치)
- Photon 동기화 흐름 점검(룸 프로퍼티, 플레이어 프로퍼티, 마스터 권한)
- 문서 반영(`PlanningAgent`, `RiskAgent`, `NoticeAgent`)까지 연계

---

## 3) 핵심 원칙

1. **단일 책임 우선**
   - 씬 상태 매핑은 `GameManager`, 네트워크 권한은 `NetworkAuthorityManager`처럼 책임을 고정합니다.

2. **씬 이름 하드코딩 최소화**
   - 문자열 비교 로직은 한 곳에서만 관리하고, 매핑 함수/설정 필드로 일원화합니다.

3. **참조 누락 방지**
   - 기본은 인스펙터 직결 + null 가드로 구성합니다.
   - `GameObject.Find`, `FindObjectOfType` 같은 런타임 탐색 의존은 신규 코드에서 지양합니다.
   - `Find*` 계열 API는 CPU 낭비와 예측 불가능한 참조 문제를 만들 수 있으므로 기본 금지합니다.
   - 참조가 비어 있으면 자동 생성/자동 탐색 대신 `null` 상태를 유지하고 `Debug.LogWarning`으로 누락을 명시합니다.

4. **이벤트 안전성 확보**
   - `AddListener`/이벤트 구독은 중복 등록을 피하고, `OnDisable`/`OnDestroy`에서 정리합니다.

5. **멀티플레이 권위 일관성 유지**
   - 전환/판정/카운트다운 같은 핵심 로직은 마스터 권한 기준으로 실행합니다.

6. **주석 최소화 정책**
   - 주석은 요청받은 경우에만 추가합니다.
   - 코드 자체로 의도가 드러나는 경우 주석 대신 네이밍/구조 개선을 우선합니다.

7. **도구/메뉴 운영 일원화**
   - 에디터 자동화 메뉴는 `GenJiTools` 상위 메뉴로 통일합니다.
   - 캐릭터 카탈로그 동기화/검증은 `GenJiTools/Character Catalog/*` 경로를 기준으로 운영합니다.

8. **런타임 탐색 금지 재강조**
   - `Find*` 계열 탐색은 신규 코드에서 기본 금지합니다.
   - 참조 누락은 자동 탐색 대신 `Debug.LogWarning`으로 노출하고, 인스펙터 연결로 해결합니다.

---

## 4) 작업 프로토콜 (Unity/C#)

### 4.1 Plan
- 요구사항을 씬 단위/스크립트 단위로 분해
- 영향 파일 목록 작성(예: `Code_Manager`, `Code_UI`, Scene 파일)
- 리스크 사전 등록(`RiskAgent.md`)

### 4.2 Build
- 코드 변경 시 기존 패턴(싱글톤, 매니저 구조, 네이밍) 준수
- Scene/Prefab 변경 시 연결 레퍼런스 확인
- 자동 바인딩/자동 생성보다 명시적 참조 연결을 우선

### 4.3 Verify
- 최소 검증:
  - 콘솔 오류 없음
  - 씬 전환 경로 정상
  - 버튼 이벤트 중복 호출 없음
  - 멀티플레이 핵심 플로우(Ready/Loading/Map) 정상

### 4.4 Log
- `NoticeAgent.md`에 날짜/영향 범위/핵심 변경 기록
- 의사결정 미완료 항목은 `OpenQuestionsAgent.md`에 등록

---

## 5) Unity/C# 리뷰 체크리스트

- [ ] `SerializeField` 참조가 씬/프리팹에서 실제 연결되어 있는가?
- [ ] 런타임 `Find*` 호출 없이 동작하는가?
- [ ] 주석은 요청받은 부분에만 추가했는가?
- [ ] null 체크 없이 접근하는 UI/컴포넌트가 없는가?
- [ ] `Start/Awake/OnEnable`에서 이벤트 중복 등록 가능성이 없는가?
- [ ] 씬 이름 분기 로직이 확장 가능하게 작성되었는가?
- [ ] Photon 권한 체크(`IsMasterClient`, `InRoom`)가 필요한 지점에 있는가?
- [ ] 매니저 간 책임 경계가 무너지지 않았는가?
- [ ] 변경 사항이 `Planning/Risk/Notice` 문서에 반영되었는가?

---

## 6) 권장 디렉토리/코드 기준

- 매니저: `Assets/GameCode/Code_Manager`
- UI: `Assets/GameCode/Code_UI`
- 시스템: `Assets/GameCode/Code_System`
- 데이터: `Assets/GameCode/Code_Data`
- 씬: `Assets/GameData/Scenes`
- 캐릭터 프리팹: `Assets/GameData/Prefabs/Characters`

작업 시 위 경계를 우선 유지하고, 경계 변경이 필요하면 문서에 이유를 남깁니다.

---

## 7) 향후 고도화 로드맵

1. **Scene-State Registry 도입**
   - 씬/상태 매핑을 ScriptableObject 기반으로 분리해 운영 유연성 확보

2. **Character Selection 파이프라인 표준화**
   - `CharacterData` → 선택 저장 → WaitingRoom/Map 스폰까지 단일 경로 확정
   - `CharacterCatalog`(ScriptableObject) + `GenJiTools/Character Catalog` 동기화 워크플로우 고정

3. **검증 자동화**
   - 에디터 스크립트로 참조 누락/씬 누락/중복 매니저 검사 자동화

4. **네트워크 회귀 시나리오 정형화**
   - 마스터 이탈/지연/재접속 케이스를 테스트 시나리오로 문서화
