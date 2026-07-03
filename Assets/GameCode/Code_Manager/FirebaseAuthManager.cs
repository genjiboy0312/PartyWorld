using System;
using System.Threading;
using System.Threading.Tasks;
using Firebase.Auth;
using UnityEngine;

// Firebase 인증 관리자 (비-MonoBehaviour 싱글톤)
public class FirebaseAuthManager
{
    private static FirebaseAuthManager _instance;

    public static FirebaseAuthManager Instance
    {
        get
        {
            _instance ??= new FirebaseAuthManager();
            return _instance;
        }
    }

    private FirebaseAuth _auth;
    private FirebaseUser _user;
    private string _pendingNicknameForNewUser;

    public string _userId => _user != null ? _user.UserId : "None";
    public string _userEmail => _user != null ? _user.Email : "None";

    // 외부에서 로그인 상태 변화를 감지하기 위한 이벤트
    public Action<bool> _loginState;

    private SynchronizationContext _unityContext;
    private int _unityThreadId;

    // Unity 메인 스레드로 전환하는 awaiter
    private Task SwitchToMainThreadAsync()
    {
        if (_unityContext == null || Environment.CurrentManagedThreadId == _unityThreadId)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource<bool>();
        _unityContext.Post(_ => tcs.SetResult(true), null);
        return tcs.Task;
    }

    public void Init()
    {
        _unityContext = SynchronizationContext.Current;
        _unityThreadId = Environment.CurrentManagedThreadId;

        _auth = FirebaseAuth.DefaultInstance;

        if (_auth.CurrentUser != null)
        {
            LogOut();
            Debug.Log("[FirebaseAuth] 초기화 중 기존 세션 로그아웃 실행");
        }

        _auth.StateChanged -= OnAuthStateChanged;
        _auth.StateChanged += OnAuthStateChanged;
    }

    private async void OnAuthStateChanged(object sender, EventArgs e)
    {
        try
        {
            // Firebase 이벤트가 백그라운드 스레드에서도 발생할 수 있으므로 메인 스레드 보장
            await SwitchToMainThreadAsync();

            if (_auth == null)
                return;

            if (_auth.CurrentUser == _user)
                return;

            bool signedIn = _auth.CurrentUser != null;

            // 로그아웃 감지
            if (!signedIn && _user != null)
            {
                Debug.Log("[FirebaseAuth] 유저 로그아웃 감지");

                if (DataManager.Instance != null)
                    DataManager.Instance.ClearUserData();

                _user = null;
                _loginState?.Invoke(false);
                return;
            }

            _user = _auth.CurrentUser;

            if (!signedIn || _user == null)
                return;

            Debug.Log($"[FirebaseAuth] 유저 로그인 성공: {_user.Email}");

            if (DataManager.Instance == null)
            {
                _loginState?.Invoke(true);
                return;
            }

            string authNickname = _user.DisplayName;

            DataManager.Instance.CurrentUserData.userId = _user.UserId;
            DataManager.Instance.MarkLoginNow();

            // async/await 방식으로 Firebase 데이터 로드
            var (success, loaded) = await DataManager.Instance.LoadUserDataFromFirebaseAsync(_user.UserId);

            if (DataManager.Instance == null)
            {
                _loginState?.Invoke(true);
                return;
            }

            string localNick = DataManager.Instance.CurrentUserData.nickname;
            bool localMissing = string.IsNullOrWhiteSpace(localNick) || localNick == "NewPlayer";

            if (localMissing && !string.IsNullOrWhiteSpace(_pendingNicknameForNewUser))
            {
                localNick = _pendingNicknameForNewUser;
                localMissing = false;
                DataManager.Instance.CurrentUserData.nickname = localNick;
            }

            if (!string.IsNullOrWhiteSpace(authNickname))
            {
                if (localMissing || !loaded)
                {
                    DataManager.Instance.CurrentUserData.nickname = authNickname;
                    await DataManager.Instance.SaveUserDataToFirebaseAsync();
                }

                _loginState?.Invoke(true);
                return;
            }

            if (localMissing)
            {
                _loginState?.Invoke(true);
                return;
            }

            // DisplayName 업데이트 (async/await)
            UserProfile profile = new UserProfile { DisplayName = localNick };
            try
            {
                await _user.UpdateUserProfileAsync(profile);
                Debug.Log($"[FirebaseAuth] 닉네임 저장 완료: {localNick}");
            }
            catch (Exception profileEx)
            {
                Debug.LogWarning($"[FirebaseAuth] 닉네임(DisplayName) 저장 실패: {profileEx.Message}");
            }

            await DataManager.Instance.SaveUserDataToFirebaseAsync();
            _pendingNicknameForNewUser = null;
            _loginState?.Invoke(true);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FirebaseAuth] 인증 상태 변경 처리 중 오류: {ex.Message}");
            _loginState?.Invoke(false);
        }
    }

    public void Create(string email, string password) => Create(email, password, null);

    // 회원가입 + 닉네임 저장(프로필 DisplayName)
    public async void Create(string email, string password, string nickname)
    {
        _pendingNicknameForNewUser = nickname;

        try
        {
            var authResult = await _auth.CreateUserWithEmailAndPasswordAsync(email, password);
            FirebaseUser newUser = authResult.User;
            Debug.Log($"[FirebaseAuth] 회원가입 완료: {newUser.Email}");

            string desiredNickname = nickname;
            if (string.IsNullOrWhiteSpace(desiredNickname) && DataManager.Instance != null)
                desiredNickname = DataManager.Instance.CurrentUserData.nickname;

            if (!string.IsNullOrWhiteSpace(desiredNickname) && desiredNickname != "NewPlayer")
            {
                if (DataManager.Instance != null)
                    DataManager.Instance.CurrentUserData.nickname = desiredNickname;

                UserProfile profile = new UserProfile { DisplayName = desiredNickname };
                await newUser.UpdateUserProfileAsync(profile);
                Debug.Log($"[FirebaseAuth] 닉네임 저장 완료: {desiredNickname}");
            }

            _pendingNicknameForNewUser = null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FirebaseAuth] 회원가입 실패: {ex.Message}");
            _pendingNicknameForNewUser = null;
        }
    }

    public async void LogIn(string email, string password)
    {
        try
        {
            var authResult = await _auth.SignInWithEmailAndPasswordAsync(email, password);
            FirebaseUser newUser = authResult.User;
            Debug.Log($"[FirebaseAuth] 로그인 완료: {newUser.Email}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FirebaseAuth] 로그인 실패: {ex.Message}");
        }
    }

    public void LogOut()
    {
        _auth.SignOut();
        Debug.Log("[FirebaseAuth] 로그아웃 명령 실행");
    }

    public async void CheckEmailExists(string email, Action<bool, bool> onCompleted)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            onCompleted?.Invoke(false, false);
            return;
        }

        try
        {
            var providers = await _auth.FetchProvidersForEmailAsync(email);
            bool exists = false;
            if (providers != null)
            {
                using (var e = providers.GetEnumerator())
                {
                    exists = e.MoveNext();
                }
            }
            onCompleted?.Invoke(true, exists);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[FirebaseAuth] 이메일 중복 확인 실패: {ex.Message}");
            onCompleted?.Invoke(false, false);
        }
    }
}
