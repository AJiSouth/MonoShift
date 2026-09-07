using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

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

    private bool isProcessing = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
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
        // 确保颜色与预制体一致
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

    // ---- 按钮染色，生成新箱子 ----
    private Box CheckAndDyeBox(Box box)
    {
        if (isProcessing) return box; // 动画期间不染色

        if (buttonDict.TryGetValue(box.gridPos, out BoxColor buttonColor))
        {
            if (box.color != buttonColor)
            {
                Vector2Int pos = box.gridPos;
                // 从字典移除旧箱子
                boxDict.Remove(pos);
                Destroy(box.gameObject);
                // 生成新颜色的箱子
                SpawnBox(pos, buttonColor);
                Debug.Log($"箱子在按钮上，销毁并重新生成，颜色变为 {buttonColor}");
                // 返回新箱子引用
                return boxDict[pos];
            }
        }
        return box; // 未染色，返回原箱子
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

        if (targetPos.x < 0 || targetPos.x >= gridSize.x || targetPos.y < 0 || targetPos.y >= gridSize.y)
        {
            Debug.Log("边界外，无法移动");
            return;
        }

        if (boxDict.TryGetValue(targetPos, out Box targetBox))
        {
            TryPushBox(targetBox, direction);
            return;
        }

        MovePlayerTo(targetPos);
    }

    // ---- 推动箱子---
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

        // 2. 检查目标格是否有按钮
        if (buttonDict.ContainsKey(pushTarget))
        {
            // 有按钮 → 强制染色，并停留在目标格
            Box currentBox = CheckAndDyeBox(box); // 可能重建
            MovePlayerTo(boxOriginalPos); // 玩家移到箱子原位置
            Debug.Log("箱子被推上按钮，染色后停留");
            return; // 结束，不执行后续滑回判断
        }

        // 3. 没有按钮 → 根据地板颜色决定是否滑回
        bool colorMatch = (box.color == BoxColor.Black && floorColor == BoxColor.Black) ||
                          (box.color == BoxColor.White && floorColor == BoxColor.White);
        if (colorMatch)
        {
            MovePlayerTo(boxOriginalPos);
            Debug.Log("推动成功，箱子停留在目标格");
        }
        else
        {
            StartCoroutine(SlideBoxBack(box, boxOriginalPos, pushTarget));
            Debug.Log("颜色不匹配，箱子滑回，玩家不动");
        }
    }

    // ---- 移动箱子 ----
    private void MoveBoxTo(Box box, Vector2Int newPos)
    {
        boxDict.Remove(box.gridPos);
        box.gridPos = newPos;
        boxDict[newPos] = box;
        Vector3 worldPos = new Vector3(newPos.x + 0.5f, newPos.y + 0.5f, -1);
        box.transform.position = worldPos;
    }

    // ---- 移动玩家 ----
    private void MovePlayerTo(Vector2Int newPos)
    {
        PlayerGridPos = newPos;
        Vector3 worldPos = new Vector3(newPos.x + 0.5f, newPos.y + 0.5f, -1);
        playerTransform.position = worldPos;
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
        CheckAndDyeBox(newBox);

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

    // ---- 重置关卡 ----
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

        PlayerGridPos = currentLevelData.playerStart;
        playerTransform.position = new Vector3(PlayerGridPos.x + 0.5f, PlayerGridPos.y + 0.5f, -1);

        foreach (var info in currentLevelData.boxSpawnList)
            SpawnBox(info.gridPos, info.color);

        foreach (var info in currentLevelData.buttonList)
            SpawnButton(info.gridPos, info.color);
    }
}