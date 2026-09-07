using UnityEngine;
using UnityEngine.UI;

public class GEnergyBarUI : MonoBehaviour
{
    [SerializeField] private Image energyFillImage;

    private void Start()
    {
        if (GEnergyManager.Instance != null)
        {
            GEnergyManager.Instance.OnEnergyChanged -= UpdateUI; // ±‹√‚÷ÿ∏¥∂©‘ƒ
            GEnergyManager.Instance.OnEnergyChanged += UpdateUI;
            UpdateUI(GEnergyManager.Instance.CurrentEnergy, GEnergyManager.Instance.MaxEnergy);
        }
        else
        {
            Debug.LogWarning("GEnergyManager.Instance is null when EnergyBarUI Start runs");
        }
    }

    private void OnEnable()
    {
        if (GEnergyManager.Instance != null)
        {
            GEnergyManager.Instance.OnEnergyChanged += UpdateUI;
            UpdateUI(GEnergyManager.Instance.CurrentEnergy, GEnergyManager.Instance.MaxEnergy);
        }
    }

    private void OnDisable()
    {
        if (GEnergyManager.Instance != null)
        {
            GEnergyManager.Instance.OnEnergyChanged -= UpdateUI;
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
