using UnityEngine;
using UnityEngine.UI;

public class LevelButton : MonoBehaviour
{
    [Header("关卡数据")]
    public LevelData levelData;   // 拖入对应的 LevelData 资产
    public int levelIndex;        // 从 0 开始

    private Button btn;
    private Image img;

    void Start()
    {
        btn = GetComponent<Button>();
        img = GetComponent<Image>();

        if (btn == null)
        {
            Debug.LogError($"LevelButton {gameObject.name} 缺少 Button 组件！");
            return;
        }

        btn.transition = Selectable.Transition.None;

        // 设置关卡图片
        if (img != null && levelData != null && levelData.levelImage != null)
        {
            img.sprite = levelData.levelImage;
            img.preserveAspect = true;
        }

        // 检查是否解锁
        bool isUnlocked = (levelIndex <= LevelManager.UnlockedLevel);

        // 设置交互状态
        btn.interactable = isUnlocked;

        if (!isUnlocked)
        {
            // 未解锁：变灰
            if (img != null)
                img.color = new Color(0.6f, 0.6f, 0.6f, 1f);
        }
        else
        {
            btn.onClick.AddListener(() => LevelManager.LoadLevel(levelIndex));
        }
    }

    // 供外部调用
    public void RefreshState()
    {
        bool isUnlocked = (levelIndex <= LevelManager.UnlockedLevel);
        btn.interactable = isUnlocked;
        if (!isUnlocked && img != null)
            img.color = new Color(0.6f, 0.6f, 0.6f, 1f);
        else if (img != null)
            img.color = Color.white;
    }
}