using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LogInSystem : MonoBehaviour
{
    [Header("Pages")]
    [SerializeField] private GameObject _uiLoginPage;
    [SerializeField] private GameObject _uiJoinPage;
    [SerializeField] private bool _showLoginPageOnStart = false;

    [Header("UI LogIn References")]
    [SerializeField] private InputField _inputLogInEmail;
    [SerializeField] private InputField _inputLogInPassword;
    [SerializeField] private Text _outputTxt;
    [SerializeField] private Button _logInBtn;
    [SerializeField] private Button _logOutBtn;
    [SerializeField] private Button _joinBtn;

    [Header("UI JoinUser References")]
    [SerializeField] private InputField _inputJoinUserNickname;
    [SerializeField] private InputField _inputJoinUserEmail;
    [SerializeField] private InputField _inputJoinUserPassword;
    [SerializeField] private InputField _inputJoinUserConfirmPassword;
    [SerializeField] private Button _checkBtn;
    [SerializeField] private Button _createBtn;
    [SerializeField] private Button _exitBtn;

    private string _lastCheckedEmail;
    private bool _isLastCheckedEmailAvailable;

    [Header("Scene Transition")]
    [SerializeField] private bool _loadNextSceneOnLogin = true;
    [SerializeField] private string _characterCreationSceneName = "Scene_CharacterCreation";
    [SerializeField] private string _nextSceneName = "Scene_WaitingRoom";
    private bool _requestedSceneLoad;

    // 로그인 상태 이벤트 구독
    private void OnEnable() => FirebaseAuthManager.Instance._loginState += OnChangedState;

    private void OnDisable() => FirebaseAuthManager.Instance._loginState -= OnChangedState;

    private void Start()
    {
        // Firebase 초기화
        FirebaseAuthManager.Instance.Init();

        AutoWirePagesIfNeeded();

        if (_showLoginPageOnStart)
            SetJoinPageActive(false);
        else
            SetLoginFlowVisible(false);

        // 버튼 클릭 이벤트 코드에서 등록
        if (_logInBtn != null) _logInBtn.onClick.AddListener(OnLogInClicked);
        if (_logOutBtn != null) _logOutBtn.onClick.AddListener(OnLogOutClicked);
        if (_joinBtn != null) _joinBtn.onClick.AddListener(OpenJoinPage);
        if (_checkBtn != null) _checkBtn.onClick.AddListener(OnCheckClicked);
        if (_exitBtn != null) _exitBtn.onClick.AddListener(CloseJoinPage);
        if (_createBtn != null) _createBtn.onClick.AddListener(OnCreateClicked);
    }

    // 로그인 상태 변경 시 UI 갱신
    private void OnChangedState(bool signedIn)
    {
        if (!signedIn)
            _requestedSceneLoad = false;

        if (_outputTxt != null)
        {
            _outputTxt.text = signedIn ? "로그인" : "로그아웃";
            _outputTxt.text += "\nUserID: " + FirebaseAuthManager.Instance._userId;
        }

        if (signedIn && _loadNextSceneOnLogin && !_requestedSceneLoad)
        {
            _requestedSceneLoad = true;

            string targetScene = ResolvePostLoginSceneName();

            if (string.IsNullOrWhiteSpace(targetScene))
            {
                Debug.LogError("[LogInSystem] Next scene name is empty.");
                return;
            }

            SceneManager.LoadScene(targetScene);
        }
    }

    private string ResolvePostLoginSceneName()
    {
        if (DataManager.Instance == null || DataManager.Instance.CurrentUserData == null)
            return _characterCreationSceneName;

        string selectedCharacterId = DataManager.Instance.CurrentUserData.selectedCharacterId;
        if (string.IsNullOrWhiteSpace(selectedCharacterId))
            return _characterCreationSceneName;

        return _nextSceneName;
    }

    // 버튼 이벤트 콜백
    private void OnLogInClicked()
    {
        string email = _inputLogInEmail != null ? _inputLogInEmail.text : string.Empty;
        string password = _inputLogInPassword != null ? _inputLogInPassword.text : string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            if (_outputTxt != null) _outputTxt.text = "이메일/비밀번호를 입력하세요.";
            return;
        }

        FirebaseAuthManager.Instance.LogIn(email, password);
    }

    private void OnLogOutClicked() => FirebaseAuthManager.Instance.LogOut();

    private void OnCreateClicked()
    {
        string nickname = _inputJoinUserNickname != null ? _inputJoinUserNickname.text : string.Empty;
        string email = _inputJoinUserEmail != null ? _inputJoinUserEmail.text : string.Empty;
        string password = _inputJoinUserPassword != null ? _inputJoinUserPassword.text : string.Empty;
        string confirm = _inputJoinUserConfirmPassword != null ? _inputJoinUserConfirmPassword.text : string.Empty;

        if (string.IsNullOrWhiteSpace(nickname))
        {
            // 닉네임 입력 UI가 아직 없거나 비어있다면 이메일 아이디 부분을 기본 닉네임으로 사용
            int at = email.IndexOf('@');
            nickname = at > 0 ? email.Substring(0, at) : "NewPlayer";
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            if (_outputTxt != null) _outputTxt.text = "이메일/비밀번호를 입력하세요.";
            return;
        }

        if (!string.Equals(password, confirm))
        {
            if (_outputTxt != null) _outputTxt.text = "비밀번호 확인이 일치하지 않습니다.";
            return;
        }

        if (password.Length < 6)
        {
            if (_outputTxt != null) _outputTxt.text = "비밀번호는 6자 이상이어야 합니다.";
            return;
        }

        if (DataManager.Instance != null)
        {
            DataManager.Instance.CurrentUserData.nickname = nickname;
        }

        if (_lastCheckedEmail == email && _isLastCheckedEmailAvailable)
        {
            FirebaseAuthManager.Instance.Create(email, password, nickname);
            return;
        }

        FirebaseAuthManager.Instance.CheckEmailExists(email, (success, exists) =>
        {
            if (!success)
            {
                if (_outputTxt != null) _outputTxt.text = "중복 확인 실패. 네트워크 상태를 확인하세요.";
                return;
            }

            if (exists)
            {
                _isLastCheckedEmailAvailable = false;
                if (_outputTxt != null) _outputTxt.text = "이미 사용 중인 이메일입니다.";
                return;
            }

            _lastCheckedEmail = email;
            _isLastCheckedEmailAvailable = true;
            FirebaseAuthManager.Instance.Create(email, password, nickname);
        });
    }

    private void OnCheckClicked()
    {
        string email = _inputJoinUserEmail != null ? _inputJoinUserEmail.text : string.Empty;

        if (string.IsNullOrWhiteSpace(email))
        {
            if (_outputTxt != null) _outputTxt.text = "중복 확인할 이메일을 입력하세요.";
            return;
        }

        FirebaseAuthManager.Instance.CheckEmailExists(email, (success, exists) =>
        {
            if (!success)
            {
                _isLastCheckedEmailAvailable = false;
                if (_outputTxt != null) _outputTxt.text = "중복 확인 실패. 잠시 후 다시 시도하세요.";
                return;
            }

            _lastCheckedEmail = email;
            _isLastCheckedEmailAvailable = !exists;

            if (_outputTxt == null)
                return;

            _outputTxt.text = exists
                ? "이미 사용 중인 이메일입니다."
                : "사용 가능한 이메일입니다.";
        });
    }

    private void OpenJoinPage()
    {
        AutoWirePagesIfNeeded();
        SetJoinPageActive(true);

        if (_inputJoinUserEmail != null && _inputLogInEmail != null)
            _inputJoinUserEmail.text = _inputLogInEmail.text;

        _lastCheckedEmail = string.Empty;
        _isLastCheckedEmailAvailable = false;

        // 인스펙터에 안 물려있으면 JoinPage에서 자동으로 찾아봄(옵션)
        if (_inputJoinUserNickname == null && _uiJoinPage != null)
        {
            Transform t = _uiJoinPage.transform.Find("Input_nickname");
            if (t != null)
                _inputJoinUserNickname = t.GetComponent<InputField>();
        }

        if (_exitBtn == null && _uiJoinPage != null)
        {
            Transform t = _uiJoinPage.transform.Find("Btn_Exit");
            if (t != null)
                _exitBtn = t.GetComponent<Button>();

            if (_exitBtn != null)
                _exitBtn.onClick.AddListener(CloseJoinPage);
        }
    }

    private void CloseJoinPage()
    {
        AutoWirePagesIfNeeded();
        SetJoinPageActive(false);
        _lastCheckedEmail = string.Empty;
        _isLastCheckedEmailAvailable = false;
    }

    private void SetJoinPageActive(bool isActive)
    {
        if (_uiJoinPage != null)
            _uiJoinPage.SetActive(isActive);

        if (_uiLoginPage != null)
            _uiLoginPage.SetActive(!isActive);
    }

    private void SetLoginFlowVisible(bool isVisible)
    {
        if (_uiJoinPage != null)
            _uiJoinPage.SetActive(false);

        if (_uiLoginPage != null)
            _uiLoginPage.SetActive(isVisible);
    }

    private void AutoWirePagesIfNeeded()
    {
        if (_uiLoginPage == null)
            _uiLoginPage = GameObject.Find("UI_LogInPage");

        if (_uiJoinPage == null)
            _uiJoinPage = GameObject.Find("UI_JoinPage");
    }
}
