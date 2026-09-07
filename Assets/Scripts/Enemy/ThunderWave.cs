using UnityEngine;

public class ThunderWave : MonoBehaviour
{
    public float speed = 8f;
    public float lifeTime = 5f;
    public int damage = 2;
    public float damageCooldown = 1f;
    private float lastDamageTime;
    private Rigidbody2D rb;

    private Vector2 moveDirection = Vector2.right; // 默认方向向右

    public void SetDirection(Vector2 dir)
    {
        moveDirection = dir.normalized;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = moveDirection * speed;
        }

        // 特殊处理：如果角色是 Iceman，伤害+1
        string selectedChar = PlayerPrefs.GetString("SelectedCharacter", "");
        if (selectedChar == "Iceman")
        {
            damage += 1;
        }

        Destroy(gameObject, lifeTime);
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
}
