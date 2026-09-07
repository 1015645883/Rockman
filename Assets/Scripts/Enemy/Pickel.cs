using UnityEngine;

public class Pickel : MonoBehaviour
{
    public float launchForce = 8f;
    public float maxFallDistance = 18f;
    public int damage = 2;
    public float damageCooldown = 1.5f;
    private float lastDamageTime;
    private Vector2 startPos;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Transform player;
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>(); // ✅ 重要
    }

    public void SetPlayer(Transform playerTransform)
    {
        player = playerTransform;
    }

    public void Launch(Vector2 targetDirection)
    {
        startPos = transform.position;

        // 计算到玩家的距离（只考虑水平）
        float horizontalDistance = Mathf.Abs(player.position.x - transform.position.x);

        // 让水平速度和玩家水平距离成比例
        float horizontalSpeed = Mathf.Clamp(horizontalDistance, 2f, 10f); // 最小2，最大10

        rb.velocity = new Vector2(Mathf.Sign(targetDirection.x) * horizontalSpeed, launchForce);

        spriteRenderer.flipX = targetDirection.x > 0;
    }


    void Update()
    {
        if (Vector2.Distance(startPos, transform.position) >= maxFallDistance)
        {
            Destroy(gameObject);
        }
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
    }

    private void OnTriggerStay2D(Collider2D collision)
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
    }
}
