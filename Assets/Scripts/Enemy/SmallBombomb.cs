using UnityEngine;

public class SmallBombomb : MonoBehaviour
{
    [Header("血量")]
    public int health = 1;
    public int damage = 2;
    public float damageCooldown = 1.4f;
    private float lastDamageTime;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        string selectedCharacter = PlayerPrefs.GetString("SelectedCharacter", "Default");
        if (selectedCharacter == "Fireman" || selectedCharacter == "Bombman")
        {
            damage = 1;
        }
    }

    // 受到任何伤害调用
    public void TakeDamage(int damage)
    {
        Die();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && Time.time > lastDamageTime + damageCooldown)
        {
            HealthSystem playerHealth = collision.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                lastDamageTime = Time.time;
            }
        }
        // 这里可以根据你的武器标签统一调用 Die()
        if (collision.CompareTag("Bullet") ||
            collision.CompareTag("ChargeBullet") ||
            collision.CompareTag("RollingCutter") ||
            collision.CompareTag("SuperArm") ||
            collision.CompareTag("HyperBomb") ||
            collision.CompareTag("FireStorm") ||
            collision.CompareTag("ThunderBeam") ||
            collision.CompareTag("IceArrow") ||
            collision.CompareTag("Ground"))
        {
            Die();
        }
    }

    private void Die()
    {
        // 禁用碰撞
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // 禁用刚体模拟
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false;

        // 播放死亡动画
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        // 延迟销毁
        Destroy(gameObject, 0.3f);
    }
}
