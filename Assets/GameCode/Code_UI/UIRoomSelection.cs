using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 방 선택 팝업의 UI만 담당하는 컨트롤러.
/// 방 데이터/Photon 로비 관리는 RoomManager에 위임.
/// </summary>
public class UIRoomSelection : MonoBehaviour
{
    [Header("Scene References - Drag & Drop")]
    [SerializeField] private GameObject _roomSelectionUI;
    [SerializeField] private GameObject _panel;
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private Transform _roomListContent;
    [SerializeField] private GameObject _roomEntryTemplate;
    [SerializeField] private GameObject _noRoomsLabel;
    [SerializeField] private GameObject _createSection;
    [SerializeField] private InputField _roomNameInput;
    [SerializeField] private Button _confirmCreateBtn;
    [SerializeField] private Button _cancelCreateBtn;
    [SerializeField] private Button _createRoomBtn;
    [SerializeField] private Button _quickMatchBtn;
    [SerializeField] private Button _refreshBtn;
    [SerializeField] private Button _closeBtn;
    [SerializeField] private Text _statusText;

    // ── State ──
    private bool _isCreatingRoom;
    private System.Action<string> _onStatusMessageHandler;

    // ── Lifecycle ──

    private void Awake()
    {
        FindReferences();
        HookupListeners();

        if (RoomManager.Instance != null)
        {
            _onStatusMessageHandler = (msg) => SetStatusText(msg);
            RoomManager.Instance.OnRoomListChanged += OnRoomListChanged;
            RoomManager.Instance.OnStatusMessage += _onStatusMessageHandler;
        }
    }

