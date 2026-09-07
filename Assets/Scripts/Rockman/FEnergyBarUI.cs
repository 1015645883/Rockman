using UnityEngine;
using UnityEngine.UI;

public class FEnergyBarUI : MonoBehaviour
{
    [SerializeField] private Image energyFillImage;

    private void Start()
    {
        if (FEnergyManager.Instance != null)
        {
            FEnergyManager.Instance.OnEnergyChanged -= UpdateUI; // ±‹√‚÷ÿ∏¥∂©‘ƒ
            FEnergyManager.Instance.OnEnergyChanged += UpdateUI;
            UpdateUI(FEnergyManager.Instance.CurrentEnergy, FEnergyManager.Instance.MaxEnergy);
        }
        else
        {
            Debug.LogWarning("FEnergyManager.Instance is null when EnergyBarUI Start runs");
        }
    }

    private void OnEnable()
    {
        if (FEnergyManager.Instance != null)
        {
            FEnergyManager.Instance.OnEnergyChanged += UpdateUI;
            UpdateUI(FEnergyManager.Instance.CurrentEnergy, FEnergyManager.Instance.MaxEnergy);
        }
    }

    private void OnDisable()
    {
        if (FEnergyManager.Instance != null)
        {
            FEnergyManager.Instance.OnEnergyChanged -= UpdateUI;
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
