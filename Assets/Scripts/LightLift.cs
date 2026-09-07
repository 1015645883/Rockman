using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LightLift : MonoBehaviour
{
    [Header("激活条件")]
    public bool activeWhenLightOn = true; // true=灯亮激活，false=灯灭激活

    [Header("激活动画（3帧）")]
    public Sprite[] activateSprites; // 3张sprite

    [Header("动画播放间隔")]
    public float animationInterval = 0.1f;

    private Collider2D col;
    private SpriteRenderer sr;

    private bool isActive = false;
    private Coroutine animCoroutine;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        if (LightManager.Instance == null)
        {
            Debug.LogError("LightManager 不存在！");
            return;
        }

        // 监听全局灯状态
        LightManager.Instance.OnLightStateChanged += OnLightChanged;

        // ✅ 开局初始化（不播放动画）
        UpdateState(LightManager.Instance.GetLightState(), true);
    }

    private void OnDestroy()
    {
        if (LightManager.Instance != null)
        {
            LightManager.Instance.OnLightStateChanged -= OnLightChanged;
        }
    }

    private void OnLightChanged(bool lightState)
    {
        // ✅ 状态变化（播放动画）
        UpdateState(lightState, false);
    }

    /// <summary>
    /// 根据灯状态更新平台状态
    /// </summary>
    private void UpdateState(bool lightState, bool instant)
    {
        bool shouldBeActive = activeWhenLightOn ? lightState : !lightState;

        if (shouldBeActive == isActive && !instant)
            return;

        isActive = shouldBeActive;

        if (isActive)
        {
            Activate(instant);
        }
        else
        {
            Deactivate(instant);
        }
    }

    /// <summary>
    /// 激活平台
    /// </summary>
    private void Activate(bool instant)
    {
        col.enabled = true;

        if (animCoroutine != null)
            StopCoroutine(animCoroutine);

        if (instant)
        {
            // ✅ 开局：直接显示最终帧
            if (activateSprites != null && activateSprites.Length > 0)
            {
                sr.sprite = activateSprites[activateSprites.Length - 1];
            }

            SetAlpha(1f);
        }
        else
        {
            // ✅ 正常：播放动画
            animCoroutine = StartCoroutine(PlayActivateAnimation());
        }
    }

    /// <summary>
    /// 关闭平台
    /// </summary>
    private void Deactivate(bool instant)
    {
        col.enabled = false;

        if (animCoroutine != null)
            StopCoroutine(animCoroutine);

        // 半透明
        SetAlpha(0.5f);
    }

    /// <summary>
    /// 播放三帧激活动画
    /// </summary>
    private IEnumerator PlayActivateAnimation()
    {
        if (activateSprites == null || activateSprites.Length == 0)
            yield break;

        for (int i = 0; i < activateSprites.Length; i++)
        {
            sr.sprite = activateSprites[i];
            SetAlpha(1f);
            yield return new WaitForSeconds(animationInterval);
        }

        // 保持最后一帧
        sr.sprite = activateSprites[activateSprites.Length - 1];
        SetAlpha(1f);
    }

    /// <summary>
    /// 设置透明度
    /// </summary>
    private void SetAlpha(float alpha)
    {
        if (sr == null) return;

        Color c = sr.color;
        c.a = alpha;
        sr.color = c;
    }
}