    private void OnDestroy()
    {
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.OnRoomListChanged -= OnRoomListChanged;
            if (_onStatusMessageHandler != null)
                RoomManager.Instance.OnStatusMessage -= _onStatusMessageHandler;
        }
    }

    // ── Reference Finder ──

    private void FindReferences()
    {
        var t = transform;
        if (_panel == null) _panel = t.Find("MainPanel")?.gameObject;
        if (_scrollRect == null) _scrollRect = t.GetComponentInChildren<ScrollRect>(true);
        if (_roomListContent == null && _scrollRect != null)
        {
            var vp = _scrollRect.transform.Find("Viewport");
            if (vp != null) _roomListContent = vp.Find("Content");
        }
        if (_roomEntryTemplate == null && _roomListContent != null)
            _roomEntryTemplate = _roomListContent.Find("RoomEntryTemplate")?.gameObject;
        if (_noRoomsLabel == null && _roomListContent != null)
            _noRoomsLabel = _roomListContent.Find("NoRoomsLabel")?.gameObject;
        if (_createSection == null) _createSection = t.Find("MainPanel/CreateSection")?.gameObject;
        if (_roomNameInput == null && _createSection != null)
            _roomNameInput = _createSection.GetComponentInChildren<InputField>(true);
        if (_confirmCreateBtn == null && _createSection != null)
            _confirmCreateBtn = _createSection.transform.Find("ConfirmCreateBtn")?.GetComponent<Button>();
        if (_cancelCreateBtn == null && _createSection != null)
            _cancelCreateBtn = _createSection.transform.Find("CancelCreateBtn")?.GetComponent<Button>();
        if (_createRoomBtn == null) _createRoomBtn = t.Find("MainPanel/BottomBar/CreateRoomBtn")?.GetComponent<Button>();
        if (_quickMatchBtn == null) _quickMatchBtn = t.Find("MainPanel/BottomBar/QuickMatchBtn")?.GetComponent<Button>();
        if (_refreshBtn == null) _refreshBtn = t.Find("MainPanel/BottomBar/RefreshBtn")?.GetComponent<Button>();
        if (_closeBtn == null) _closeBtn = t.Find("MainPanel/Header/CloseBtn")?.GetComponent<Button>();
        if (_statusText == null) _statusText = t.Find("MainPanel/StatusText")?.GetComponent<Text>();
    }

    private void HookupListeners()
    {
        if (_closeBtn != null) _closeBtn.onClick.AddListener(CloseButton);
        if (_createRoomBtn != null) _createRoomBtn.onClick.AddListener(ShowCreateRoom);
        if (_quickMatchBtn != null) _quickMatchBtn.onClick.AddListener(RandomJoinButton);
        if (_refreshBtn != null) _refreshBtn.onClick.AddListener(RefreshRoomList);
        if (_confirmCreateBtn != null) _confirmCreateBtn.onClick.AddListener(ConfirmCreateRoom);
        if (_cancelCreateBtn != null) _cancelCreateBtn.onClick.AddListener(CancelCreateRoom);
        if (_roomNameInput != null) _roomNameInput.onValueChanged.AddListener(OnRoomNameChanged);
    }

    // ── Public API (called from UIPlay) ──

    public void Open()
    {
        SetRoomSelectionActive(true);
        if (RoomManager.Instance != null)
            RoomManager.Instance.OpenRoomList();
    }

    public void CloseButton()
    {
        SetRoomSelectionActive(false);
    }

    // ── RoomManager Event Handlers ──

    private void OnRoomListChanged()
    {
        RefreshRoomListUI();
    }

    private void OnStatusMessage(string message)
    {
        SetStatusText(message);
    }

    // ── Room List UI Rendering ──

    private void RefreshRoomListUI()
    {
        if (_roomListContent == null) return;

        // Clear existing entries (keep template and no-rooms label)
        var toRemove = new List<GameObject>();
        foreach (Transform child in _roomListContent)
        {
            if (child.gameObject == _roomEntryTemplate || child.gameObject == _noRoomsLabel)
                continue;
            toRemove.Add(child.gameObject);
        }
        foreach (var go in toRemove)
            Destroy(go);

        var roomList = RoomManager.Instance != null
            ? RoomManager.Instance.RoomList
            : System.Array.Empty<RoomData>();

        int totalCount = roomList.Count;

        if (_noRoomsLabel != null)
            _noRoomsLabel.SetActive(totalCount == 0);

        if (totalCount > 0)
            SetStatusText($"{totalCount}개의 방 발견");
        else
            SetStatusText("");

        for (int i = 0; i < totalCount; i++)
        {
            RoomData data = roomList[i];
            string roomName = data.roomName;
            bool isVirtual = data.isVirtual;

            GameObject entry = Instantiate(_roomEntryTemplate, _roomListContent);
            entry.name = $"RoomEntry_{roomName}";
            entry.SetActive(true);

            Text numText = entry.transform.Find("RoomNumber")?.GetComponent<Text>();
            Text nameText = entry.transform.Find("RoomName")?.GetComponent<Text>();
            Text countText = entry.transform.Find("PlayerCount")?.GetComponent<Text>();
            if (numText != null) numText.text = $"{data.roomNumber}";
            if (nameText != null) nameText.text = roomName;
            if (countText != null) countText.text = isVirtual ? "—/—" : $"{data.playerCount}/{data.maxPlayers}";

            Image bg = entry.GetComponent<Image>();
            if (bg != null)
                bg.color = isVirtual ? new Color(0.85f, 0.9f, 1.0f) : Color.white;

            Button btn = entry.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = true;
                string capturedName = roomName;
                if (isVirtual)
                    btn.onClick.AddListener(() => RoomManager.Instance?.CreateVirtualRoom(capturedName));
                else
                    btn.onClick.AddListener(() => RoomManager.Instance?.JoinRoom(capturedName));
            }
        }
    }

    // ── Create Room UI ──

    private void ShowCreateRoom()
    {
        _isCreatingRoom = true;
        if (_createSection != null) _createSection.SetActive(true);
        if (_roomNameInput != null)
        {
            _roomNameInput.text = "";
            _roomNameInput.ActivateInputField();
        }
        if (_confirmCreateBtn != null) _confirmCreateBtn.interactable = false;
        SetStatusText("");
    }

    private void CancelCreateRoom()
    {
        SetCreateRoomMode(false);
    }

    private void OnRoomNameChanged(string value)
    {
        if (_confirmCreateBtn != null)
            _confirmCreateBtn.interactable = !string.IsNullOrWhiteSpace(value);
    }

    private void ConfirmCreateRoom()
    {
        string roomName = _roomNameInput?.text?.Trim();
        if (string.IsNullOrWhiteSpace(roomName))
        {
            SetStatusText("방 이름을 입력해주세요.", true);
            return;
        }

        RoomManager.Instance?.RequestCreateRoom(roomName);
        SetCreateRoomMode(false);
    }

    private void SetCreateRoomMode(bool active)
    {
        _isCreatingRoom = active;
        if (_createSection != null)
            _createSection.SetActive(active);
    }

    // ── Quick Match ──

    private void RandomJoinButton()
    {
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.RequestQuickMatch();
            SetRoomSelectionActive(false);
            return;
        }

        SetStatusText("RoomManager를 찾을 수 없습니다.", true);
    }

    // ── Refresh ──

    private void RefreshRoomList()
    {
        RoomManager.Instance?.RequestRefresh();
    }

    // ── Helpers ──

    private void SetRoomSelectionActive(bool isActive)
    {
        if (_roomSelectionUI != null)
            _roomSelectionUI.SetActive(isActive);

        if (isActive)
        {
            if (RoomManager.Instance != null)
                RoomManager.Instance.OpenRoomList();
        }
        else
        {
            if (RoomManager.Instance != null)
                RoomManager.Instance.CloseRoomList();
            _isCreatingRoom = false;
            if (_createSection != null)
                _createSection.SetActive(false);
        }
    }

    private void SetStatusText(string msg, bool isError = false)
    {
        if (_statusText == null) return;
        _statusText.gameObject.SetActive(!string.IsNullOrEmpty(msg));
        _statusText.text = msg;
        _statusText.color = isError ? new Color(0.85f, 0.2f, 0.2f) : new Color(0.4f, 0.4f, 0.4f);
    }
}
