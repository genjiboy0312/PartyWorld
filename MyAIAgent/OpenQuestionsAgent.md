# OpenQuestionsAgent.md

## 1) 목적

기획/구현을 진행하다가 **결정이 필요해서 보류된 질문들**을 모아두고, 결정되면 `PlanningAgent.md`에 반영합니다.

**마지막 업데이트**: 2026-03-27

---

## 2) 오픈 질문(결정되면 계획 업데이트)

| ID | 질문 | 옵션 | 결정자(Owner) | 결정 기한 | 기본값(Default) | 상태 |
|:--|:--|:--|:--|:--|:--|:--|
| Q-001 | 라운드 기본 장르는 무엇에 더 가깝나? | 장애물 레이싱 / 물리 격투 / 혼합 | Game Design Lead (TBD) | 2026-03-31 | 혼합 | Open |
| Q-002 | 승리 조건은 단판 생존인가, 점수제 복수 라운드인가? | 마지막 1인 / 점수제 | Game Design Lead (TBD) | 2026-03-31 | 마지막 1인 생존 | Open |
| Q-003 | 모바일 조작/카메라 UX 기본 정책은? | 조이스틱+버튼 / 자동 카메라 보조 포함 | UX Lead (TBD) | 2026-04-05 | 조이스틱+버튼(점프/다이브/잡기), 자동 보조 Off | Open |
| Q-004 | `Scene_CharacterCreation`은 어떤 조건에서 진입하나? | 항상 진입 / 신규 유저만 / 비활성 | Game Design Lead (TBD) | 2026-03-28 | 신규 유저만 진입 | Decided (2026-03-27) |
| Q-005 | 플레이어 스폰 책임 주체를 어디로 고정할 것인가? | `NetworkAuthorityManager` 단일화 / 별도 SpawnManager / 씬별 처리 | Tech Lead (TBD) | 2026-03-28 | `NetworkAuthorityManager` 단일화 | Decided (2026-03-27) |
| Q-006 | Photon 스폰용 캐릭터 프리팹 표준 경로는? | `Assets/Resources/Characters/*` / 커스텀 PrefabPool | Tech Lead (TBD) | 2026-03-29 | `Assets/Resources/Characters/*` | Open |
| Q-007 | CharacterCatalog 에셋 운영 기준은? | 수동 갱신 / `GenJiTools` Sync 강제 | Tech Lead (TBD) | 2026-03-29 | `GenJiTools/Character Catalog/Sync` 기준 | Open |

## 3) 의사결정 운영 규칙

- 기한 내 미결정 시 `기본값(Default)`을 임시 적용하고, 다음 스프린트에서 재검토
- 결정 완료 시 `PlanningAgent.md`에 반영 후, 본 문서 상태를 `Decided`로 변경
- 결정자가 비어 있으면 이번 주 스탠드업에서 즉시 담당 지정
