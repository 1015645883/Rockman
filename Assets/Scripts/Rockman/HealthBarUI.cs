using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private HealthSystem healthSystem; // 当前绑定的玩家生命系统
    [SerializeField] private Image healthBarFill;       // 血条 UI 填充

    private void Start()
    {
        // 如果没手动赋值 healthSystem，则启动等待玩家的协程
        if (healthSystem == null)
        {
            StartCoroutine(WaitForPlayer());
        }
        else
        {
            BindHealthSystem(healthSystem);
        }
    }

    private IEnumerator WaitForPlayer()
    {
        GameObject player = null;
        while (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            yield return null;
        }

        HealthSystem hs = player.GetComponent<HealthSystem>();
        if (hs != null)
        {
            BindHealthSystem(hs);
            Debug.Log("HealthBarUI 已绑定到玩家: " + player.name);
        }
        else
        {
            Debug.LogError("在 Player 上找不到 HealthSystem 组件！");
        }
    }

    /// <summary>
    /// 🔹 绑定到新的 HealthSystem（可在特殊关卡角色切换时调用）
    /// </summary>
    public void BindToNewPlayer(GameObject newPlayer)
    {
        if (newPlayer == null) return;

        HealthSystem newHealthSystem = newPlayer.GetComponent<HealthSystem>();
        if (newHealthSystem == null)
        {
            Debug.LogError("新玩家上没有 HealthSystem 组件！");
            return;
        }

        // 解绑旧的
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged -= UpdateHealthBar;
        }

        BindHealthSystem(newHealthSystem);
        Debug.Log("HealthBarUI 已重新绑定到玩家: " + newPlayer.name);
    }

    private void BindHealthSystem(HealthSystem hs)
    {
        healthSystem = hs;
        healthSystem.OnHealthChanged += UpdateHealthBar;
        UpdateHealthBar(healthSystem.CurrentHealth, healthSystem.MaxHealth);
    }

    private void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        if (healthBarFill != null)
            healthBarFill.fillAmount = (float)currentHealth / maxHealth;
    }

    private void OnDestroy()
    {
        if (healthSystem != null)
            healthSystem.OnHealthChanged -= UpdateHealthBar;
    }
}
