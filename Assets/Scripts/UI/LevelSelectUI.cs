using UnityEngine;
using UnityEngine.UI;
using TMPro;   // 引入 TMP 命名空间
using System.Collections.Generic;

public class LevelSelectUI : MonoBehaviour
{
    public Transform buttonContainer;
    public GameObject levelButtonPrefab;
    public Button backButton;

    private List<Button> levelButtons = new List<Button>();

    void Start()
    {
        LevelManager.allLevels = new LevelData[]
        {
            Resources.Load<LevelData>("Levels/Level_01_Data"),
            // 添加更多关卡
        };

        Debug.Log($"加载了 {LevelManager.allLevels.Length} 个关卡数据");
        for (int i = 0; i < LevelManager.allLevels.Length; i++)
        {
            Debug.Log($"关卡 {i + 1}: {(LevelManager.allLevels[i] == null ? "null" : "有效")}");
        }

        GenerateLevelButtons();
        backButton.onClick.AddListener(OnBackClicked);
    }

    void GenerateLevelButtons()
    {
        if (levelButtonPrefab == null)
        {
            Debug.LogError("Level Button Prefab 未赋值！");
            return;
        }
        if (buttonContainer == null)
        {
            Debug.LogError("Button Container 未赋值！");
            return;
        }

        int unlocked = LevelManager.UnlockedLevel;
        Debug.Log($"解锁进度: {unlocked}");

        for (int i = 0; i < LevelManager.allLevels.Length; i++)
        {
            GameObject btnObj = Instantiate(levelButtonPrefab, buttonContainer);
            Button btn = btnObj.GetComponent<Button>();
            if (btn == null)
            {
                Debug.LogError($"第 {i + 1} 关按钮缺少 Button 组件");
                continue;
            }

            // 尝试获取 Legacy Text 或 TMP
            Text legacyText = btnObj.GetComponentInChildren<Text>();
            TextMeshProUGUI tmpText = btnObj.GetComponentInChildren<TextMeshProUGUI>();

            if (legacyText == null && tmpText == null)
            {
                Debug.LogError($"第 {i + 1} 关按钮缺少 Text 或 TextMeshProUGUI 组件，请检查预制体。");
                continue;
            }

            int index = i;

            // 设置文字
            if (legacyText != null)
                legacyText.text = $"第 {i + 1} 关";
            else if (tmpText != null)
                tmpText.text = $"第 {i + 1} 关";

            // 锁关逻辑
            if (i > unlocked)
            {
                btn.interactable = false;
                if (legacyText != null) legacyText.text += " 未解锁";
                else if (tmpText != null) tmpText.text += " 未解锁";
            }
            else
            {
                btn.onClick.AddListener(() => LevelManager.LoadLevel(index));
            }

            levelButtons.Add(btn);
        }
    }

    void OnBackClicked()
    {
        LevelManager.GoToMainMenu();
    }
}