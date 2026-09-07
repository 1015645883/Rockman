using UnityEngine;

public class Thunder1 : MonoBehaviour
{
    public float speed = 5f;
    public float maxDistance = 8f; // 最大飞行距离
    private Vector2 startPosition; // 记录起始位置
    public int damage = 4;
    public float damageCooldown = 1.5f; // 伤害冷却时间
    private float lastDamageTime;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
        // 特殊处理：如果当前角色是 Iceman，伤害+1
        string selectedChar = PlayerPrefs.GetString("SelectedCharacter", "");
        if (selectedChar == "Iceman")
        {
            damage += 1;
        }
    }

    public void Initialize(Vector2 direction)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.velocity = direction.normalized * speed;
        startPosition = transform.position; // 初始化时记录起点
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
            Destroy(gameObject);
        }
    }
}
