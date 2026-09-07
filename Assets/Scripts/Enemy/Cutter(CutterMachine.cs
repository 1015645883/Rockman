using UnityEngine;

public class CutterProjectile : MonoBehaviour
{
    public float launchForce = 8f;
    public float maxFallDistance = 5f;
    public int damage = 2;
    public float damageCooldown = 1.5f;
    private float lastDamageTime;
    public Rigidbody2D rb;

    private Vector2 startPos;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Launch(Vector2 targetDirection)
    {
        startPos = transform.position;
        rb.velocity = new Vector2(targetDirection.x * 2f, launchForce);

        // 判断方向并翻转
        if (targetDirection.x > 0)
            spriteRenderer.flipX = true;
        else
            spriteRenderer.flipX = false;
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
