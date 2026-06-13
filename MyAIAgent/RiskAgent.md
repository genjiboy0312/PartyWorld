# RiskAgent.md

## 1) 목적

`PartyWorld` Unity 프로젝트의 **기술 리스크, 성능 문제, 회귀 포인트**를 관리합니다.
타겟 플랫폼: Android / iOS 모바일

**마지막 업데이트**: 2026-06-13

---

## 2) 리스크 매트릭스

| ID | 항목 | 심각도 | 발생 조건 | 대응책 | 상태 |
|:--|:--|:--|:--|:--|:--|
| R-001 | 모바일 성능 (Frame Drop) | High | 다수 플레이어/파티클/이펙트 동시 재생 시 | LOD, 오클루전 컬링, VFX 그래프 최적화 | Open |
| R-002 | Photon 네트워크 지연/끊김 | High | 모바일 데이터 환경 (3G/5G 전환, Wi-Fi 불안정) | 재연결 로직, 상태 동기화 폴백, RTT 모니터링 | Open |
| R-003 | 메모리 부족 (저사양 기기) | High | 구형 Android 기기, 대용량 텍스처/모델 | Addressables, 텍스처 압축, 에셋 번들 | Open |
| R-004 | 씬 전환 데이터 동기화 불일치 | Medium | 마스터 클라이언트 이탈, 네트워크 지연 시 | 로딩 게이트, 플레이어 프로퍼티 기반 상태 검증 | Open |
| R-005 | Firebase 인증 지연/실패 | Medium | 네트워크 불안정, 토큰 만료 | 재시도 로직, 오프라인 모드 폴백 | Open |

---

## 3) 리스크 상세

### R-001: 모바일 성능 (Frame Drop)
- **환경**: Android/iOS 저중사양 기기 (RAM 3GB 이하)
- **증상**: 파티클 이펙트, 다수 캐릭터 동시 표시 시 30fps 이하로 하락
- **영향**: 사용자 경험 저하, 조작감 악화
- **대응**:
  - Quality Settings 레벨별 분기 (저/중/고)
  - VFX Graph → GPU 파티클로 전환
  - 캐릭터 LOD (멀리 있는 플레이어 Polygon 감소)
  - UI Canvas 최적화 (Atlas, Overlay 병합)

### R-002: Photon 네트워크 지연/끊김
- **환경**: 모바일 데이터 환경, Wi-Fi 핸드오프
- **증상**: 플레이어 위치 순간이동, Ready 상태 불일치, 카운트다운 동기화 실패
- **영향**: 게임 진행 불가, 플레이어 경험 악화
- **대응**:
  - PhotonNetwork.SendRate/SerializationRate 모바일 환경에 맞게 조정
  - OnDisconnected 시 재연결 UI 및 자동 재시도
  - 마스터 이탈 시 새 마스터 선출 및 상태 복구

### R-003: 메모리 부족
- **환경**: 2-3GB RAM 기기, 대규모 맵
- **증상**: 앱 크래시, OutOfMemoryException
- **영향**: 게임 플레이 불가
- **대응**:
  - Addressables 도입 (레이지 로딩)
  - 텍스처 최대 해상도 2048 제한
  - 풀링 시스템 적용 (캐릭터/아이템/이펙트)
  - Profiler 정기 측정

---

## 4) Known Issue 템플릿

```md
### [ISSUE-ID] 제목
- 발견일: YYYY-MM-DD
- 심각도: High / Medium / Low
- 환경: (기기/OS/네트워크 조건)
- 재현 스텝:
  1) ...
  2) ...
- 기대 결과: ...
- 실제 결과: ...
- 원인 가설: ...
- 해결 상태: Open / Fixing / Resolved
```
