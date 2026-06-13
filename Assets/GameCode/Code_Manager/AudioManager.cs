using System;
using UnityEngine;

// 다른 스크립트에서 AudioManager.Instance.함수명 으로 호출 가능
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        GameObject go = new GameObject(nameof(AudioManager));
        go.AddComponent<AudioSource>(); // Music Source
        go.AddComponent<AudioSource>(); // SFX Source
        go.AddComponent<AudioManager>();
    }

    [Header("Sound 설정")]
    [SerializeField] private Sound[] _musicSounds;
    [SerializeField] private Sound[] _sfxSounds;

    private AudioSource _musicSource;
    private AudioSource _sfxSource;

    private void Awake()
    {
        // 싱글톤 처리
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // AudioSource 컴포넌트 할당
        AudioSource[] sources = GetComponents<AudioSource>();
        while (sources.Length < 2)
        {
            gameObject.AddComponent<AudioSource>();
            sources = GetComponents<AudioSource>();
        }

        if (sources.Length >= 2)
        {
            _musicSource = sources[0];
            _sfxSource = sources[1];
        }
        else
        {
            Debug.LogError("AudioManager: 필요한 AudioSource 컴포넌트 2개를 찾을 수 없습니다.");
        }
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        // 기본 BGM 재생
        PlayMusic("BGM_01");
    }

    /// <summary>
    /// 배경음악 재생
    /// </summary>
    public void PlayMusic(string name)
    {
        if (_musicSource == null) return;
        if (_musicSounds == null || _musicSounds.Length == 0)
        {
            Debug.LogWarning("AudioManager: music sound array is empty.");
            return;
        }

        Sound sound = Array.Find(_musicSounds, x => x._name == name);
        if (sound == null)
        {
            Debug.LogWarning("AudioManager: 요청한 음악을 찾을 수 없습니다. 이름: " + name);
            return;
        }

        if (_musicSource.clip == sound._clip && _musicSource.isPlaying) return;

        _musicSource.clip = sound._clip;
        _musicSource.loop = true;
        _musicSource.Play();
    }

    /// <summary>
    /// 효과음 재생
    /// </summary>
    public void PlaySFX(string name)
    {
        if (_sfxSource == null) return;
        if (_sfxSounds == null || _sfxSounds.Length == 0)
        {
            Debug.LogWarning("AudioManager: sfx sound array is empty.");
            return;
        }

        Sound sound = Array.Find(_sfxSounds, x => x._name == name);
        if (sound == null)
        {
            Debug.LogWarning("AudioManager: 요청한 SFX를 찾을 수 없습니다. 이름: " + name);
            return;
        }

        _sfxSource.PlayOneShot(sound._clip);
    }

    public void ToggleMusic() => _musicSource.mute = !_musicSource.mute;
    public void ToggleSFX() => _sfxSource.mute = !_sfxSource.mute;

    public void MusicVolume(float volume)
    {
        if (_musicSource != null) _musicSource.volume = Mathf.Clamp01(volume);
    }

    public void SFXVolume(float volume)
    {
        if (_sfxSource != null) _sfxSource.volume = Mathf.Clamp01(volume);
    }
}
