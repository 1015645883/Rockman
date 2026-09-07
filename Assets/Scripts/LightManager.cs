using System;
using UnityEngine;

public class LightManager : MonoBehaviour
{
    public static LightManager Instance;

    // ✅ 全局唯一状态
    private bool globalLightState = false;

    // 状态变化事件（不再需要ID）
    public Action<bool> OnLightStateChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 初始化状态（只会用一次）
    /// </summary>
    public void Initialize(bool initialState)
    {
        globalLightState = initialState;
    }

    /// <summary>
    /// 设置状态
    /// </summary>
    public void SetLightState(bool state)
    {
        if (globalLightState == state)
            return;

        globalLightState = state;

        // 通知所有灯泡 / 平台
        OnLightStateChanged?.Invoke(globalLightState);
    }

    /// <summary>
    /// 切换状态
    /// </summary>
    public void ToggleLight()
    {
        SetLightState(!globalLightState);
    }

    /// <summary>
    /// 获取当前状态
    /// </summary>
    public bool GetLightState()
    {
        return globalLightState;
    }
}