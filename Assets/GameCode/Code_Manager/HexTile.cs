using UnityEngine;

/// <summary>
/// Hex-A-Gone 스타일 육각형 타일
/// - 플레이어가 밟으면 내구도가 감소
/// - 내구도에 따라 색상이 변화 (Green → Yellow → Orange → Red)
/// - 내구도 0 → 타일이 가라앉음
/// </summary>
public class HexTile : MonoBehaviour
{
    [Header("Tile Settings")]
    [SerializeField] private int _maxDurability = 3;
    [SerializeField] private float _sinkDelay = 2f;
    [SerializeField] private float _sinkSpeed = 0.5f;
    [SerializeField] private float _sinkDepth = 5f;

    [Header("Materials (색상 변화용)")]
    [SerializeField] private Material _greenMaterial;
    [SerializeField] private Material _yellowMaterial;
    [SerializeField] private Material _orangeMaterial;
    [SerializeField] private Material _redMaterial;

    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _stepSound;
    [SerializeField] private AudioClip _sinkSound;

    // 현재 상태
    private int _tileIndex = -1;
    private int _currentDurability;
    private int _playersOnTile = 0;
    private bool _isSinking = false;
    private bool _isSunk = false;
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    private Renderer _renderer;
    private MeshFilter _meshFilter;

    // 프로퍼티
    public int TileIndex => _tileIndex;
    public int CurrentDurability => _currentDurability;
    public int MaxDurability => _maxDurability;
    public bool IsSunk => _isSunk;
    public bool IsSinking => _isSinking;
    public Vector3 OriginalPosition => _originalPosition;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _meshFilter = GetComponent<MeshFilter>();
        _currentDurability = _maxDurability;
        _originalPosition = transform.position;
        _originalRotation = transform.rotation;

        // 프리팹에서 머티리얼 로드 (메쉬는 그대로 유지)
        LoadMaterials();

