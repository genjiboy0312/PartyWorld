# AgentCycle.md

## 1) 목적

`PartyWorld` 프로젝트에서 에이전트 작업을 **Plan(설계) → Build(구현)**로 분리해,
작업 누락과 회귀를 줄이고 문서 동기화를 쉽게 유지합니다.

**마지막 업데이트**: 2026-03-25

---

## 2) Plan / Build 역할 정의

| 구분 | 목적 | 주 참고 문서 | 주요 산출물 |
|:--|:--|:--|:--|
| Plan | 무엇을, 왜, 어떤 순서로 할지 결정 | `PlanningAgent.md`, `OpenQuestionsAgent.md`, `RiskAgent.md` | 작업 범위, 우선순위, 결정사항, 리스크 대응안 |
| Build | 계획한 내용을 실제 변경으로 반영 | 코드/씬/프리팹, `CLIAgent.md` | 구현 결과, 검증 결과, 변경 로그 |

---

## 3) 문서 매핑

- `PlanningAgent.md`: Plan 메인 문서(플로우/우선순위/DoD)
- `OpenQuestionsAgent.md`: Plan 중 미결정 항목(기한/결정자/기본값)
- `RiskAgent.md`: Plan/Build 공통 리스크 관리(사전 점검 + 사후 회귀)
- `NoticeAgent.md`: Build 완료 후 변경 이력 기록(영향 범위 포함)
- `README.md`: 문서 운영 규칙(동시 업데이트 규칙)
- `CLIAgent.md`: 작업 정책/운영 가이드

---

## 4) 권장 작업 순환

1. Plan: `PlanningAgent.md`에서 이번 작업 범위를 확정
2. Plan: `OpenQuestionsAgent.md`의 미결정 항목 확인(필요 시 기본값 임시 적용)
3. Plan: `RiskAgent.md` 사전 리스크 점검 및 대응안 확인
4. Build: 코드/씬/프리팹/문서 수정 반영
5. Verify: 테스트 및 회귀 체크(`RiskAgent.md` 기준)
6. Log: `NoticeAgent.md`에 날짜/영향 범위/핵심 변경 기록

---

## 5) 실행 체크리스트 템플릿

```md
## 작업명: <작업 이름>
- 날짜: YYYY-MM-DD
- 담당: <이름/역할>

### Plan
- [ ] 목표/범위 확정 (`PlanningAgent.md`)
- [ ] 미결정 항목 확인 (`OpenQuestionsAgent.md`)
- [ ] 리스크 점검 (`RiskAgent.md`)

### Build
- [ ] 구현 반영(코드/씬/프리팹)
- [ ] 테스트/검증 수행
- [ ] 회귀 체크 완료

### Log
- [ ] 변경 이력 기록 (`NoticeAgent.md`)
- [ ] 관련 문서 동기화(`README.md` 운영 규칙 준수)
```

---

## 6) 운영 원칙

- Plan이 비어 있는 Build는 지양하고, 최소 범위라도 계획을 먼저 확정합니다.
- Build 중 새 리스크가 발견되면 즉시 `RiskAgent.md`에 추가합니다.
- 의사결정이 필요한 항목은 작업 종료 전 `OpenQuestionsAgent.md`에 등록합니다.
- 작업 종료 시 `NoticeAgent.md` 기록을 누락하지 않습니다.
