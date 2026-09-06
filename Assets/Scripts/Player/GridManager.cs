using UnityEngine;

public class GridManager : MonoBehaviour
{
    // 单例模式
    public static GridManager Instance { get; private set; }

    [Header("网格尺寸（暂时固定10x10）")]
    [SerializeField] private Vector2Int gridSize = new Vector2Int(10, 10);

    // 玩家当前位置（格子坐标）
    public Vector2Int PlayerGridPos { get; private set; }

    // 玩家Transform引用
    private Transform playerTransform;

    private void Awake()
    {
        // 单例初始化
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        // 生成玩家（从预制体实例化）
        GameObject playerPrefab = Resources.Load<GameObject>("Player/Player");
        if (playerPrefab == null)
        {
            Debug.LogError("未在Resources/Player/找到Player预制体！请检查路径。");
            return;
        }

        PlayerGridPos = new Vector2Int(0, 2);// 出生在(0, 2)
        Vector3 worldPos = new Vector3(PlayerGridPos.x + 0.5f, PlayerGridPos.y + 0.5f, 0);
        GameObject playerObj = Instantiate(playerPrefab, worldPos, Quaternion.identity);
        playerTransform = playerObj.transform;
    }

    /// <summary>
    /// 尝试移动玩家到目标格子
    /// </summary>
    public bool TryMovePlayer(Vector2Int direction)
    {
        Vector2Int targetPos = PlayerGridPos + direction;

        // 边界检查
        if (targetPos.x < 0 || targetPos.x >= gridSize.x || targetPos.y < 0 || targetPos.y >= gridSize.y)
            return false;

        // 执行移动（后续Day 2会在这里加入箱子碰撞检测）
        PlayerGridPos = targetPos;
        Vector3 worldPos = new Vector3(targetPos.x + 0.5f, targetPos.y + 0.5f, 0); // 格子中心偏移0.5
        playerTransform.position = worldPos;

        Debug.Log($"玩家移动到 ({targetPos.x}, {targetPos.y})");
        return true;
    }

    /// <summary>
    /// 重置玩家位置（供重新开始使用）
    /// </summary>
    public void ResetPlayer()
    {
        PlayerGridPos = Vector2Int.zero;
        playerTransform.position = new Vector3(0.5f, 0.5f, 0);
    }
}