        UpdateMaterial();
    }

    /// <summary>
    /// 타일 인덱스 설정 (HexArenaManager에서 호출)
    /// </summary>
    public void SetTileIndex(int index)
    {
        _tileIndex = index;
    }

    /// <summary>
    /// 네트워크에서 받은 상태 적용
    /// </summary>
    public void ApplyNetworkState(int durability)
    {
        _currentDurability = durability;
        UpdateMaterial();
    }

    /// <summary>
    /// 머티리얼 로드 (Resources/Materials에서 또는 동적 생성)
    /// </summary>
    private void LoadMaterials()
    {
        // Resources/Materials에서 머티리얼 로드
        Material[] materials = Resources.LoadAll<Material>("Materials/");

        foreach (Material mat in materials)
        {
            if (mat.name.Contains("Green") && _greenMaterial == null)
                _greenMaterial = mat;
            else if (mat.name.Contains("Yellow") && _yellowMaterial == null)
                _yellowMaterial = mat;
            else if (mat.name.Contains("Orange") && _orangeMaterial == null)
                _orangeMaterial = mat;
            else if (mat.name.Contains("Red") && _redMaterial == null)
                _redMaterial = mat;
        }

        // 없으면 동적 생성
        if (_greenMaterial == null)
            _greenMaterial = CreateSolidMaterial(new Color(0.2f, 0.8f, 0.2f));
        if (_yellowMaterial == null)
            _yellowMaterial = CreateSolidMaterial(new Color(0.9f, 0.9f, 0.2f));
        if (_orangeMaterial == null)
            _orangeMaterial = CreateSolidMaterial(new Color(0.9f, 0.5f, 0.2f));
        if (_redMaterial == null)
            _redMaterial = CreateSolidMaterial(new Color(0.9f, 0.2f, 0.2f));
    }

    private void Update()
    {
        if (_isSinking && !_isSunk)
        {
            // 천천히 아래로 이동
            transform.position = Vector3.MoveTowards(
                transform.position,
                _originalPosition + Vector3.down * _sinkDepth,
                _sinkSpeed * Time.deltaTime
            );

            // 목표 위치에 도달하면
            if (transform.position.y <= _originalPosition.y - _sinkDepth + 0.01f)
            {
                _isSunk = true;
                _isSinking = false;
                gameObject.SetActive(false);

                // 아레나 매니저에 알림 (tileIndex 기반)
                NotifySunk();
            }
        }
    }

    /// <summary>
    /// 플레이어가 타일에 진입
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (_isSunk || _isSinking)
            return;

        if (other.CompareTag("Player") || other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            _playersOnTile++;
            OnPlayerEntered();
        }
    }

    /// <summary>
    /// 플레이어가 타일에서 퇴장
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            _playersOnTile--;
            if (_playersOnTile < 0)
                _playersOnTile = 0;

            OnPlayerExited();
        }
    }

    private void OnPlayerEntered()
    {
        // 내구도 감소
        _currentDurability--;

        if (_currentDurability <= 0)
        {
            _currentDurability = 0;
            StartSink();
        }
        else
        {
            UpdateMaterial();
            PlaySound(_stepSound);
        }

        // 아레나 매니저에 알림 (tileIndex 기반)
        if (_tileIndex >= 0 && HexArenaManager.Instance != null)
        {
            HexArenaManager.Instance.OnTileDamaged(_tileIndex, _currentDurability);
        }
    }

    private void OnPlayerExited()
    {
        // 모든 플레이어가 나가면 색상 복원 (내구도가 아직 남아있을 경우)
        if (_playersOnTile == 0 && !_isSinking && !_isSunk)
        {
            UpdateMaterial();
        }
    }

    /// <summary>
    /// 타일 가라앉기 시작
    /// </summary>
    public void StartSink()
    {
        if (_isSinking || _isSunk)
            return;

        StartCoroutine(SinkRoutine());
    }

    private System.Collections.IEnumerator SinkRoutine()
    {
        // 딜레이 후 가라앉기
        yield return new WaitForSeconds(_sinkDelay);

        _isSinking = true;
        UpdateMaterial(); // 빨간색으로 변경
        PlaySound(_sinkSound);
    }

    /// <summary>
    /// 가라앉기 완료 시 HexArenaManager에 알림 (외부에서 호출)
    /// </summary>
    public void NotifySunk()
    {
        if (_tileIndex >= 0 && HexArenaManager.Instance != null)
        {
            HexArenaManager.Instance.HandleTileSunk(_tileIndex);
        }
    }

    /// <summary>
    /// 머티리얼 색상 업데이트
    /// </summary>
    private void UpdateMaterial()
    {
        if (_renderer == null)
            return;

        Material targetMaterial = _greenMaterial;

        if (_isSunk)
        {
            // 사라진 상태
            targetMaterial = _redMaterial;
        }
        else if (_currentDurability <= 0)
        {
            targetMaterial = _redMaterial;
        }
        else if (_currentDurability == 1)
        {
            targetMaterial = _redMaterial;
        }
        else if (_currentDurability == 2)
        {
            targetMaterial = _orangeMaterial;
        }
        else if (_currentDurability == 3)
        {
            targetMaterial = _yellowMaterial;
        }
        else
        {
            targetMaterial = _greenMaterial;
        }

        _renderer.material = targetMaterial;
    }

    /// <summary>
    /// 타일 리셋 (다음 라운드 준비)
    /// </summary>
    public void ResetTile()
    {
        _currentDurability = _maxDurability;
        _playersOnTile = 0;
        _isSinking = false;
        _isSunk = false;
        _sinkSpeed = 0f; // 일시정지

        transform.position = _originalPosition;
        transform.rotation = _originalRotation;
        gameObject.SetActive(true);

        UpdateMaterial();
    }

    /// <summary>
    /// 타일 리셋 완료 (이동 재개)
    /// </summary>
    public void ConfirmReset()
    {
        _sinkSpeed = 0.5f; // 원래 속도 복원
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// 솔리드 컬러 머티리얼 동적 생성
    /// </summary>
    private Material CreateSolidMaterial(Color color)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        return mat;
    }

    /// <summary>
    /// 인스펙터에서 내구도 변경 (디버그용)
    /// </summary>
    public void SetDurability(int value)
    {
        _currentDurability = Mathf.Clamp(value, 0, _maxDurability);
        UpdateMaterial();
    }

    /// <summary>
    /// 강제 가라앉히기
    /// </summary>
    public void ForceSink()
    {
        _currentDurability = 0;
        UpdateMaterial();
        StartSink();
    }
}
