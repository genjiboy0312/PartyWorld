# UnityAgent.md

## 1) 목적

`PartyWorld`의 Unity/C# 개발 품질을 높이기 위한 전용 에이전트 운영 기준입니다.
씬/프리팹/스크립트/네트워크(Photon) 작업에서 발생하기 쉬운 회귀를 줄이고,
확장 가능한 구조로 유지하는 것을 목표로 합니다.

**마지막 업데이트**: 2026-06-18

---

## 2) 역할 범위

- Unity 씬 구조 점검(`.unity`, Build Settings, 씬 전환)
- C# 스크립트 구현/리팩토링(매니저, UI, 플레이어, 네트워크)
- 프리팹 연결 검증(참조 누락, 컴포넌트 불일치)
- Photon 동기화 흐름 점검(룸 프로퍼티, 플레이어 프로퍼티, 마스터 권한)
- 모바일 플랫폼 성능 및 메모리 최적화
- 문서 반영(`RiskAgent.md`, `NoticeAgent.md`)까지 연계

---

## 3) 핵심 원칙

1. **단일 책임 우선**
   - 씬 상태 매핑은 `GameManager`, 네트워크 권한은 `NetworkAuthorityManager`처럼 책임을 고정합니다.

2. **씬 이름 하드코딩 최소화**
   - 문자열 비교 로직은 한 곳에서만 관리하고, 매핑 함수/설정 필드로 일원화합니다.
   - `_mapScenePrefix` 필드를 통해 접두어 기반 비교로 통일합니다.

3. **Assembly Definition File 사용**
   - `Assets/GameCode/GameCode.asmdef`를 통해 스크립트를 전용 어셈블리로 격리합니다.
   - 외부 참조는 asmdef의 `references` 필드에 명시적으로 추가합니다.

4. **참조 누락 방지**
   - 기본은 인스펙터 직결 + null 가드로 구성합니다.
   - `GameObject.Find`, `FindObjectOfType` 같은 런타임 탐색 의존은 신규 코드에서 지양합니다.
   - `Find*` 계열 API는 CPU 낭비와 예측 불가능한 참조 문제를 만들 수 있으므로 기본 금지합니다.
   - 참조가 비어 있으면 자동 생성/자동 탐색 대신 `null` 상태를 유지하고 `Debug.LogWarning`으로 누락을 명시합니다.

5. **이벤트 안전성 확보**
   - `AddListener`/이벤트 구독은 중복 등록을 피하고, `OnDisable`/`OnDestroy`에서 정리합니다.
   - 구독자는 스스로 등록/해지하는 패턴을 우선합니다(`ChatManager` 참고).

6. **멀티플레이 권위 일관성 유지**
   - 전환/판정/카운트다운 같은 핵심 로직은 마스터 권한 기준으로 실행합니다.
   - 마스터 이탈 시 `OnMasterClientSwitched`에서 새 마스터가 상태를 이어받도록 처리합니다.

7. **모바일 성능 고려**
   - `PlatformQualityManager`가 기기 RAM/GPU 메모리 기준으로 품질 레벨을 자동 설정합니다.
   - 저사양 기기: 30fps 제한, Quality Level 0
   - 고사양 기기: 60fps, Quality Level 2

8. **Photon 네트워크 파라미터 모바일 튜닝**
   - `SendRate`/`SerializationRate` 기본값을 모바일 환경에 맞게 20으로 조정합니다.
   - 데스크탑 중심 값(60)은 모바일 배터리/데이터 사용량에 부적합합니다.

