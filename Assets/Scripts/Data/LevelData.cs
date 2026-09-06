using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "推箱子/关卡数据")]
public class LevelData : ScriptableObject
{
    [Header("网格尺寸")]
    public Vector2Int gridSize = new Vector2Int(8, 5);

    [Header("玩家出生位置（格子坐标）")]
    public Vector2Int playerStart = new Vector2Int(0, 2);

    [Header("箱子初始列表")]
    public List<BoxSpawnInfo> boxSpawnList = new List<BoxSpawnInfo>();
}

[System.Serializable]
public class BoxSpawnInfo
{
    public Vector2Int gridPos;
    public BoxColor color;
}