using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameUI : MonoBehaviour
{
    public TextMeshProUGUI stepText;
    public GameObject winPanel;
    public Button nextLevelButton;
    public Button winMenuButton;
    public Button settingButton;      // ← 新增
    public SettingPanel settingPanel; // ← 新增

    private GridManager gridManager;

    void Start()
    {
        gridManager = GridManager.Instance;
        if (gridManager != null)
        {
            gridManager.OnStepChanged += UpdateStepUI;
            UpdateStepUI(gridManager.stepCount);
        }

        if (winPanel != null) winPanel.SetActive(false);
        if (nextLevelButton != null) nextLevelButton.onClick.AddListener(OnNextLevel);
        if (winMenuButton != null) winMenuButton.onClick.AddListener(OnMainMenu);
        if (settingButton != null) settingButton.onClick.AddListener(OnSettingClicked);
    }

    void UpdateStepUI(int steps)
    {
        if (stepText != null)
            stepText.text = steps.ToString();
    }

    public void ShowWinPanel()
    {
        if (winPanel != null)
            winPanel.SetActive(true);

        LevelManager.CompleteLevel();
    }

    void OnNextLevel()
    {
        //UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene02");
        LevelManager.LoadNextLevel();
    }

    void OnMainMenu()
    {
        LevelManager.GoToMainMenu();
    }

    void OnSettingClicked()
    {
        if (settingPanel != null)
            settingPanel.Open();
    }

    void OnDestroy()
    {
        if (gridManager != null)
            gridManager.OnStepChanged -= UpdateStepUI;
    }
}