9. **주석 최소화 정책 (Token 절약)**
   - 불필요한 주석은 토큰 낭비입니다. AI 에이전트가 생성하는 모든 코드에서 절대 추가하지 않습니다.
   - 특히 코드만으로 명확한 내용(// 변수 선언, // 함수 호출 등)에 주석을 달지 않습니다.

10. **도구/메뉴 운영 일원화**
    - 에디터 자동화 메뉴는 `GenJiTools` 상위 메뉴로 통일합니다.
    - 캐릭터 카탈로그 동기화/검증은 `GenJiTools/Character Catalog/*` 경로를 기준으로 운영합니다.

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
- `GameCode.asmdef`가 적용되었으므로, 외부 어셈블리 참조 필요 시 `references` 배열에 추가

### 4.3 Verify
- 최소 검증:
  - 콘솔 오류 없음
  - 씬 전환 경로 정상
  - 버튼 이벤트 중복 호출 없음
  - 멀티플레이 핵심 플로우(Ready/Loading/Map) 정상
  - 모바일 빌드 시 품질 레벨 자동 설정 확인

### 4.4 Log
- `NoticeAgent.md`에 날짜/영향 범위/핵심 변경 기록
- 미해결 의사결정이나 기술 부채는 `NoticeAgent.md`의 작업 상태 표에 등록

---

## 5) Unity/C# 리뷰 체크리스트

- [ ] `SerializeField` 참조가 씬/프리팹에서 실제 연결되어 있는가?
- [ ] 런타임 `Find*` 호출 없이 동작하는가?
- [ ] asmdef 참조가 누락되지 않았는가?
- [ ] 이벤트 구독/해지가 `OnEnable`/`OnDisable`에서 쌍으로 관리되는가?
- [ ] null 체크 없이 접근하는 UI/컴포넌트가 없는가?
- [ ] Photon 콜백에서 `OnMasterClientSwitched` 처리가 누락되지 않았는가?
- [ ] 씬 이름 분기 로직이 `_mapScenePrefix` 필드 기반으로 확장 가능한가?
- [ ] Photon 권한 체크(`IsMasterClient`, `InRoom`)가 필요한 지점에 있는가?
- [ ] 모바일 기기에서 성능/메모리 이슈가 예상되는 부분은 없는가?
- [ ] 매니저 간 책임 경계가 무너지지 않았는가?
- [ ] 변경 사항이 `Risk/Notice` 문서에 반영되었는가?

---

## 6) 권장 디렉토리/코드 기준

- 매니저: `Assets/GameCode/Code_Manager`
- UI: `Assets/GameCode/Code_UI`
- 시스템: `Assets/GameCode/Code_System`
- 데이터: `Assets/GameCode/Code_Data`
- 플레이어: `Assets/GameCode/Code_Player`
- 오브젝트: `Assets/GameCode/Code_Object`
- 씬: `Assets/GameData/Scenes`
- 캐릭터 프리팹: `Assets/GameData/Prefabs/Characters`
- Assembly Definition: `Assets/GameCode/GameCode.asmdef`

작업 시 위 경계를 우선 유지하고, 경계 변경이 필요하면 문서에 이유를 남깁니다.

---

## 7) 향후 고도화 로드맵

1. **Scene-State Registry 도입**
   - 씬/상태 매핑을 ScriptableObject 기반으로 분리해 운영 유연성 확보

2. **검증 자동화**
   - 에디터 스크립트로 참조 누락/씬 누락/중복 매니저 검사 자동화

3. **네트워크 회귀 시나리오 정형화**
   - 마스터 이탈/지연/재접속 케이스를 테스트 시나리오로 문서화

### 완료된 항목

- Assembly Definition File (`GameCode.asmdef`) 적용 완료
- 플랫폼 자동 품질 조절 (`PlatformQualityManager`) 구현 완료
- 마스터 이탈 처리 (`OnMasterClientSwitched`) 구현 완료
- `Find*` API 제거 (GameManager, NetworkAuthorityManager Bootstrap)
- Photon SendRate/SerializationRate 모바일 기본값(20)으로 조정 완료
- 씬 이름 하드코딩 정리 (`_mapScenePrefix` 도입)
- ChatManager 이중구독 버그 수정
