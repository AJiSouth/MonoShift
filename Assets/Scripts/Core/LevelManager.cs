using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelManager
{
    // 当前要加载的关卡索引
    public static int currentLevelIndex = 0;

    // 所有关卡数据
    public static LevelData[] allLevels;

    // 每关对应的场景名
    private static readonly string[] levelSceneNames = new string[]
    {
        "GameScene",     // 第 1 关（索引 0）
        "GameScene02",   // 第 2 关
        "GameScene03",   // 第 3 关
    };

    // 解锁进度
    public static int UnlockedLevel
    {
        get => PlayerPrefs.GetInt("UnlockedLevel", 0);
        set => PlayerPrefs.SetInt("UnlockedLevel", value);
    }

    // 加载关卡选择场景
    public static void GoToLevelSelect()
    {
        SceneManager.LoadScene("LevelSelect");
    }

    // 加载主菜单
    public static void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    // 加载指定关卡
    public static void LoadLevel(int index)
    {
        if (index < 0 || index >= levelSceneNames.Length)
        {
            Debug.LogError($"关卡索引 {index} 超出范围！");
            return;
        }
        currentLevelIndex = index;
        SceneManager.LoadScene(levelSceneNames[index]);
    }

    // 加载下一关
    public static void LoadNextLevel()
    {
        int nextIndex = currentLevelIndex + 1;
        if (nextIndex < levelSceneNames.Length)
        {
            LoadLevel(nextIndex);
        }
        else
        {
            Debug.Log("已通关所有关卡！");
            GoToLevelSelect();
        }
    }

    // 通关当前关卡，更新解锁进度
    public static void CompleteLevel()
    {
        if (currentLevelIndex + 1 > UnlockedLevel)
        {
            UnlockedLevel = currentLevelIndex + 1;
        }
    }

    public static void SetCurrentIndexBySceneName(string sceneName)
    {
        for (int i = 0; i < levelSceneNames.Length; i++)
        {
            if (levelSceneNames[i] == sceneName)
            {
                currentLevelIndex = i;
                return;
            }
        }
        Debug.LogWarning($"场景 {sceneName} 不在 levelSceneNames 列表中");
    }
}
