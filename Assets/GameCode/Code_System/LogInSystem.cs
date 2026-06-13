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
    private void OnEnable()
    {
        if (FirebaseAuthManager.Instance != null)
            FirebaseAuthManager.Instance._loginState += OnChangedState;
    }

    private void OnDisable()
    {
        if (FirebaseAuthManager.Instance != null)
            FirebaseAuthManager.Instance._loginState -= OnChangedState;
    }

    private void Start()
    {
        // Firebase 초기화
        if (FirebaseAuthManager.Instance != null)
            FirebaseAuthManager.Instance.Init();
        else
            Debug.LogError("[LogInSystem] FirebaseAuthManager.Instance is null on Start.");

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
        if (FirebaseAuthManager.Instance == null)
            return;

        if (!signedIn)
            _requestedSceneLoad = false;

        if (_outputTxt != null)
        {
            _outputTxt.text = signedIn ? "\uB85C\uADF8\uC778" : "\uB85C\uADF8\uC544\uC6C3";
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
        if (FirebaseAuthManager.Instance == null)
        {
            Debug.LogError("[LogInSystem] FirebaseAuthManager.Instance is null.");
            return;
        }

        string email = _inputLogInEmail != null ? _inputLogInEmail.text : string.Empty;
        string password = _inputLogInPassword != null ? _inputLogInPassword.text : string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            if (_outputTxt != null) _outputTxt.text = "\uC774\uBA54\uC77C/\uBE44\uBC00\uBC88\uD638\uB97C \uC785\uB825\uD558\uC138\uC694.";
            return;
        }

        FirebaseAuthManager.Instance.LogIn(email, password);
    }

    private void OnLogOutClicked()
    {
        if (FirebaseAuthManager.Instance != null)
            FirebaseAuthManager.Instance.LogOut();
    }

    private void OnCreateClicked()
    {
        if (FirebaseAuthManager.Instance == null)
        {
            Debug.LogError("[LogInSystem] FirebaseAuthManager.Instance is null.");
            return;
        }

        string nickname = _inputJoinUserNickname != null ? _inputJoinUserNickname.text : string.Empty;
        string email = _inputJoinUserEmail != null ? _inputJoinUserEmail.text : string.Empty;
        string password = _inputJoinUserPassword != null ? _inputJoinUserPassword.text : string.Empty;
        string confirm = _inputJoinUserConfirmPassword != null ? _inputJoinUserConfirmPassword.text : string.Empty;

        if (string.IsNullOrWhiteSpace(nickname))
        {
            int at = email.IndexOf('@');
            nickname = at > 0 ? email.Substring(0, at) : "NewPlayer";
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            if (_outputTxt != null) _outputTxt.text = "\uC774\uBA54\uC77C/\uBE44\uBC00\uBC88\uD638\uB97C \uC785\uB825\uD558\uC138\uC694.";
            return;
        }

        if (!string.Equals(password, confirm))
        {
            if (_outputTxt != null) _outputTxt.text = "\uBE44\uBC00\uBC88\uD638 \uD655\uC778\uC774 \uC77C\uCE58\uD558\uC9C0 \uC54A\uC2B5\uB2C8\uB2E4.";
            return;
        }

        if (password.Length < 6)
        {
            if (_outputTxt != null) _outputTxt.text = "\uBE44\uBC00\uBC88\uD638\uB294 6\uC790 \uC774\uC0C1\uC774\uC5B4\uC57C \uD569\uB2C8\uB2E4.";
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
                if (_outputTxt != null) _outputTxt.text = "\uC911\uBCF5 \uD655\uC778 \uC2E4\uD328. \uB124\uD2B8\uC6CC\uD06C \uC0C1\uD0DC\uB97C \uD655\uC778\uD558\uC138\uC694.";
                return;
            }

            if (exists)
            {
                _isLastCheckedEmailAvailable = false;
                if (_outputTxt != null) _outputTxt.text = "\uC774\uBBF8 \uC0AC\uC6A9 \uC911\uC778 \uC774\uBA54\uC77C\uC785\uB2C8\uB2E4.";
                return;
            }

            _lastCheckedEmail = email;
            _isLastCheckedEmailAvailable = true;
            FirebaseAuthManager.Instance.Create(email, password, nickname);
        });
    }

    private void OnCheckClicked()
    {
        if (FirebaseAuthManager.Instance == null)
        {
            Debug.LogError("[LogInSystem] FirebaseAuthManager.Instance is null.");
            return;
        }

        string email = _inputJoinUserEmail != null ? _inputJoinUserEmail.text : string.Empty;

        if (string.IsNullOrWhiteSpace(email))
        {
            if (_outputTxt != null) _outputTxt.text = "\uC911\uBCF5 \uD655\uC778\uD560 \uC774\uBA54\uC77C\uC744 \uC785\uB825\uD558\uC138\uC694.";
            return;
        }

        FirebaseAuthManager.Instance.CheckEmailExists(email, (success, exists) =>
        {
            if (!success)
            {
                _isLastCheckedEmailAvailable = false;
                if (_outputTxt != null) _outputTxt.text = "\uC911\uBCF5 \uD655\uC778 \uC2E4\uD328. \uC7A0\uC2DC \uD6C4 \uB2E4\uC2DC \uC2DC\uB3C4\uD558\uC138\uC694.";
                return;
            }

            _lastCheckedEmail = email;
            _isLastCheckedEmailAvailable = !exists;

            if (_outputTxt == null)
                return;

            _outputTxt.text = exists
                ? "\uC774\uBBF8 \uC0AC\uC6A9 \uC911\uC778 \uC774\uBA54\uC77C\uC785\uB2C8\uB2E4."
                : "\uC0AC\uC6A9 \uAC00\uB2A5\uD55C \uC774\uBA54\uC77C\uC785\uB2C8\uB2E4.";
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
            Debug.LogWarning($"[LogInSystem] {nameof(_uiLoginPage)} is not assigned in Inspector.");

        if (_uiJoinPage == null)
            Debug.LogWarning($"[LogInSystem] {nameof(_uiJoinPage)} is not assigned in Inspector.");
    }
