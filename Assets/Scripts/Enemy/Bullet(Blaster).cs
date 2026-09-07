using UnityEngine;

public class BlasterBullet : MonoBehaviour
{
    public float speed = 10f;          // 子弹速度
    public float lifeTime = 5f;        // 子弹的生命周期，超过这个时间后自动销毁
    public int damage = 1;
    public float damageCooldown = 1f; // 伤害冷却时间
    private float lastDamageTime;

    private Rigidbody2D rb; // 子弹的刚体组件

    private void Start()
    {
        // 获取刚体组件
        rb = GetComponent<Rigidbody2D>();

        // 设置子弹的初始速度
        if (rb != null)
        {
            rb.velocity = transform.up * speed; // 根据子弹的方向设置速度
        }

        // 设置子弹的生命周期
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && Time.time > lastDamageTime + damageCooldown)
        {
            // 获取玩家的HealthSystem组件
            HealthSystem playerHealth = collision.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                lastDamageTime = Time.time; // 防止同一攻击多次伤害
            }
        }
    }
}
