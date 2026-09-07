using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 挂载在Image上，跟随绑定的EEnergyBarUI填充量变化
/// </summary>
public class PauseEEnergyBarUI : MonoBehaviour
{
    [Header("引用的EEnergyBarUI")]
    [SerializeField] private EEnergyBarUI sourceEnergyBar;

    private Image image;

    private void Awake()
    {
        image = GetComponent<Image>();
        if (image == null)
        {
            Debug.LogWarning($"{nameof(PauseEEnergyBarUI)}必须挂载在一个带Image组件的对象上");
        }
    }

    private void OnEnable()
    {
        if (sourceEnergyBar != null)
        {
            // 初始化一次显示
            UpdateFill();
            // 订阅源的刷新事件
            sourceEnergyBar.BindCallback(UpdateFill);
        }
    }

    private void OnDisable()
    {
        if (sourceEnergyBar != null)
        {
            sourceEnergyBar.UnbindCallback(UpdateFill);
        }
    }

    private void UpdateFill()
    {
        if (image != null && sourceEnergyBar != null)
        {
            // 使用EEnergyBarUI的当前fillAmount
            image.fillAmount = sourceEnergyBar.GetFillAmount();
        }
    }
}
