using UnityEngine;
using UnityEngine.UI;

public class IEnergyBarUI : MonoBehaviour
{
    [SerializeField] private Image energyFillImage;

    private void Start()
    {
        if (IEnergyManager.Instance != null)
        {
            IEnergyManager.Instance.OnEnergyChanged -= UpdateUI; // ±‹√‚÷ÿ∏¥∂©‘ƒ
            IEnergyManager.Instance.OnEnergyChanged += UpdateUI;
            UpdateUI(IEnergyManager.Instance.CurrentEnergy, IEnergyManager.Instance.MaxEnergy);
        }
        else
        {
            Debug.LogWarning("IEnergyManager.Instance is null when EnergyBarUI Start runs");
        }
    }

    private void OnEnable()
    {
        if (IEnergyManager.Instance != null)
        {
            IEnergyManager.Instance.OnEnergyChanged += UpdateUI;
            UpdateUI(IEnergyManager.Instance.CurrentEnergy, IEnergyManager.Instance.MaxEnergy);
        }
    }

    private void OnDisable()
    {
        if (IEnergyManager.Instance != null)
        {
            IEnergyManager.Instance.OnEnergyChanged -= UpdateUI;
        }
    }

    private void UpdateUI(int current, int max)
    {
        if (energyFillImage != null)
        {
            energyFillImage.fillAmount = (float)current / max;
        }
        else
        {
            Debug.LogWarning("energyFillImage is not assigned in EnergyBarUI.");
        }
    }
}
