using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("关卡数据")]
    [SerializeField] private LevelData currentLevelData;

    [Header("Tilemap引用")]
    [SerializeField] private Tilemap tilemapBlack;
    [SerializeField] private Tilemap tilemapWhite;

    private Vector2Int gridSize;
    private Transform playerTransform;
    public Vector2Int PlayerGridPos { get; private set; }

    private Dictionary<Vector2Int, Box> boxDict = new Dictionary<Vector2Int, Box>();

    // 按钮系统
    private Dictionary<Vector2Int, BoxColor> buttonDict = new Dictionary<Vector2Int, BoxColor>();
    private Dictionary<Vector2Int, GameObject> buttonObjects = new Dictionary<Vector2Int, GameObject>();

    // 目标点系统
    private Dictionary<Vector2Int, GameObject> goalObjects = new Dictionary<Vector2Int, GameObject>();
    private HashSet<Vector2Int> triggeredGoals = new HashSet<Vector2Int>();

    private bool isProcessing = false;

    // ---- 步数系统 ----
    public int stepCount = 0;
    public System.Action<int> OnStepChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 识别当前关卡索引
        LevelManager.SetCurrentIndexBySceneName(gameObject.scene.name);

        if (currentLevelData == null)
        {
            Debug.LogError("未设置关卡数据！");
            return;
        }

        gridSize = currentLevelData.gridSize;

        // 生成玩家
        GameObject playerPrefab = Resources.Load<GameObject>("Player/Player");
        if (playerPrefab == null)
        {
            Debug.LogError("未找到Player预制体！");
            return;
        }

        PlayerGridPos = currentLevelData.playerStart;
        Vector3 worldPos = new Vector3(PlayerGridPos.x + 0.5f, PlayerGridPos.y + 0.5f, -1);
        GameObject playerObj = Instantiate(playerPrefab, worldPos, Quaternion.identity);
        playerTransform = playerObj.transform;

        // 生成箱子
        foreach (var info in currentLevelData.boxSpawnList)
        {
            SpawnBox(info.gridPos, info.color);
        }

        // 生成按钮
        foreach (var info in currentLevelData.buttonList)
        {
            SpawnButton(info.gridPos, info.color);
        }

        // ---- 生成目标点 ----
        foreach (Vector2Int goalPos in currentLevelData.goalPoints)
        {
            SpawnGoalPoint(goalPos);
        }

        triggeredGoals.Clear();

        // 初始化步数
        stepCount = 0;
        OnStepChanged?.Invoke(stepCount);
    }

    // ---- 生成箱子 ----
    public void SpawnBox(Vector2Int gridPos, BoxColor color)
    {
        if (boxDict.ContainsKey(gridPos))
        {
            Destroy(boxDict[gridPos].gameObject);
            boxDict.Remove(gridPos);
        }

        string prefabPath = (color == BoxColor.Black) ? "Box/Box_Black" : "Box/Box_White";
        GameObject boxPrefab = Resources.Load<GameObject>(prefabPath);
        if (boxPrefab == null)
        {
            Debug.LogError($"未找到箱子预制体：{prefabPath}");
            return;
        }

        Vector3 worldPos = new Vector3(gridPos.x + 0.5f, gridPos.y + 0.5f, -1);
        GameObject boxObj = Instantiate(boxPrefab, worldPos, Quaternion.identity);
        Box box = boxObj.GetComponent<Box>();
        if (box == null)
        {
            Debug.LogError("箱子预制体缺少 Box 组件！");
            return;
        }

        box.gridPos = gridPos;
        boxDict[gridPos] = box;
    }

    // ---- 生成按钮 ----
    private void SpawnButton(Vector2Int gridPos, BoxColor color)
    {
        string prefabPath = (color == BoxColor.Black) ? "Button/Button_Black" : "Button/Button_White";
        GameObject btnPrefab = Resources.Load<GameObject>(prefabPath);
        if (btnPrefab == null)
        {
            Debug.LogError($"未找到按钮预制体：{prefabPath}");
            return;
        }

        Vector3 worldPos = new Vector3(gridPos.x + 0.5f, gridPos.y + 0.5f, -0.5f);
        GameObject btnObj = Instantiate(btnPrefab, worldPos, Quaternion.identity);
        buttonDict[gridPos] = color;
        buttonObjects[gridPos] = btnObj;
    }

    // ---- 生成目标点 ----
    private void SpawnGoalPoint(Vector2Int gridPos)
    {
        GameObject goalPrefab = Resources.Load<GameObject>("Goal/GoalPoint");
        if (goalPrefab == null)
        {
            Debug.LogError("未找到目标点预制体，请放在 Resources/Goal/GoalPoint");
            return;
        }

        Vector3 worldPos = new Vector3(gridPos.x + 0.5f, gridPos.y + 0.5f, -0.8f);
        GameObject goalObj = Instantiate(goalPrefab, worldPos, Quaternion.identity);
        goalObjects[gridPos] = goalObj;
        // 初始为灰色（未激活）
        SpriteRenderer sr = goalObj.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.gray;
    }

    // ---- 按钮染色，返回新箱子 ----
    private Box CheckAndDyeBox(Box box)
    {
        if (isProcessing) return box;

        if (buttonDict.TryGetValue(box.gridPos, out BoxColor buttonColor))
        {
            if (box.color != buttonColor)
            {
                Vector2Int pos = box.gridPos;
                boxDict.Remove(pos);
                Destroy(box.gameObject);
                SpawnBox(pos, buttonColor);
                Debug.Log($"箱子在按钮上，销毁并重新生成，颜色变为 {buttonColor}");
                return boxDict[pos];
            }
        }
        return box;
    }

    // ---- 检测格子是否有按钮 ----
    private bool HasButton(Vector2Int pos)
    {
        return buttonDict.ContainsKey(pos);
    }

    // ---- 检测格子地板颜色 ----
    public BoxColor? GetFloorColor(Vector2Int pos)
    {
        Vector3Int cellPos = new Vector3Int(pos.x, pos.y, 0);
        bool hasBlack = tilemapBlack.HasTile(cellPos);
        bool hasWhite = tilemapWhite.HasTile(cellPos);

        if (hasBlack && hasWhite)
        {
            Debug.LogWarning($"格子 ({pos.x},{pos.y}) 同时有黑白地板，视为白色");
            return BoxColor.White;
        }
        else if (hasBlack)
            return BoxColor.Black;
        else if (hasWhite)
            return BoxColor.White;
        else
            return null;
    }

    // ---- 玩家移动请求 ----
    public void TryMovePlayer(Vector2Int direction)
    {
        if (isProcessing) return;

        Vector2Int targetPos = PlayerGridPos + direction;

        // 边界检查
        if (targetPos.x < 0 || targetPos.x >= gridSize.x || targetPos.y < 0 || targetPos.y >= gridSize.y)
        {
            Debug.Log("边界外，无法移动");
            return;
        }

        // 地板检查
        BoxColor? floorColor = GetFloorColor(targetPos);
        if (floorColor == null)
        {
            Debug.Log("目标格无地板，无法移动");
            return;
        }

        // 箱子检查
        if (boxDict.TryGetValue(targetPos, out Box targetBox))
        {
            TryPushBox(targetBox, direction);
            return;
        }

        // 空地移动
        MovePlayerTo(targetPos);
    }

    // ---- 推动箱子 ----
    private void TryPushBox(Box box, Vector2Int direction)
    {
        Vector2Int pushTarget = box.gridPos + direction;
        Vector2Int boxOriginalPos = box.gridPos;

        // 边界检查
        if (pushTarget.x < 0 || pushTarget.x >= gridSize.x || pushTarget.y < 0 || pushTarget.y >= gridSize.y)
        {
            Debug.Log("箱子推到边界外，推不动");
            return;
        }

        // 检查目标格是否有另一个箱子
        if (boxDict.TryGetValue(pushTarget, out Box targetBox))
        {
            if (box.color == targetBox.color)
            {
                Debug.Log($"触发融合：{box.color} + {targetBox.color} → {GetOppositeColor(box.color)}");
                StartCoroutine(MergeBoxes(box, targetBox, direction));
                return;
            }
            else
            {
                Debug.Log("目标格有异色箱子，无法推动");
                return;
            }
        }

        // 检查目标格是否有地板（必须存在）
        BoxColor? floorColor = GetFloorColor(pushTarget);
        if (floorColor == null)
        {
            Debug.Log("目标格无地板，无法推动");
            return;
        }

        // 1. 箱子先移动到目标格（瞬间）
        MoveBoxTo(box, pushTarget);

        // 2. 检查是否有按钮，如果有则染色并直接停留
        bool hasButton = HasButton(pushTarget);
        if (hasButton)
        {
            // 染色
            Box currentBox = CheckAndDyeBox(box);
            // 直接停留，玩家移到箱子原位置
            MovePlayerTo(boxOriginalPos);
            Debug.Log("箱子推到按钮上，染色后停留");
            // 不需要检测地板，即使不匹配也不滑回
        }
        else
        {
            // 没有按钮，检测地板颜色匹配
            bool colorMatch = (box.color == BoxColor.Black && floorColor == BoxColor.Black) ||
                              (box.color == BoxColor.White && floorColor == BoxColor.White);
            if (colorMatch)
            {
                // 匹配，停留，玩家前进
                MovePlayerTo(boxOriginalPos);
                Debug.Log("推动成功，箱子停留在目标格");
            }
            else
            {
                // 不匹配，触发滑回
                StartCoroutine(SlideBoxBack(box, boxOriginalPos, pushTarget));
                Debug.Log("颜色不匹配，箱子滑回，玩家不动");
            }
        }
    }

    // ---- 移动箱子（瞬间） ----
    private void MoveBoxTo(Box box, Vector2Int newPos)
    {
        boxDict.Remove(box.gridPos);
        box.gridPos = newPos;
        boxDict[newPos] = box;
        Vector3 worldPos = new Vector3(newPos.x + 0.5f, newPos.y + 0.5f, -1);
        box.transform.position = worldPos;

        // 检测目标点（箱子移动后）
        CheckGoalPoint(box);
    }

    // ---- 移动玩家（同时增加步数） ----
    private void MovePlayerTo(Vector2Int newPos)
    {
        PlayerGridPos = newPos;
        Vector3 worldPos = new Vector3(newPos.x + 0.5f, newPos.y + 0.5f, -1);
        playerTransform.position = worldPos;

        // 增加步数
        stepCount++;
        OnStepChanged?.Invoke(stepCount);
    }

    // ---- 取反颜色 ----
    private BoxColor GetOppositeColor(BoxColor color) => color == BoxColor.Black ? BoxColor.White : BoxColor.Black;

    // ---- 双箱融合协程 ----
    private IEnumerator MergeBoxes(Box boxA, Box boxB, Vector2Int direction)
    {
        isProcessing = true;

        Vector2Int posA = boxA.gridPos;
        Vector2Int posB = boxB.gridPos;

        MovePlayerTo(posA);

        boxDict.Remove(posA);
        boxDict.Remove(posB);
        Destroy(boxA.gameObject);
        Destroy(boxB.gameObject);

        BoxColor newColor = GetOppositeColor(boxA.color);
        SpawnBox(posB, newColor);

        // 生长动画
        Box newBox = boxDict[posB];
        Vector3 originalScale = newBox.transform.localScale;
        newBox.transform.localScale = Vector3.zero;
        float duration = 0.2f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            newBox.transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, t);
            yield return null;
        }
        newBox.transform.localScale = originalScale;

        // 融合后检测按钮
        if (HasButton(posB))
        {
            CheckAndDyeBox(newBox);
            Debug.Log("融合在按钮上，染色停留");
        }

        // 检测目标点
        CheckGoalPoint(newBox);

        isProcessing = false;
        Debug.Log("融合完成！");
    }

    // ---- 滑回协程 ----
    private IEnumerator SlideBoxBack(Box box, Vector2Int originalPos, Vector2Int currentPos)
    {
        isProcessing = true;

        Vector3 startWorld = new Vector3(currentPos.x + 0.5f, currentPos.y + 0.5f, -1);
        Vector3 endWorld = new Vector3(originalPos.x + 0.5f, originalPos.y + 0.5f, -1);

        float duration = 0.3f;
        float elapsed = 0f;

        boxDict.Remove(currentPos);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            box.transform.position = Vector3.Lerp(startWorld, endWorld, t);
            yield return null;
        }

        box.transform.position = endWorld;
        box.gridPos = originalPos;
        boxDict[originalPos] = box;

        isProcessing = false;
        Debug.Log("滑回完成");
    }

    // ---- 目标点检测 ----
    private void CheckGoalPoint(Box box)
    {
        Debug.Log($"CheckGoalPoint 被调用，箱子位置: {box.gridPos}，目标点列表: {string.Join(", ", currentLevelData.goalPoints)}");
        if (currentLevelData == null) return;

        foreach (Vector2Int goalPos in currentLevelData.goalPoints)
        {
            if (box.gridPos == goalPos && !triggeredGoals.Contains(goalPos))
            {
                triggeredGoals.Add(goalPos);
                // 激活目标点视觉效果
                if (goalObjects.TryGetValue(goalPos, out GameObject goalObj))
                {
                    SpriteRenderer sr = goalObj.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.color = Color.green;
                }
                Debug.Log($"箱子到达目标点 {goalPos}！触发事件！");
                OnGoalReached(goalPos);
                break;
            }
        }
    }

    // ---- 目标点事件（触发胜利弹窗） ----
    private void OnGoalReached(Vector2Int goalPos)
    {
        Debug.Log($"目标点 {goalPos} 已激活！");
        // 记录通关
        LevelManager.CompleteLevel();
        // 显示胜利弹窗
        GameUI gameUI = FindObjectOfType<GameUI>();
        if (gameUI != null)
        {
            gameUI.ShowWinPanel();
        }
        else
        {
            Debug.LogWarning("未找到 GameUI，无法显示胜利弹窗");
        }
    }

    // ---- 重置关卡（同时重置步数） ----
    public void ResetLevel()
    {
        if (currentLevelData == null) return;

        foreach (var kvp in boxDict)
            Destroy(kvp.Value.gameObject);
        boxDict.Clear();

        foreach (var kvp in buttonObjects)
            Destroy(kvp.Value.gameObject);
        buttonDict.Clear();
        buttonObjects.Clear();

        // 清除目标点
        foreach (var kvp in goalObjects)
            Destroy(kvp.Value.gameObject);
        goalObjects.Clear();
        triggeredGoals.Clear();

        PlayerGridPos = currentLevelData.playerStart;
        playerTransform.position = new Vector3(PlayerGridPos.x + 0.5f, PlayerGridPos.y + 0.5f, -1);

        foreach (var info in currentLevelData.boxSpawnList)
            SpawnBox(info.gridPos, info.color);

        foreach (var info in currentLevelData.buttonList)
            SpawnButton(info.gridPos, info.color);

        foreach (Vector2Int goalPos in currentLevelData.goalPoints)
            SpawnGoalPoint(goalPos);

        // 重置步数
        stepCount = 0;
        OnStepChanged?.Invoke(0);

        Debug.Log("关卡已重置");
    }

    // ---- 退出关卡 ----
    public void QuitLevel()
    {
        Debug.Log("退出关卡");
        LevelManager.GoToMainMenu();
    }
}