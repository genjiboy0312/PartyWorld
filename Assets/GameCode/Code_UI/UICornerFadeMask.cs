using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(Graphic))]
[AddComponentMenu("UI/UICornerFadeMask")]
public class UICornerFadeMask : MonoBehaviour
{
    public enum FadeDirection
    {
        None = 0,
        Left = 1,
        Right = 2,
        Top = 3,
        Bottom = 4
    }

    [Header("Round")]
    [Tooltip("모서리 둥글기 반경(px)입니다. 값이 클수록 코너가 더 둥글어집니다.")]
    [SerializeField, Min(0f)] private float _radius = 24f;
    [Tooltip("라운드 경계의 부드러움입니다. 값이 클수록 가장자리가 더 그라데이션처럼 보입니다.")]
    [SerializeField, Range(0f, 10f)] private float _feather = 1f;

    [Header("Directional Fade")]
    [Tooltip("방향성 페이드 기준 방향입니다. Left는 왼쪽이 선명, Right는 오른쪽이 선명합니다.")]
    [SerializeField] private FadeDirection _fadeDirection = FadeDirection.None;
    [Tooltip("방향성 페이드 강도입니다. 0은 없음, 1에 가까울수록 반대편이 더 많이 흐려집니다.")]
    [SerializeField, Range(0f, 1f)] private float _fadeStrength = 0f;

    [Header("Optional")]
    [Tooltip("라운드 마스크에 사용할 셰이더입니다. 비워두면 기본(UI/UIRoundMask)을 사용합니다.")]
    [SerializeField] private Shader _roundShader;

    private static readonly int SizeId = Shader.PropertyToID("_UIRound_Size");
    private static readonly int RadiusId = Shader.PropertyToID("_UIRound_Radius");
    private static readonly int FeatherId = Shader.PropertyToID("_UIRound_Feather");
    private static readonly int FadeDirId = Shader.PropertyToID("_UIRound_FadeDir");
    private static readonly int FadeStrengthId = Shader.PropertyToID("_UIRound_FadeStrength");

    private Graphic _graphic;
    private RectTransform _rect;
    private Material _runtimeMaterial;

    private void Awake()
    {
        Cache();
        EnsureMaterial();
        Apply();
    }

    private void OnEnable()
    {
        Cache();
        EnsureMaterial();
        Apply();
    }

    private void OnDisable()
    {
        if (_graphic != null)
            _graphic.material = null;
    }

    private void OnDestroy()
    {
        if (_runtimeMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(_runtimeMaterial);
            else
                DestroyImmediate(_runtimeMaterial);
        }
    }

    private void OnRectTransformDimensionsChange()
    {
        Apply();
    }

    private void OnValidate()
    {
        Cache();
        EnsureMaterial();
        Apply();
    }

    private void Cache()
    {
        if (_graphic == null)
            _graphic = GetComponent<Graphic>();

        if (_rect == null)
            _rect = transform as RectTransform;
    }

    private void EnsureMaterial()
    {
        if (_graphic == null)
            return;

        if (_roundShader == null)
            _roundShader = Shader.Find("UI/UIRoundMask");

        if (_roundShader == null)
        {
            Debug.LogWarning("[UICornerFadeMask] Shader 'UI/UIRoundMask' not found.", this);
            return;
        }

        if (_runtimeMaterial == null || _runtimeMaterial.shader != _roundShader)
        {
            if (_runtimeMaterial != null)
            {
                if (Application.isPlaying)
                    Destroy(_runtimeMaterial);
                else
                    DestroyImmediate(_runtimeMaterial);
            }

            _runtimeMaterial = new Material(_roundShader)
            {
                name = "UICornerFadeMask_Runtime"
            };
        }

        _graphic.material = _runtimeMaterial;
    }

    private void Apply()
    {
        if (_runtimeMaterial == null || _rect == null)
            return;

        Rect r = _rect.rect;
        float width = Mathf.Max(1f, r.width);
        float height = Mathf.Max(1f, r.height);
        float maxRadius = Mathf.Min(width, height) * 0.5f;

        float clampedRadius = Mathf.Clamp(_radius, 0f, maxRadius);
        float clampedFeather = Mathf.Clamp(_feather, 0f, 10f);

        _runtimeMaterial.SetVector(SizeId, new Vector4(width, height, 0f, 0f));
        _runtimeMaterial.SetFloat(RadiusId, clampedRadius);
        _runtimeMaterial.SetFloat(FeatherId, clampedFeather);
        _runtimeMaterial.SetFloat(FadeDirId, (float)_fadeDirection);
        _runtimeMaterial.SetFloat(FadeStrengthId, Mathf.Clamp01(_fadeStrength));

        if (_graphic != null)
            _graphic.SetMaterialDirty();
    }
}
