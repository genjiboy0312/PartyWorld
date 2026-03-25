# MyAIAgent

프로젝트에서 사용하는 에이전트 문서 모음입니다.

- `PlanningAgent.md`: 씬/네트워크 흐름, 우선순위, 구현 계획(메인)
- `RiskAgent.md`: 알려진 리스크 & 체크리스트(릴리즈/회귀 점검)
- `OpenQuestionsAgent.md`: 오픈 질문(결정 필요 항목)
- `NoticeAgent.md`: 최근 작업 내역/공지용 변경 로그
- `CLIAgent.md`: Codex CLI 사용 가이드(프로젝트용)
- `AgentCycle.md`: Plan/Build 작업 순환 가이드 및 체크리스트

## 문서 운영 규칙

- 기능/플로우 변경 시: `PlanningAgent.md` + `RiskAgent.md` + `NoticeAgent.md` 동시 업데이트
- 결정 필요 항목이 생기면: `OpenQuestionsAgent.md`에 먼저 등록 후, 결정되면 `PlanningAgent.md`에 반영
- 회귀/장애가 발생하면: `RiskAgent.md`에 재현 절차를 남기고, 해결 후 체크리스트 상태를 갱신
- 작업 종료 시: `NoticeAgent.md`에 날짜 기준으로 영향 범위(씬/스크립트/프리팹)까지 함께 기록
- 문서 변경 시: 각 문서의 `마지막 업데이트` 날짜를 당일 기준으로 유지
