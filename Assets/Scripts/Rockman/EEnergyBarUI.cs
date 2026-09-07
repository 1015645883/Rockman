using UnityEngine;
using UnityEngine.UI;

public class EEnergyBarUI : MonoBehaviour
{
    [SerializeField] private Image energyFillImage;
    private EEnergyManager boundManager;

    // 外部调用：绑定到当前角色的 EEnergyManager
    public void Bind(EEnergyManager manager)
    {
        Debug.Log($"EEnergyBarUI绑定到管理器: {manager?.gameObject.name ?? "null"}");

        if (boundManager != null)
        {
            boundManager.OnEnergyChanged -= UpdateUI;
        }

        boundManager = manager;

        if (boundManager != null)
        {
            boundManager.OnEnergyChanged += UpdateUI;
            UpdateUI(boundManager.CurrentEnergy, boundManager.MaxEnergy);
        }
        else
        {
            UpdateUI(0, 1);
        }
    }

    public void Refresh()
    {
        if (boundManager != null)
        {
            UpdateUI(boundManager.CurrentEnergy, boundManager.MaxEnergy);
        }
    }

    private void OnDisable()
    {
        if (boundManager != null)
        {
            boundManager.OnEnergyChanged -= UpdateUI;
        }
    }

    // 返回当前填充比例
    public float GetFillAmount()
    {
        if (boundManager == null || boundManager.MaxEnergy == 0)
            return 0f;
        return (float)boundManager.CurrentEnergy / boundManager.MaxEnergy;
    }

    // 外部回调列表
    private event System.Action OnUIUpdated;

    // PauseEEnergyBarUI调用绑定
    public void BindCallback(System.Action callback)
    {
        OnUIUpdated += callback;
    }

    public void UnbindCallback(System.Action callback)
    {
        OnUIUpdated -= callback;
    }

    private void UpdateUI(int current, int max)
    {
        if (energyFillImage != null)
        {
            energyFillImage.fillAmount = (float)current / max;
        }
        OnUIUpdated?.Invoke();
    }
}
