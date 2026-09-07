using UnityEngine;
using UnityEngine.UI;

public class CEnergyBarUI : MonoBehaviour
{
    [SerializeField] private Image energyFillImage;

    private void Start()
    {
        if (CEnergyManager.Instance != null)
        {
            CEnergyManager.Instance.OnEnergyChanged -= UpdateUI; // ±‹√‚÷ÿ∏¥∂©‘ƒ
            CEnergyManager.Instance.OnEnergyChanged += UpdateUI;
            UpdateUI(CEnergyManager.Instance.CurrentEnergy, CEnergyManager.Instance.MaxEnergy);
        }
        else
        {
            Debug.LogWarning("CEnergyManager.Instance is null when EnergyBarUI Start runs");
        }
    }

    private void OnEnable()
    {
        if (CEnergyManager.Instance != null)
        {
            CEnergyManager.Instance.OnEnergyChanged += UpdateUI;
            UpdateUI(CEnergyManager.Instance.CurrentEnergy, CEnergyManager.Instance.MaxEnergy);
        }
    }

    private void OnDisable()
    {
        if (CEnergyManager.Instance != null)
        {
            CEnergyManager.Instance.OnEnergyChanged -= UpdateUI;
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
