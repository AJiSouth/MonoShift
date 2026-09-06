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

    // 网格尺寸（从LevelData读取）
    private Vector2Int gridSize;

    // 玩家
    private Transform playerTransform;
    public Vector2Int PlayerGridPos { get; private set; }

    // 箱子系统
    private Dictionary<Vector2Int, Box> boxDict = new Dictionary<Vector2Int, Box>();

    // 操作锁
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
            Debug.LogError("未设置关卡数据！请拖拽 LevelData 资产到 GridManager 的 currentLevelData 槽");
            return;
        }

        // 从关卡数据读取网格尺寸
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

        // 根据关卡数据生成所有箱子
        foreach (var info in currentLevelData.boxSpawnList)
        {
            SpawnBox(info.gridPos, info.color);
        }
    }

    // ---- 生成箱子（根据颜色加载不同预制体） ----
    public void SpawnBox(Vector2Int gridPos, BoxColor color)
    {
        if (boxDict.ContainsKey(gridPos))
        {
            Destroy(boxDict[gridPos].gameObject);
            boxDict.Remove(gridPos);
        }

        // 根据颜色选择不同预制体（实现不同贴图）
        string prefabPath = (color == BoxColor.Black) ? "Box/Box_Black" : "Box/Box_White";
        GameObject boxPrefab = Resources.Load<GameObject>(prefabPath);
        if (boxPrefab == null)
        {
            Debug.LogError($"未找到箱子预制体：{prefabPath}，请确保在 Resources/Box/ 下有 Box_Black 和 Box_White");
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
        //box.SetColor(color);
        boxDict[gridPos] = box;
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

    // ---- 推动箱子 ----
    private void TryPushBox(Box box, Vector2Int direction)
    {
        Vector2Int pushTarget = box.gridPos + direction;
        Vector2Int boxOriginalPos = box.gridPos;  // 记录原位置

        // 边界检查
        if (pushTarget.x < 0 || pushTarget.x >= gridSize.x || pushTarget.y < 0 || pushTarget.y >= gridSize.y)
        {
            Debug.Log("箱子推到边界外，推不动");
            return;
        }

        // 检查目标格是否有另一个箱子
        if (boxDict.ContainsKey(pushTarget))
        {
            Debug.Log("目标格有另一个箱子，无法推动");
            return;
        }

        // 检查地板颜色
        BoxColor? floorColor = GetFloorColor(pushTarget);
        if (floorColor == null)
        {
            Debug.Log("目标格无地板，无法推动");
            return;
        }

        bool colorMatch = (box.color == BoxColor.Black && floorColor == BoxColor.Black) ||
                          (box.color == BoxColor.White && floorColor == BoxColor.White);

        if (colorMatch)
        {
            // 正常推动
            MoveBoxTo(box, pushTarget);
            MovePlayerTo(boxOriginalPos);  // 玩家移到箱子原位置
        }
        else
        {
            // 颜色不匹配
            MoveBoxTo(box, pushTarget);
            StartCoroutine(SlideBoxBack(box, boxOriginalPos, pushTarget));
            Debug.Log("颜色不匹配，无法推动");
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
    }

    // ---- 移动玩家 ----
    private void MovePlayerTo(Vector2Int newPos)
    {
        PlayerGridPos = newPos;
        Vector3 worldPos = new Vector3(newPos.x + 0.5f, newPos.y + 0.5f, -1);
        playerTransform.position = worldPos;
    }

    // ---- 白箱滑回协程 ----
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
        Debug.Log("白箱滑回完成");
    }

    // ---- 重置关卡 ----
    public void ResetLevel()
    {
        if (currentLevelData == null) return;

        // 清空所有箱子
        foreach (var kvp in boxDict)
        {
            Destroy(kvp.Value.gameObject);
        }
        boxDict.Clear();

        // 重置玩家位置
        PlayerGridPos = currentLevelData.playerStart;
        playerTransform.position = new Vector3(PlayerGridPos.x + 0.5f, PlayerGridPos.y + 0.5f, -1);

        // 重新生成箱子
        foreach (var info in currentLevelData.boxSpawnList)
        {
            SpawnBox(info.gridPos, info.color);
        }
    }
}