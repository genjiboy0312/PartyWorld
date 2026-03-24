using System;
using System.Threading;
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

    public string _userId => _user != null ? _user.UserId : "None";
    public string _userEmail => _user != null ? _user.Email : "None";

    // 외부에서 로그인 상태 변화를 감지하기 위한 이벤트
    public Action<bool> _loginState;

    private SynchronizationContext _unityContext;
    private int _unityThreadId;

    private void RunOnUnityThread(Action action)
    {
        if (action == null)
            return;

        if (_unityContext == null || Environment.CurrentManagedThreadId == _unityThreadId)
        {
            action();
            return;
        }

        _unityContext.Post(_ => action(), null);
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

    private void OnAuthStateChanged(object sender, EventArgs e)
    {
        if (_unityContext != null && Environment.CurrentManagedThreadId != _unityThreadId)
        {
            _unityContext.Post(_ => OnAuthStateChanged(sender, e), null);
            return;
        }

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

        if (DataManager.Instance != null)
        {
            DataManager.Instance.CurrentUserData.userId = _user.UserId;

            // Firebase Auth 프로필(DisplayName)에서 닉네임 로드
            if (!string.IsNullOrWhiteSpace(_user.DisplayName))
            {
                DataManager.Instance.CurrentUserData.nickname = _user.DisplayName;
            }
            else
            {
                // DisplayName이 비어있으면 로컬 닉네임을 역으로 업로드(레거시 계정 대응)
                string localNick = DataManager.Instance.CurrentUserData.nickname;
                if (!string.IsNullOrWhiteSpace(localNick) && localNick != "NewPlayer")
                {
                    UserProfile profile = new UserProfile { DisplayName = localNick };
                    _user.UpdateUserProfileAsync(profile).ContinueWith(profileTask =>
                    {
                        RunOnUnityThread(() =>
                        {
                            if (profileTask.IsCanceled || profileTask.IsFaulted)
                            {
                                Debug.LogWarning("[FirebaseAuth] 닉네임(DisplayName) 저장 실패: " + profileTask.Exception);
                                return;
                            }

                            Debug.Log($"[FirebaseAuth] 닉네임 저장 완료: {localNick}");
                        });
                    });
                }
            }
        }

        _loginState?.Invoke(true);
    }

    public void Create(string email, string password) => Create(email, password, null);

    // 회원가입 + 닉네임 저장(프로필 DisplayName)
    public void Create(string email, string password, string nickname)
    {
        _auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
        {
            RunOnUnityThread(() =>
            {
                if (task.IsCanceled)
                {
                    Debug.LogError("[FirebaseAuth] 회원가입 취소됨");
                    return;
                }
                if (task.IsFaulted)
                {
                    Debug.LogError("[FirebaseAuth] 회원가입 실패: " + task.Exception);
                    return;
                }

                FirebaseUser newUser = task.Result.User;
                Debug.Log($"[FirebaseAuth] 회원가입 완료: {newUser.Email}");

                string desiredNickname = nickname;
                if (string.IsNullOrWhiteSpace(desiredNickname) && DataManager.Instance != null)
                    desiredNickname = DataManager.Instance.CurrentUserData.nickname;

                if (!string.IsNullOrWhiteSpace(desiredNickname) && desiredNickname != "NewPlayer")
                {
                    UserProfile profile = new UserProfile { DisplayName = desiredNickname };
                    newUser.UpdateUserProfileAsync(profile).ContinueWith(profileTask =>
                    {
                        RunOnUnityThread(() =>
                        {
                            if (profileTask.IsCanceled || profileTask.IsFaulted)
                            {
                                Debug.LogWarning("[FirebaseAuth] 닉네임(DisplayName) 저장 실패: " + profileTask.Exception);
                                return;
                            }

                            Debug.Log($"[FirebaseAuth] 닉네임 저장 완료: {desiredNickname}");
                        });
                    });
                }
            });
        });
    }

    public void LogIn(string email, string password)
    {
        _auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
        {
            RunOnUnityThread(() =>
            {
                if (task.IsCanceled)
                {
                    Debug.LogError("[FirebaseAuth] 로그인 취소됨");
                    return;
                }
                if (task.IsFaulted)
                {
                    Debug.LogError("[FirebaseAuth] 로그인 실패: " + task.Exception);
                    return;
                }

                FirebaseUser newUser = task.Result.User;
                Debug.Log($"[FirebaseAuth] 로그인 완료: {newUser.Email}");
            });
        });
    }

    public void LogOut()
    {
        _auth.SignOut();
        Debug.Log("[FirebaseAuth] 로그아웃 명령 실행");
    }
}
