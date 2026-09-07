using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class LightBulb : MonoBehaviour
{
    [Header("初始状态（true = 开）")]
    public bool initialState = false;

    [Header("灯泡Sprite")]
    public Sprite onSprite;   // 发光
    public Sprite offSprite;  // 未激活

    [Header("防止连续触发（冷却时间）")]
    public float triggerCooldown = 0.2f;

    private float lastTriggerTime = -999f;
    private SpriteRenderer sr;

    private void Awake()
    {
        // ✅ 更安全（支持Sprite在子物体）
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        if (LightManager.Instance == null)
        {
            Debug.LogError("LightManager 不存在！");
            return;
        }

        // ✅ 初始化全局状态（只会第一次生效）
        LightManager.Instance.Initialize(initialState);

        // ✅ 订阅事件
        LightManager.Instance.OnLightStateChanged += OnLightChanged;

        // ✅ 初始化显示
        UpdateVisual(LightManager.Instance.GetLightState());
    }

    private void OnDestroy()
    {
        if (LightManager.Instance != null)
        {
            LightManager.Instance.OnLightStateChanged -= OnLightChanged;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("ThunderBeam"))
        {
            if (Time.time - lastTriggerTime < triggerCooldown)
                return;

            lastTriggerTime = Time.time;

            // ✅ 切换“全局状态”（关键变化）
            LightManager.Instance.ToggleLight();

            // ✅ 获取 ThunderBeam
            ThunderBeam beam = other.GetComponentInParent<ThunderBeam>();

            if (beam != null)
            {
                // 👉 走回调销毁流程（非常关键）
                beam.DestroyBullet();
            }
            else
            {
                // 兜底
                Destroy(other.transform.root.gameObject);
            }
        }
    }

    /// <summary>
    /// 全局灯状态变化
    /// </summary>
    private void OnLightChanged(bool state)
    {
        Debug.Log($"全局灯状态改变: {state}");
        UpdateVisual(state);
    }

    /// <summary>
    /// 更新灯泡外观
    /// </summary>
    private void UpdateVisual(bool state)
    {
        if (sr == null) return;

        sr.sprite = state ? onSprite : offSprite;
    }
}