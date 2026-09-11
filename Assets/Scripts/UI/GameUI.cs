using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    public TextMeshProUGUI stepText;
    public GameObject winPanel;
    public Button nextLevelButton;
    public Button winMenuButton;

    private GridManager gridManager;

    void Start()
    {
        gridManager = GridManager.Instance;
        if (gridManager != null)
        {
            gridManager.OnStepChanged += UpdateStepUI;
            UpdateStepUI(gridManager.stepCount);
        }

        winPanel.SetActive(false);
        nextLevelButton.onClick.AddListener(OnNextLevel);
        winMenuButton.onClick.AddListener(OnMainMenu);
    }

    void UpdateStepUI(int steps)
    {
        stepText.text = steps.ToString();
    }

    public void ShowWinPanel()
    {
        winPanel.SetActive(true);
        // 检查是否最后一关
        int nextIndex = LevelManager.currentLevelIndex + 1;
        bool hasNext = nextIndex < LevelManager.allLevels.Length;
        nextLevelButton.gameObject.SetActive(hasNext);
        if (!hasNext)
        {
            // 如果是最后一关，可以改文字提示
            Text btnText = nextLevelButton.GetComponentInChildren<Text>();
            if (btnText != null) btnText.text = "已通关";
            nextLevelButton.interactable = false;
        }
        // 记录通关
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

    void OnDestroy()
    {
        if (gridManager != null)
            gridManager.OnStepChanged -= UpdateStepUI;
    }
}