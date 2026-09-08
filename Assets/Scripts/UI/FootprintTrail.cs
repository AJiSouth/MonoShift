using UnityEngine;
using System.Collections;

public class FootprintTrail : MonoBehaviour
{
    [Header("脚印设置")]
    public GameObject leftFootPrefab;   // 左脚预制体
    public GameObject rightFootPrefab;  // 右脚预制体
    public float spawnDistance = 0.4f;
    public float lifeTime = 3f;
    public float minScale = 0.1f;
    public float maxScale = 0.5f;
    public float sideOffset = 0.15f;    // 左右偏移量（垂直于移动方向）

    private Vector2 lastPosition;
    private bool hasLastPos = false;
    private bool isLeft = true;

    void Start()
    {
        lastPosition = GetMouseWorldPos();
        hasLastPos = true;
    }

    void Update()
    {
        Vector2 currentPos = GetMouseWorldPos();
        if (!hasLastPos)
        {
            lastPosition = currentPos;
            hasLastPos = true;
            return;
        }

        float dist = Vector2.Distance(currentPos, lastPosition);
        if (dist >= spawnDistance)
        {
            // 计算移动方向
            Vector2 dir = (currentPos - lastPosition).normalized;

            // 计算垂直方向（垂直于移动方向）
            Vector2 perpendicular = new Vector2(-dir.y, dir.x);

            // 左右偏移：左脚向左偏移，右脚向右偏移
            float offsetSign = isLeft ? -1f : 1f;
            Vector2 offset = perpendicular * offsetSign * sideOffset;

            // 最终位置 = 鼠标位置 + 垂直偏移
            Vector2 footPosition = currentPos + offset;

            // 计算旋转角度（让脚印朝向移动方向）
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            // 选择左脚或右脚预制体
            GameObject prefab = isLeft ? leftFootPrefab : rightFootPrefab;
            GameObject footprint = Instantiate(prefab, footPosition, Quaternion.Euler(0, 0, angle));
            footprint.transform.localScale = Vector3.one * maxScale;

            StartCoroutine(FadeFootprint(footprint));

            // 切换左右脚
            isLeft = !isLeft;
            lastPosition = currentPos;
        }
    }

    private Vector2 GetMouseWorldPos()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Camera.main.nearClipPlane + 10f;
        return Camera.main.ScreenToWorldPoint(mousePos);
    }

    IEnumerator FadeFootprint(GameObject obj)
    {
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr == null) yield break;

        float timer = lifeTime;
        Vector3 startScale = obj.transform.localScale;

        while (timer > 0)
        {
            timer -= Time.deltaTime;
            float progress = timer / lifeTime;

            Color c = sr.color;
            c.a = progress;
            sr.color = c;

            float scale = Mathf.Lerp(minScale, maxScale, progress);
            obj.transform.localScale = startScale * scale;

            yield return null;
        }

        Destroy(obj);
    }
}