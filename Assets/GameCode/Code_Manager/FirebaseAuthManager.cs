using System;
using Firebase.Auth;
using UnityEngine;

// Firebase 인증 관리 싱글톤 클래스
public class FirebaseAuthManager
{
    #region 싱글톤 인스턴스
    private static FirebaseAuthManager _instance = null;

    public static FirebaseAuthManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new FirebaseAuthManager();
            }
            return _instance;
        }
    }
    #endregion

    #region 변수 및 이벤트 필드
    private FirebaseAuth _auth;     // Firebase 인증 객체
    private FirebaseUser _user;     // 인증된 유저 정보

    // 현재 인증된 유저의 UID와 이메일을 반환 (로그인 상태가 아니면 "None")
    public string _userId => _user != null ? _user.UserId : "None";
    public string _userEmail => _user != null ? _user.Email : "None";

    // 외부에서 로그인 상태 변화를 감지하기 위한 이벤트
    public Action<bool> _loginState; 
    #endregion

    #region 초기화 로직
    // Firebase 인증 시스템을 초기화합니다.
    public void Init()
    {
        _auth = FirebaseAuth.DefaultInstance;

        // 보안을 위해 초기화 시 이미 로그인된 유저가 있다면 강제 로그아웃 처리
        if (_auth.CurrentUser != null)
        {
            LogOut();
            Debug.Log("[FirebaseAuth] 초기화 중 기존 세션 로그아웃 실행");
        }

        // Firebase 인증 상태 변경 시 OnAuthStateChanged 메서드가 호출되도록 등록
        _auth.StateChanged += OnAuthStateChanged;
    }
    #endregion

    #region 인증 콜백 핸들러 (DataManager 연동)
    // Firebase의 인증 상태(로그인/로그아웃)가 변경될 때마다 실행됩니다.
    private void OnAuthStateChanged(object sender, EventArgs e)
    {
        if (_auth.CurrentUser != _user)
        {
            bool signedIn = (_auth.CurrentUser != null);

            // [상태: 로그아웃]
            if (!signedIn && _user != null)
            {
                Debug.Log("[FirebaseAuth] 유저 로그아웃 감지");
                
                // 전역 데이터 매니저의 유저 정보를 초기화합니다.
                if (DataManager.Instance != null)
                {
                    DataManager.Instance.ClearUserData();
                }

                _loginState?.Invoke(false);
            }

            _user = _auth.CurrentUser;

            // [상태: 로그인 성공]
            if (signedIn)
            {
                Debug.Log($"[FirebaseAuth] 유저 로그인 성공: {_user.Email}");
                
                // 로그인에 성공한 유저의 고유 UID를 DataManager에 저장합니다.
                if (DataManager.Instance != null)
                {
                    DataManager.Instance.CurrentUserData.userId = _user.UserId;
                    // TODO: 여기서 Firebase Database로부터 나머지 데이터(Money, Items 등)를 로드하는 로직을 호출할 수 있습니다.
                }

                _loginState?.Invoke(true);
            }
        }
    }
    #endregion

    #region 인증 액션 메서드 (회원가입, 로그인, 로그아웃)
    // 새로운 이메일 계정을 생성합니다.
    public void Create(string email, string password)
    {
        _auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
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

            AuthResult authResult = task.Result;
            FirebaseUser newUser = authResult.User;
            Debug.Log($"[FirebaseAuth] 회원가입 완료: {newUser.Email}");
        });
    }

    // 기존 이메일 계정으로 로그인을 시도합니다.
    public void LogIn(string email, string password)
    {
        _auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
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

            AuthResult authResult = task.Result;
            FirebaseUser newUser = authResult.User;
            Debug.Log($"[FirebaseAuth] 로그인 완료: {newUser.Email}");
        });
    }

    // 현재 세션을 종료하고 로그아웃합니다.
    public void LogOut()
    {
        _auth.SignOut();
        Debug.Log("[FirebaseAuth] 로그아웃 명령 실행");
    }
    #endregion
}
