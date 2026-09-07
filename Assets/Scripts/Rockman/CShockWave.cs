using UnityEngine;

public class CShockWave : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private PlayerMovement playerMovement;
    private Transform playerTransform;
    public float damageCooldown = 0.7f; // 伤害冷却时间
    public float lastDamageTime;

    void Start()
    {
        // 获取 Player 对象及其 PlayerMovement 组件
        playerTransform = GameObject.FindWithTag("Player")?.transform;
        playerMovement = playerTransform?.GetComponent<PlayerMovement>();

        // 将冲击波位置设置为玩家的 shockwaveSpawnPoint 位置
        if (playerMovement != null && playerMovement.shockwaveSpawnPoint != null)
        {
            // 初始对齐
            transform.position = playerMovement.shockwaveSpawnPoint.position;
        }
    }

    void Update()
    {
        // 实时跟随冲击波生成点
        if (playerMovement != null && playerMovement.shockwaveSpawnPoint != null)
        {
            transform.position = playerMovement.shockwaveSpawnPoint.position;
        }

        // 冲刺结束后销毁冲击波
        if (playerMovement != null && !playerMovement.isDashing)
        {
            Destroy(gameObject);
        }

    }
}
