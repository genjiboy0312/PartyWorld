using UnityEngine;

public enum DeviceTier
{
    Low,
    Medium,
    High
}

public class PlatformQualityManager : MonoBehaviour
{
    public static PlatformQualityManager Instance { get; private set; }

    [Header("Memory Thresholds (MB)")]
    [SerializeField] private int _lowMemoryThresholdMB = 2048;
    [SerializeField] private int _mediumMemoryThresholdMB = 4096;

    [Header("Target Framerate")]
    [SerializeField] private int _lowTargetFps = 30;
    [SerializeField] private int _mediumTargetFps = 30;
    [SerializeField] private int _highTargetFps = 60;

    [Header("Override")]
    [SerializeField] private bool _forceTier = false;
    [SerializeField] private DeviceTier _forcedTier = DeviceTier.Medium;

    public DeviceTier CurrentTier { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CurrentTier = DetectTier();
        ApplyQuality(CurrentTier);

        Debug.Log($"[PlatformQualityManager] Tier={CurrentTier}, RAM={SystemInfo.systemMemorySize}MB, GPU mem={SystemInfo.graphicsMemorySize}MB, CPUs={SystemInfo.processorCount}");
    }

    /// <summary>
    /// Manually override quality tier (called by UI).
    /// </summary>
    public void SetQualityOverride(DeviceTier tier)
    {
        _forceTier = true;
        _forcedTier = tier;
        CurrentTier = tier;
        ApplyQuality(tier);
        Debug.Log($"[PlatformQualityManager] Override set to {tier}");
    }

    private DeviceTier DetectTier()
    {
        if (_forceTier)
            return _forcedTier;

        int ramMB = SystemInfo.systemMemorySize;
        int gpuMemMB = SystemInfo.graphicsMemorySize;

        if (ramMB <= _lowMemoryThresholdMB || (gpuMemMB > 0 && gpuMemMB <= 256))
            return DeviceTier.Low;

        if (ramMB <= _mediumMemoryThresholdMB || (gpuMemMB > 0 && gpuMemMB <= 1024))
            return DeviceTier.Medium;

        return DeviceTier.High;
    }

    private void ApplyQuality(DeviceTier tier)
    {
        switch (tier)
        {
            case DeviceTier.Low:
                QualitySettings.SetQualityLevel(0, true);
                Application.targetFrameRate = _lowTargetFps;
                break;

            case DeviceTier.Medium:
                QualitySettings.SetQualityLevel(1, true);
                Application.targetFrameRate = _mediumTargetFps;
                break;

            case DeviceTier.High:
                QualitySettings.SetQualityLevel(Mathf.Min(QualitySettings.names.Length - 1, 2), true);
                Application.targetFrameRate = _highTargetFps;
                break;
        }
    }
}
