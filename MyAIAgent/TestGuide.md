# TestGuide.md

## 1) 목적

`PartyWorld` 프로젝트의 **기능 검증 및 회귀 테스트**를 체계적으로 관리합니다.

**마지막 업데이트**: 2026-03-30

---

## 2) 캐릭터 선택 저장 테스트 시나리오

### 시나리오 1: 신규 사용자 테스트

**목적**: 앱 최초 실행 → 캐릭터 선택 → 재실행 시 선택 상태 유지 확인

**테스트 단계**:
1. 앱 최초 실행 (Firebase 미연결 상태 시뮬레이션 가능)
2. 타이틀 화면에서 "회원가입" 선택
3. 이메일/비밀번호/닉네임 입력 후 가입 완료
4. 캐릭터 생성 씬(`Scene_CharacterCreation`)으로 자동 이동
5. 캐릭터 목록에서 특정 캐릭터 클릭 (확인 단계 거치지 않음)
6. Console 로그 확인: `[CharacterCreationManager] 캐릭터 선택 즉시 저장: {characterId}`
7. 앱 강제 종료 (에디터 Stop 또는 앱 강제 종료)
8. 앱 재실행 후 동일 계정으로 로그인
9. **검증 포인트**: Waiting Room으로 바로 이동하는지 확인 (캐릭터 생성 씬 우회 = selectedCharacterId 유지 성공)

**성공 기준**:
- 로그인 후 `selectedCharacterId`가 비어있지 않은 경우 → `Scene_WaitingRoom`으로 이동
- 로그인 후 `selectedCharacterId`가 빈 경우 → `Scene_CharacterCreation`으로 이동

**실패 시 확인 사항**:
- Firebase 콘솔에서 `users/{uid}/profile` 데이터 존재 여부
- `DataManager.Instance.CurrentUserData.selectedCharacterId` 값
- `PlayerPrefs`의 `PW_SELECTED_CHARACTER_ID` 값

---

### 시나리오 2: 기존 사용자 테스트

**목적**: 기존 계정으로 로그인 → 캐릭터 변경 → 재실행 시 변경된 캐릭터 유지 확인

**테스트 단계**:
1. 기존 계정으로 로그인 (이미 `selectedCharacterId`가 Firebase에 저장된 상태)
2. Waiting Room으로 바로 이동하는지 확인
3. 앱 종료 후 재실행 또는 캐릭터 재선택 필요 시:
   - 로그아웃 후 캐릭터 생성 씬으로 이동
   - 다른 캐릭터 선택 (확인 전 즉시 선택)
4. 앱 재실행
5. **검증 포인트**: 변경된 캐릭터 ID가 유지되는지 확인

**성공 기준**:
- 마지막으로 선택한 캐릭터 ID가 Firebase에 저장됨
- 재로그인 시 마지막 선택 캐릭터로 Waiting Room 진입

**실패 시 확인 사항**:
- `OnCharacterClicked` 호출 시 로그 출력 여부
- `SaveUserDataToFirebase()` 호출 성공 여부
- Firebase 실시간 데이터베이스 규칙 (쓰기 권한) 확인

---

### 시나리오 3: 재실행 테스트 (앱 백그라운드 전환)

**목적**: 게임 실행 중 캐릭터 선택 후 앱 백그라운드/복구 시 선택 상태 유지 확인

**테스트 단계**:
1. 캐릭터 생성 씬에서 캐릭터 선택
2. 앱을 백그라운드로 전환 (홈 버튼 또는 다른 앱 실행)
3. 일정 시간 대기 (30초 ~ 1분)
4. 앱 포그라운드로 복구
5. **검증 포인트**: 선택된 캐릭터 상태가 메모리에서 유지되는지 확인

**성공 기준**:
- `_selectedIndex`, `_selectedCharacterData` 값 유지
- 캐릭터 미리보기 유지
- PlayerPrefs에 저장된 값 손실 없음

**실패 시 확인 사항**:
- Unity의 `Application.runInBackground` 설정
- 씬 로드 시 `OnCharacterClicked` 재호출 여부
- `DontDestroyOnLoad` 오브젝트 유지 여부 (`DataManager`)

---

## 3) Firebase 데이터 검증 체크리스트

### 공통 검증 포인트
- [ ] `users/{uid}/profile` 경로에 데이터 존재
- [ ] `selectedCharacterId` 필드가 비어있지 않음
- [ ] `nickname` 필드가 "NewPlayer"가 아님
- [ ] `updatedAt` 타임스탬프가 최근 시간

### Firebase 콘솔 확인 방법
1. Firebase 콘솔 접속 (https://console.firebase.google.com)
2. 프로젝트 선택 (`project-playworld`)
3. Realtime Database → 데이터 탭
4. `users` → `{uid}` → `profile` 순서로 탐색

---

## 4) Unity 에디터 디버그 방법

### Console 로그 모니터링
```
[CharacterCreationManager] 캐릭터 선택 즉시 저장: bear_base
[DataManager] Firebase 저장 완료: users/{uid}/profile
```

### PlayerPrefs 확인 (에디터)
```csharp
// Inspector 또는 코드에서 확인
Debug.Log($"PlayerPrefs CharacterID: {PlayerPrefs.GetString("PW_SELECTED_CHARACTER_ID")}");
```

### DataManager 상태 확인
```csharp
// 씬의 DataManager 오브젝트 선택 → Inspector에서 CurrentUserData 확인
Debug.Log($"selectedCharacterId: {DataManager.Instance.CurrentUserData.selectedCharacterId}");
```

---

## 5) 회귀 체크리스트

### 캐릭터 선택 관련
- [ ] 캐릭터 클릭 시 즉시 PlayerPrefs 저장
- [ ] 캐릭터 클릭 시 즉시 DataManager 업데이트
- [ ] 확인 버튼 클릭 시 Firebase 저장 (기존 동작 유지)
- [ ] 선택 취소 시 상태 초기화 안 됨 (즉시 저장 방식이므로)

### 로그인 플로우 관련
- [ ] 신규 사용자: 캐릭터 선택 → Waiting Room 진입
- [ ] 기존 사용자 (캐릭터 있음): 캐릭터 선택 → Waiting Room 진입
- [ ] 기존 사용자 (캐릭터 없음): 캐릭터 생성 씬으로 이동
- [ ] 로그아웃 → 로그인 시 selectedCharacterId 유지

### 네트워크/동기화 관련
- [ ] Photon 방 입장 시 selectedCharacterId 프로퍼티 동기화
- [ ] 캐릭터 스폰 시 올바른 프리팹 로드
- [ ] 대기실 캐릭터 미리보기 정상 표시

---

## 6) 테스트 결과 기록

```md
### YYYY-MM-DD: 테스트 결과
- 시나리오: [1/2/3]
- 계정: {테스트 이메일}
- 결과: [성공/실패]
- 화면 로그: ...
- Firebase 확인: [예/아니오]
-备注: ...
```
