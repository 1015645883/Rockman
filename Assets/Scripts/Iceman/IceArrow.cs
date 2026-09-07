using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class IceArrow : MonoBehaviour
{
    public float speed = 5f;
    public float maxDistance = 15f;
    public int damage = 4;
    public float damageCooldown = 1.4f;
    private float lastDamageTime;

    private Vector3 startPosition;
    private Vector2 moveDirection;
    private Rigidbody2D rb;

    public void SetDirection(Vector2 direction)
    {
        moveDirection = direction.normalized;

        // 翻转精灵
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.flipX = direction.x > 0;
        }

        // ✅ 设置刚体速度，而不是用 Translate
        if (rb != null)
        {
            rb.velocity = moveDirection * speed;
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;

        // 确保不会受重力影响
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // 初始化速度（防止没调用 SetDirection 时箭停着）
        if (moveDirection != Vector2.zero)
        {
            rb.velocity = moveDirection * speed;
        }
    }

    void Update()
    {
        // 超出最大射程，销毁
        if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && Time.time > lastDamageTime + damageCooldown)
        {
            HealthSystem playerHealth = other.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                int finalDamage = damage; // 默认伤害

                // 判断玩家当前角色
                string currentChar = PlayerPrefs.GetString("SelectedCharacter", "Rockman");
                if (currentChar == "Fireman")
                {
                    finalDamage = 6; // 属性克制
                }
                if (currentChar == "Iceman")
                {
                    finalDamage = 1; // 属性减免
                }
                playerHealth.TakeDamage(finalDamage);
                lastDamageTime = Time.time;
            }
        }
    }

    // ✅ 新增：反弹函数
    public void Deflect()
    {
        // 反转方向
        moveDirection = -moveDirection;

        // 保持速度不变
        rb.velocity = moveDirection * speed;

        // 改变标签，让它能伤害敌人
        gameObject.tag = "Bullet";

        // 可选：视觉变化，表示它已被反弹
        GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.5f);
    }
    public void DestroyBullet()
    {
        Destroy(gameObject);  // 删除子弹
    }
}
