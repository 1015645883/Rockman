using UnityEngine;

public class RollingCutter_Ground : MonoBehaviour
{
    [Header("滚地参数")]
    public float groundSpeed = 6f;
    public float groundLifeTime = 5f;

    [Header("碰撞检测")]
    public LayerMask groundLayer;
    public Transform groundCheck;
    public Vector2 groundCheckSize = new Vector2(1f, 0.5f);

    [Header("伤害")]
    public int damage = 2;
    public float damageCooldown = 1f;
    private float lastDamageTime;

    [Header("反弹控制")]
    public int maxBounces = 3;
    public float bounceProtectTime = 0.2f;

    [Header("初始保护")]
    public float startProtectTime = 0.3f;

    [Header("音效")]
    public AudioClip cutterLoopSound;

    private float direction = 1f;
    private float lifeTimer = 0f;

    private float lastBounceTime = -10f;
    private int bounceCount = 0;

    private float protectTimer = 0f;
    private bool isDying = false;

    private AudioSource audioSource;
    private Animator animator;

    private bool hasDestroyedCallback = false;
    private PlayerShooting playerShooting;

    // ⭐ 新增：Boss引用
    private BossHealthSystem bossHealth;

    // ⭐ 初始化（给Boss用）
    public void Initialize(float dir, Transform player)
    {
        direction = Mathf.Sign(dir);

        playerShooting = player.GetComponent<PlayerShooting>();

        // ⭐ 获取Boss血量系统
        bossHealth = player.GetComponent<BossHealthSystem>();
        if (bossHealth != null)
        {
            bossHealth.OnBossDeathStart += OnBossDeath; // ✅ 订阅死亡事件
        }

        // 防止卡墙
        transform.position += Vector3.right * direction * 0.2f;

        // 音效
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = cutterLoopSound;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0.7f;
        audioSource.Play();
    }

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (isDying) return;

        protectTimer += Time.deltaTime;
        lifeTimer += Time.deltaTime;

        // 生命周期
        if (lifeTimer >= groundLifeTime)
        {
            Die();
            return;
        }

        // 墙检测
        Collider2D hit = Physics2D.OverlapBox(
            groundCheck.position,
            groundCheckSize,
            0f,
            groundLayer
        );

        if (hit != null && Time.time - lastBounceTime > bounceProtectTime)
        {
            direction *= -1f;
            lastBounceTime = Time.time;

            if (protectTimer >= startProtectTime)
            {
                bounceCount++;

                if (bounceCount >= maxBounces)
                {
                    Die();
                    return;
                }
            }
        }

        // 移动
        transform.position += Vector3.right * direction * groundSpeed * Time.deltaTime;
    }

    void Die()
    {
        if (isDying) return;

        isDying = true;

        animator?.SetTrigger("Die");

        if (audioSource != null)
            audioSource.Stop();

        Destroy(gameObject, 0.18f);

        if (!hasDestroyedCallback)
        {
            hasDestroyedCallback = true;
            playerShooting?.OnBulletDestroyed();
        }
    }

    // ⭐ Boss死亡时调用
    private void OnBossDeath()
    {
        Die(); // 直接销毁
    }

    private void OnDestroy()
    {
        // ⭐ 取消订阅（防止报错！）
        if (bossHealth != null)
        {
            bossHealth.OnBossDeathStart += OnBossDeath;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && Time.time > lastDamageTime + damageCooldown)
        {
            HealthSystem playerHealth = collision.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                int finalDamage = damage;

                string currentChar = PlayerPrefs.GetString("SelectedCharacter", "Rockman");

                if (currentChar == "Bombman")
                    finalDamage = 3;

                if (currentChar == "Elecman")
                    finalDamage = 4;

                playerHealth.TakeDamage(finalDamage);

                lastDamageTime = Time.time;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
    }
}