using UnityEngine;

public class Thunder2 : MonoBehaviour
{
    public float speed = 5f;              // 飞行速度
    public float maxDistance = 5f;         // 最大飞行距离
    public int damage = 2;
    public float damageCooldown = 1f; // 伤害冷却时间
    private float lastDamageTime;

    private Vector2 startPosition;         // 起始位置
    private Rigidbody2D rb;                // Rigidbody2D

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // 特殊处理：如果当前角色是 Iceman，伤害+1
        string selectedChar = PlayerPrefs.GetString("SelectedCharacter", "");
        if (selectedChar == "Iceman")
        {
            damage += 1;
        }
    }

    public void Initialize(Vector2 direction)
    {
        startPosition = transform.position;               // 记录起始位置
        rb.velocity = direction.normalized * speed;        // 给个初速度
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

    private void Update()
    {
        if (Vector2.Distance(startPosition, transform.position) >= maxDistance)
        {
            Destroy(gameObject); // 飞行超过最大距离，销毁自己
        }
    }
}
