using System;
using Firebase.Auth;
using UnityEngine;

// Firebase 인증 관리 싱글톤 클래스
public class FirebaseAuthManager
{
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

    private FirebaseAuth _auth;     // Firebase 인증 객체
    private FirebaseUser _user;     // 인증된 유저 정보

    // 유저 ID와 이메일 가져오기
    public string _userId => _user != null ? _user.UserId : "None";
    public string _userEmail => _user != null ? _user.Email : "None";

    public Action<bool> _loginState; // 로그인 상태 이벤트

    // Firebase 인증 초기화
    public void Init()
    {
        _auth = FirebaseAuth.DefaultInstance;

        // 이미 로그인된 유저가 있으면 로그아웃 처리
        if (_auth.CurrentUser != null)
        {
            LogOut();
            Debug.Log("초기화 중 로그아웃 실행");
        }

        // 인증 상태 변경 이벤트 구독
        _auth.StateChanged += OnAuthStateChanged;
    }

    // 인증 상태가 변경될 때 호출
    private void OnAuthStateChanged(object sender, EventArgs e)
    {
        if (_auth.CurrentUser != _user)
        {
            bool signedIn = (_auth.CurrentUser != null);

            if (!signedIn && _user != null)
            {
                Debug.Log("유저 로그아웃");
                _loginState?.Invoke(false);
            }

            _user = _auth.CurrentUser;
            if (signedIn)
            {
                Debug.Log("유저 로그인");
                _loginState?.Invoke(true);
            }
        }
    }

    // 새 유저 계정 생성
    public void Create(string email, string password)
    {
        _auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("회원가입 취소");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("회원가입 실패");
                return;
            }

            AuthResult authResult = task.Result;
            FirebaseUser newUser = authResult.User;
            Debug.Log("회원가입 완료");
        });
    }

    // 이메일과 비밀번호로 로그인
    public void LogIn(string email, string password)
    {
        _auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("로그인 취소");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("로그인 실패");
                return;
            }

            AuthResult authResult = task.Result;
            FirebaseUser newUser = authResult.User;
            Debug.Log("로그인 완료");
        });
    }

    // 현재 유저 로그아웃
    public void LogOut()
    {
        _auth.SignOut();
        Debug.Log("유저 로그아웃");
    }
}
