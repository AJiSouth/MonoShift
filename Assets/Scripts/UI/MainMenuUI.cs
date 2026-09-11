using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    public Button startButton;
    public Button quitButton;

    [Header("BGM")]
    public AudioClip mainMenuBGM;

    void Start()
    {
        startButton.onClick.AddListener(OnStartClicked);
        quitButton.onClick.AddListener(OnQuitClicked);
        //bgm
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
}