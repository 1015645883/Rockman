using UnityEngine;

public class Thunder3 : MonoBehaviour
{
    public float speed = 6f;
    public int damage = 4;
    public float damageCooldown = 1f;

    private float lastDamageTime;
    private Rigidbody2D rb;

    //  飞行方向（-1=左，1=右）
    private int direction;

    //  边界
    private float leftBound = 56f;
    private float rightBound = 71f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // 特殊角色加伤
        string selectedChar = PlayerPrefs.GetString("SelectedCharacter", "");
        if (selectedChar == "Iceman")
        {
            damage += 1;
        }
    }

    public void Initialize(int dir)
    {
        direction = dir;

        if (rb == null) rb = GetComponent<Rigidbody2D>();

        rb.velocity = new Vector2(direction * speed, 0f);

        //  控制朝向
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.flipX = (direction > 0); // 向右时 flipX = true
        }
    }

    private void Update()
    {
        float x = transform.position.x;

        //  左边界
        if (direction < 0 && x <= leftBound)
        {
            Destroy(gameObject);
        }

        //  右边界
        if (direction > 0 && x >= rightBound)
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
}