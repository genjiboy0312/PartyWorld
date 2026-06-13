# AgentCycle.md

## 1) 목적
`Building-Editor` 프로젝트에서 에이전트 작업을 **Plan(설계) → Build(구현)**로 분리해,
작업 누락과 기술 리스크를 줄이고 문서 동기화를 유지합니다.

**마지막 업데이트**: 2026-03-25

---

## 2) Plan / Build 역할 정의

| 구분 | 목적 | 주 참고 문서 | 주요 산출물 |
|:--|:--|:--|:--|
| Plan | 무엇을, 왜, 어떤 순서로 할지 결정 | `PlanningAgent.md`, `OpenQuestionsAgent.md`, `RiskAgent.md` | 작업 범위, 우선순위, 리스크 대응안 |
| Build | 계획한 내용을 실제 변경으로 반영 | 코드(React/FastAPI), 설정 파일, `CLIAgent.md` | 구현 결과, 검증 결과, 변경 로그 |

---

## 3) 문서 매핑

- `PlanningAgent.md`: Plan 메인 문서(기능 명세/우선순위/MVP 기준)
- `OpenQuestionsAgent.md`: Plan 중 미결정 항목(기술적 타당성 검토 필요 항목)
- `RiskAgent.md`: Plan/Build 공통 리스크 관리(성능/데이터 무결성/성능 최적화)
- `NoticeAgent.md`: Build 완료 후 변경 이력 기록(영향 범위 포함)
- `CLIAgent.md`: 작업 정책/운영 가이드(BIM/GIS 도메인 지침)

---

## 4) 권장 작업 순환

1. **Plan**: `PlanningAgent.md`에서 이번 작업 범위(IFC/DXF 처리, UI 편집 등)를 확정
2. **Plan**: `OpenQuestionsAgent.md`의 미결정 기술 항목(데이터 모델/라이브러리 등) 확인
3. **Plan**: `RiskAgent.md` 사전 리스크 점검 (성능 부하/데이터 정합성 확인)
4. **Build**: 코드/데이터/문서 수정 반영
5. **Verify**: 프로젝트 테스트 파이프라인(Vitest/Pytest) 수행 및 정합성 검증
6. **Log**: `NoticeAgent.md`에 날짜/영향 범위(프론트엔드/백엔드)/핵심 변경 기록

---

## 5) 실행 체크리스트 템플릿

```md
## 작업명: <작업 이름>
- 날짜: YYYY-MM-DD
- 담당: <이름/역할>

### Plan
- [ ] 목표/범위 확정 (`PlanningAgent.md`)
- [ ] 미결정 항목 확인 (`OpenQuestionsAgent.md`)
- [ ] 기술 리스크 점검 (`RiskAgent.md`)

### Build
- [ ] 구현 반영(코드/데이터 모델/API)
- [ ] 테스트/검증 수행 (Vitest/Pytest)
- [ ] 성능/정합성 체크 완료

### Log
- [ ] 변경 이력 기록 (`NoticeAgent.md`)
```

---

## 6) 운영 원칙

- Plan이 비어 있는 Build는 지양하고, 최소 범위라도 계획을 먼저 확정합니다.
- Build 중 성능 이슈나 좌표계 불일치 등 새 리스크가 발견되면 즉시 `RiskAgent.md`에 추가합니다.
- 의사결정이 필요한 기술 항목은 작업 종료 전 `OpenQuestionsAgent.md`에 등록합니다.
- 작업 종료 시 `NoticeAgent.md` 기록을 누락하지 않습니다.
