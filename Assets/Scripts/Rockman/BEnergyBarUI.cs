using UnityEngine;
using UnityEngine.UI;

public class BEnergyBarUI : MonoBehaviour
{
    [SerializeField] private Image energyFillImage;

    private void Start()
    {
        if (BEnergyManager.Instance != null)
        {
            BEnergyManager.Instance.OnEnergyChanged -= UpdateUI; // ±‹√‚÷ÿ∏¥∂©‘ƒ
            BEnergyManager.Instance.OnEnergyChanged += UpdateUI;
            UpdateUI(BEnergyManager.Instance.CurrentEnergy, BEnergyManager.Instance.MaxEnergy);
        }
        else
        {
            Debug.LogWarning("BEnergyManager.Instance is null when EnergyBarUI Start runs");
        }
    }

    private void OnEnable()
    {
        if (BEnergyManager.Instance != null)
        {
            BEnergyManager.Instance.OnEnergyChanged += UpdateUI;
            UpdateUI(BEnergyManager.Instance.CurrentEnergy, BEnergyManager.Instance.MaxEnergy);
        }
    }

    private void OnDisable()
    {
        if (BEnergyManager.Instance != null)
        {
            BEnergyManager.Instance.OnEnergyChanged -= UpdateUI;
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
