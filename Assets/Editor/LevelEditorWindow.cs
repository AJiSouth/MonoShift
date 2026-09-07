using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class LevelEditorWindow : EditorWindow
{
    private LevelData currentLevelData;
    private int selectedTool = -1;
    private readonly string[] toolNames = { "黑箱", "白箱", "黑按钮", "白按钮", "目标点", "擦除", "玩家起点" };
    private Vector2 scrollPos;

    private Dictionary<Vector2Int, BoxColor> boxMap = new Dictionary<Vector2Int, BoxColor>();
    private Dictionary<Vector2Int, BoxColor> buttonMap = new Dictionary<Vector2Int, BoxColor>();
    private HashSet<Vector2Int> goalSet = new HashSet<Vector2Int>();

    private Texture2D blackTex;
    private Texture2D whiteTex;
    private Texture2D grayTex;
    private Texture2D goalTex;
    private Texture2D btnBlackTex;
    private Texture2D btnWhiteTex;
    private Texture2D playerTex;

    [MenuItem("Tools/关卡编辑器")]
    public static void ShowWindow()
    {
        LevelEditorWindow window = GetWindow<LevelEditorWindow>("关卡编辑器");
        window.minSize = new Vector2(600, 500);
        window.Show();
    }

    private void OnEnable()
    {
        blackTex = MakeTex(new Color(0.2f, 0.2f, 0.2f));
        whiteTex = MakeTex(new Color(0.9f, 0.9f, 0.9f));
        grayTex = MakeTex(new Color(0.5f, 0.5f, 0.5f));
        goalTex = MakeTex(new Color(1f, 0.8f, 0.2f));
        btnBlackTex = MakeTex(new Color(0.1f, 0.1f, 0.1f));
        btnWhiteTex = MakeTex(new Color(0.95f, 0.95f, 0.95f));
        playerTex = MakeTex(new Color(0.2f, 0.6f, 1f));
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("关卡编辑器（仅开发者）", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        currentLevelData = (LevelData)EditorGUILayout.ObjectField("关卡数据", currentLevelData, typeof(LevelData), false);
        if (GUILayout.Button("新建关卡数据", GUILayout.Width(120)))
        {
            CreateNewLevelData();
        }
        EditorGUILayout.EndHorizontal();

        if (currentLevelData == null)
        {
            EditorGUILayout.HelpBox("请选择一个现有的关卡数据，或点击“新建关卡数据”创建。", MessageType.Info);
            return;
        }

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"网格尺寸: {currentLevelData.gridSize.x} x {currentLevelData.gridSize.y}");
        EditorGUILayout.LabelField($"玩家起始: {currentLevelData.playerStart}");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("选择工具（点击切换，绿色=已选中）", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < toolNames.Length; i++)
        {
            GUI.backgroundColor = (selectedTool == i) ? Color.green : Color.white;
            if (GUILayout.Button(toolNames[i], GUILayout.Width(80), GUILayout.Height(25)))
            {
                if (selectedTool == i) selectedTool = -1;
                else selectedTool = i;
            }
            GUI.backgroundColor = Color.white;
        }
        EditorGUILayout.EndHorizontal();

        if (selectedTool == -1)
            EditorGUILayout.HelpBox("请选择一个工具，点击下方网格进行编辑。", MessageType.Info);
        else
            EditorGUILayout.LabelField($"当前工具: {toolNames[selectedTool]}", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        DrawGrid();

        EditorGUILayout.Space();

        if (GUILayout.Button("清空", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("确认清空", "确定要清空当前关卡的所有数据吗？", "确定", "取消"))
                ClearAllData();
        }

        UpdateCache();
    }

    private void CreateNewLevelData()
    {
        string path = EditorUtility.SaveFilePanelInProject("创建关卡数据", "Level_New", "asset", "请选择保存位置");
        if (string.IsNullOrEmpty(path)) return;

        LevelData newData = ScriptableObject.CreateInstance<LevelData>();
        newData.gridSize = new Vector2Int(12, 6);
        newData.playerStart = new Vector2Int(0, 2);
        AssetDatabase.CreateAsset(newData, path);
        AssetDatabase.SaveAssets();
        currentLevelData = newData;
        Debug.Log($"已创建关卡数据: {path}");
    }

    private void DrawGrid()
    {
        int width = currentLevelData.gridSize.x;
        int height = currentLevelData.gridSize.y;

        UpdateCache();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        float cellSize = Mathf.Min(40, (EditorGUILayout.GetControlRect().width - 20) / width);
        cellSize = Mathf.Max(cellSize, 25);

        for (int y = height - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < width; x++)
            {
                Vector2Int pos = new Vector2Int(x, y);

                Texture2D bgTex = grayTex;
                string label = "";
                Color textColor = Color.black; // 默认

                bool hasBox = boxMap.ContainsKey(pos);
                bool hasButton = buttonMap.ContainsKey(pos);
                bool hasGoal = goalSet.Contains(pos);
                bool isPlayerStart = (pos == currentLevelData.playerStart);

                if (hasBox)
                {
                    bgTex = (boxMap[pos] == BoxColor.Black) ? blackTex : whiteTex;
                    label = (boxMap[pos] == BoxColor.Black) ? "黑箱" : "白箱";
                    textColor = (boxMap[pos] == BoxColor.Black) ? Color.black : Color.white;
                }
                else if (hasButton)
                {
                    bgTex = (buttonMap[pos] == BoxColor.Black) ? btnBlackTex : btnWhiteTex;
                    label = (buttonMap[pos] == BoxColor.Black) ? "黑钮" : "白钮";
                    textColor = (buttonMap[pos] == BoxColor.Black) ? Color.black : Color.white;
                }
                else if (hasGoal)
                {
                    bgTex = goalTex;
                    label = "目标点";
                    textColor = Color.cyan; // 改为亮青色
                }
                else if (isPlayerStart)
                {
                    bgTex = playerTex;
                    label = "玩家";
                    textColor = Color.cyan; // 改为亮青色
                }

                GUIStyle style = new GUIStyle(GUI.skin.button);
                style.normal.background = bgTex;
                style.fontSize = (int)Mathf.Min(cellSize / 3, 12);
                style.alignment = TextAnchor.MiddleCenter;
                style.normal.textColor = textColor;

                if (GUILayout.Button(label, style, GUILayout.Width(cellSize), GUILayout.Height(cellSize)))
                {
                    HandleGridClick(x, y);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private void HandleGridClick(int x, int y)
    {
        if (selectedTool == -1) return;

        Vector2Int pos = new Vector2Int(x, y);
        Undo.RecordObject(currentLevelData, "编辑关卡");

        if (selectedTool == 5) // 擦除
        {
            currentLevelData.boxSpawnList.RemoveAll(item => item.gridPos == pos);
            currentLevelData.buttonList.RemoveAll(item => item.gridPos == pos);
            currentLevelData.goalPoints.RemoveAll(item => item == pos);
            if (currentLevelData.playerStart == pos)
                currentLevelData.playerStart = Vector2Int.zero;
            Debug.Log($"已擦除 ({x}, {y})");
            EditorUtility.SetDirty(currentLevelData);
            AssetDatabase.SaveAssets();
            UpdateCache();
            return;
        }

        // 移除同位置的其他元素
        currentLevelData.boxSpawnList.RemoveAll(item => item.gridPos == pos);
        currentLevelData.buttonList.RemoveAll(item => item.gridPos == pos);
        currentLevelData.goalPoints.RemoveAll(item => item == pos);

        switch (selectedTool)
        {
            case 0: // 黑箱
                currentLevelData.boxSpawnList.Add(new BoxSpawnInfo { gridPos = pos, color = BoxColor.Black });
                Debug.Log($"添加黑箱 at ({x}, {y})");
                break;
            case 1: // 白箱
                currentLevelData.boxSpawnList.Add(new BoxSpawnInfo { gridPos = pos, color = BoxColor.White });
                Debug.Log($"添加白箱 at ({x}, {y})");
                break;
            case 2: // 黑按钮
                currentLevelData.buttonList.Add(new ButtonSpawnInfo { gridPos = pos, color = BoxColor.Black });
                Debug.Log($"添加黑按钮 at ({x}, {y})");
                break;
            case 3: // 白按钮
                currentLevelData.buttonList.Add(new ButtonSpawnInfo { gridPos = pos, color = BoxColor.White });
                Debug.Log($"添加白按钮 at ({x}, {y})");
                break;
            case 4: // 目标点
                currentLevelData.goalPoints.Add(pos);
                Debug.Log($"添加目标点 at ({x}, {y})");
                break;
            case 6: // 玩家起点
                currentLevelData.playerStart = pos;
                Debug.Log($"设置玩家起点 at ({x}, {y})");
                break;
            default:
                break;
        }

        EditorUtility.SetDirty(currentLevelData);
        AssetDatabase.SaveAssets();
        UpdateCache();
    }

    private void UpdateCache()
    {
        if (currentLevelData == null) return;

        boxMap.Clear();
        foreach (var info in currentLevelData.boxSpawnList)
            boxMap[info.gridPos] = info.color;

        buttonMap.Clear();
        foreach (var info in currentLevelData.buttonList)
            buttonMap[info.gridPos] = info.color;

        goalSet.Clear();
        foreach (var pos in currentLevelData.goalPoints)
            goalSet.Add(pos);
    }

    private void ClearAllData()
    {
        Undo.RecordObject(currentLevelData, "清空关卡数据");
        currentLevelData.boxSpawnList.Clear();
        currentLevelData.buttonList.Clear();
        currentLevelData.goalPoints.Clear();
        currentLevelData.playerStart = Vector2Int.zero;
        EditorUtility.SetDirty(currentLevelData);
        AssetDatabase.SaveAssets();
        UpdateCache();
        Debug.Log("已清空所有数据");
    }

    private Texture2D MakeTex(Color color)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }
}