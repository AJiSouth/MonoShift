using UnityEngine;

public enum BoxColor
{
    Black,
    White
}

public class Box : MonoBehaviour
{
    [Header("Ïä×ÓÑÕÉ«")]
    public BoxColor color;

    [HideInInspector] public Vector2Int gridPos;

    public void SetColor(BoxColor newColor)
    {
        color = newColor;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = (newColor == BoxColor.Black) ? Color.black : Color.white;
        }
    }

    public void MoveTo(Vector2Int newGridPos, Vector3 worldPos)
    {
        gridPos = newGridPos;
        transform.position = worldPos;
    }
}