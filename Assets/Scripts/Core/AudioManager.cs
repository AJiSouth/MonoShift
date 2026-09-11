using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("BGM")]
    public AudioSource bgmSource;

    [Header("音量设置（0-1）")]
    [Range(0f, 1f)] public float bgmVolume = 0.5f;

    private void Awake()
    {
        // 单例 + 跨场景不销毁
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
    }

    private void Start()
    {
        if (bgmSource != null)
        {
            bgmSource.volume = bgmVolume;
            bgmSource.loop = true;
        }
    }

    // 播放 BGM
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSource == null) return;

        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.Play();
    }

    // 停止
    public void StopBGM()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    // 设置音量
    public void SetVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmSource != null) bgmSource.volume = bgmVolume;
    }
}