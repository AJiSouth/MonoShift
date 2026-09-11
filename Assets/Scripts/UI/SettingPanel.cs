using UnityEngine;
using UnityEngine.UI;

public class SettingPanel : MonoBehaviour
{
    [Header("UI 组件")]
    public Slider bgmSlider;
    public Toggle fullscreenToggle;
    public Button closeButton;

    private void Start()
    {
        // 从 PlayerPrefs 读取已保存的设置
        float savedVolume = PlayerPrefs.GetFloat("BGMVolume", 0.5f);
        bool savedFullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;

        // 初始化 UI 并绑定事件
        if (bgmSlider != null)
        {
            bgmSlider.value = savedVolume;
            bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = savedFullscreen;
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        }

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        // 初始隐藏
        gameObject.SetActive(false);
    }

    // 打开面板
    public void Open()
    {
        gameObject.SetActive(true);
    }

    // 关闭面板
    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void OnBGMVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetVolume(value);
        else
        {
            PlayerPrefs.SetFloat("BGMVolume", value);
            PlayerPrefs.Save();
        }
    }

    private void OnFullscreenChanged(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }
}