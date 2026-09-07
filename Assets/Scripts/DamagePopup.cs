using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    [Header("移动参数")]
    public float jumpHeight = 1.5f;       // 跳出高度
    public float jumpDuration = 0.5f;     // 跳跃总时长
    public float moveSideRange = 1.2f;    // 左右位移范围（随机）

    [Header("显示时长")]
    public float stayDuration = 0.4f;     // 停留时间（跳完后）
    public float fadeDuration = 0.6f;     // 渐隐时间

    private TextMeshPro textMesh;
    private Color textColor;
    private float timer = 0f;
    private Vector3 startPos;
    private float sideOffset;             // 水平方向随机偏移

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        textColor = textMesh.color;
    }

    public void Setup(int damageAmount)
    {
        textMesh.text = damageAmount.ToString();
        startPos = transform.position;

        // 出现时给一个左右随机的目标偏移
        sideOffset = Random.Range(-moveSideRange, moveSideRange);
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer < jumpDuration)
        {
            // 使用抛物线曲线：t=0 上升，t=0.5 最高点，t=1 落回
            float t = timer / jumpDuration;
            float height = 4 * jumpHeight * t * (1 - t); // 抛物线曲线 y = 4h*t*(1-t)

            float x = Mathf.Lerp(0, sideOffset, t); // 逐渐位移到目标点
            transform.position = startPos + new Vector3(x, height, 0f);
        }
        else if (timer < jumpDuration + stayDuration)
        {
            // 跳完后停留在最后的位置
        }
        else
        {
            // 渐隐处理
            float fadeProgress = (timer - jumpDuration - stayDuration) / fadeDuration;
            textColor.a = Mathf.Lerp(1f, 0f, fadeProgress);
            textMesh.color = textColor;

            if (fadeProgress >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
