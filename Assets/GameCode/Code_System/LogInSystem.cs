using UnityEngine;
using UnityEngine.UI;

public class LogInSystem : MonoBehaviour
{
    [SerializeField] private InputField _inputEmail;
    [SerializeField] private InputField _intputPassword;
    [SerializeField] private Text _outputTxt;
    [SerializeField] private Button _logInBtn;
    [SerializeField] private Button _logOutBtn;
    [SerializeField] private Button _createBtn;

    private void OnEnable()
    {
        // 로그인 상태 이벤트 구독
        FirebaseAuthManager.Instance._loginState += OnChangedState;
    }

    private void OnDisable()
    {
        FirebaseAuthManager.Instance._loginState -= OnChangedState;
    }

    private void Start()
    {
        // Firebase 초기화
        FirebaseAuthManager.Instance.Init();

        // 버튼 클릭 이벤트 코드에서 등록
        if (_logInBtn != null) _logInBtn.onClick.AddListener(OnLogInClicked);
        if (_logOutBtn != null) _logOutBtn.onClick.AddListener(OnLogOutClicked);
        if (_createBtn != null) _createBtn.onClick.AddListener(OnCreateClicked);
    }

    // 로그인 상태 변경 시 UI 갱신
    private void OnChangedState(bool signedIn)
    {
        _outputTxt.text = signedIn ? "로그인" : "로그아웃";
        _outputTxt.text += "\nUserID: " + FirebaseAuthManager.Instance._userId;
    }

    // 버튼 이벤트 콜백
    private void OnLogInClicked()
    {
        string _email = _inputEmail.text;
        string _password = _intputPassword.text;
        FirebaseAuthManager.Instance.LogIn(_email, _password);
    }

    private void OnLogOutClicked()
    {
        FirebaseAuthManager.Instance.LogOut();
    }

    private void OnCreateClicked()
    {
        string _email = _inputEmail.text;
        string _password = _intputPassword.text;
        FirebaseAuthManager.Instance.Create(_email, _password);
    }
}
