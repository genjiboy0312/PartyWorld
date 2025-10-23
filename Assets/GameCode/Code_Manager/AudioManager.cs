using System;
using UnityEngine;

// 다른 스크립트에서 AudioManager.Instance.함수명 으로 호출 가능
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Sound 설정")]
    [SerializeField] private Sound[] _musicSounds;
    [SerializeField] private Sound[] _sfxSounds;

    [Header("AudioSource")]
    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private AudioSource _sfxSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (_musicSource == null || _sfxSource == null)
            Debug.LogWarning("AudioManager: AudioSource가 할당되지 않았습니다.");
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

        Sound sound = Array.Find(_musicSounds, x => x._name == name);
        if (sound == null)
        {
            Debug.LogWarning("AudioManager: 요청한 음악을 찾을 수 없습니다. 이름: " + name);
            return;
        }

        // 이미 재생 중인 음악이면 재생하지 않음
        if (_musicSource.clip == sound._clip && _musicSource.isPlaying) return;

        _musicSource.clip = sound._clip;
        _musicSource.loop = true; // 기본 루프 설정
        _musicSource.Play();
    }

    /// <summary>
    /// 효과음 재생
    /// </summary>
    public void PlaySFX(string name)
    {
        if (_sfxSource == null) return;

        Sound sound = Array.Find(_sfxSounds, x => x._name == name);
        if (sound == null)
        {
            Debug.LogWarning("AudioManager: 요청한 SFX를 찾을 수 없습니다. 이름: " + name);
            return;
        }

        _sfxSource.PlayOneShot(sound._clip);
    }

    /// <summary>
    /// 배경음/효과음 음소거 토글
    /// </summary>
    public void ToggleMusic() => _musicSource.mute = !_musicSource.mute;
    public void ToggleSFX() => _sfxSource.mute = !_sfxSource.mute;

    /// <summary>
    /// 볼륨 조절
    /// </summary>
    public void MusicVolume(float volume)
    {
        if (_musicSource != null) _musicSource.volume = Mathf.Clamp01(volume);
    }

    public void SFXVolume(float volume)
    {
        if (_sfxSource != null) _sfxSource.volume = Mathf.Clamp01(volume);
    }
}
