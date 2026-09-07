using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyFireStorm : MonoBehaviour
{
    [Header("子弹参数")]
    public float speed = 9f;
    public float maxDistance = 15f;
    public int damage = 3;
    public float damageCooldown = 1.4f;
    private float lastDamageTime;

    private Vector3 startPosition;
    private Vector2 moveDirection;
    private Rigidbody2D rb;

    [Header("GroundCheck 设置")]
    public Transform groundCheck;         // 在预制体里放一个子物体
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;         // 地面层

    [Header("残留火焰")]
    public GameObject fireResiduePrefab;  // 残留火焰预制体
    private bool spawnedResidue = false;  // 避免重复生成

    private Transform player;

    public void SetDirection(Vector2 direction)
    {
        moveDirection = direction.normalized;

        // 翻转精灵
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.flipX = direction.x > 0;
        }

        // ✅ 设置刚体速度
        if (rb != null)
        {
            rb.velocity = moveDirection * speed;
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;

        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (moveDirection != Vector2.zero)
        {
            rb.velocity = moveDirection * speed;
        }

        // 找玩家
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    void Update()
    {
        // 超出最大射程，销毁
        if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
        {
            Destroy(gameObject);
            return;
        }

        // ✅ GroundCheck 检测贴地
        if (!spawnedResidue && player != null)
        {
            bool isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

            if (isGrounded)
            {
                Debug.Log("现在正在贴地");
                // 检测是否和玩家 X 轴重合
                if (Mathf.Abs(player.position.x - transform.position.x) < 0.2f)
                {
                    GameObject residue = Instantiate(fireResiduePrefab, groundCheck.position, Quaternion.identity);
                    Destroy(residue, 1.5f);
                    spawnedResidue = true;
                }
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && Time.time > lastDamageTime + damageCooldown)
        {
            HealthSystem playerHealth = other.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                int finalDamage = damage;

                string currentChar = PlayerPrefs.GetString("SelectedCharacter", "Rockman");
                if (currentChar == "Bombman") finalDamage = 5;
                if (currentChar == "Iceman" || currentChar == "Fireman") finalDamage = 1;

                playerHealth.TakeDamage(finalDamage);
                lastDamageTime = Time.time;
            }
        }
    }

    // ✅ 反弹函数
    public void Deflect()
    {
        moveDirection = -moveDirection;
        rb.velocity = moveDirection * speed;
        gameObject.tag = "Bullet";
        GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.5f);
    }

    public void DestroyBullet()
    {
        Destroy(gameObject);
    }
}
