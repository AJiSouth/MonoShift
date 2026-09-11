using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    public Button startButton;
    public Button quitButton;
    public Button settingButton;
    public SettingPanel settingPanel;

    [Header("BGM")]
    public AudioClip mainMenuBGM;

    void Start()
    {
        if (startButton != null)
            startButton.onClick.AddListener(OnStartClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);

        if (settingButton != null)
            settingButton.onClick.AddListener(OnSettingClicked);

        if (AudioManager.Instance != null && mainMenuBGM != null)
            AudioManager.Instance.PlayBGM(mainMenuBGM);
    }

    void OnStartClicked()
    {
        LevelManager.GoToLevelSelect();
    }

    void OnQuitClicked()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void OnSettingClicked()
    {
        if (settingPanel != null)
            settingPanel.Open();
    }
}