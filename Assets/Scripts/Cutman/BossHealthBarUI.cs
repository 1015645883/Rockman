using UnityEngine;
using UnityEngine.UI;

public class BossHealthBarUI : MonoBehaviour
{
    [SerializeField] private BossHealthSystem healthSystem;
    [SerializeField] private Image healthBarFill;
    [SerializeField] private GameObject bossHealthContainer;

    private void Start()
    {
        if (bossHealthContainer != null)
        {
            bossHealthContainer.SetActive(false); // ← 这里是关键隐藏代码
        }
        healthSystem.OnHealthChanged += UpdateHealthBar;
        UpdateHealthBar(healthSystem.CurrentHealth, healthSystem.MaxHealth);
    }

    private void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        healthBarFill.fillAmount = (float)currentHealth / maxHealth;

        // 颜色变化逻辑...
    }

    private void OnDestroy()
    {
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged -= UpdateHealthBar;
        }
    }
}