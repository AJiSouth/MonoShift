using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "推箱子/关卡数据")]
public class LevelData : ScriptableObject
{
    [Header("网格尺寸")]
    public Vector2Int gridSize = new Vector2Int(12, 6);

    [Header("玩家出生位置")]
    public Vector2Int playerStart = new Vector2Int(0, 2);

    [Header("箱子初始列表")]
    public List<BoxSpawnInfo> boxSpawnList = new List<BoxSpawnInfo>();

    [Header("按钮列表")]
    public List<ButtonSpawnInfo> buttonList = new List<ButtonSpawnInfo>();

    [Header("目标点列表（箱子到达即触发事件）")]
    public List<Vector2Int> goalPoints = new List<Vector2Int>();
}

[System.Serializable]
public class BoxSpawnInfo
{
    public Vector2Int gridPos;
    public BoxColor color;
}

[System.Serializable]
public class ButtonSpawnInfo
{
    public Vector2Int gridPos;
    public BoxColor color;
}