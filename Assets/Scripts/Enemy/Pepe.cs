using UnityEngine;

public class Pepe : MonoBehaviour
{
    public int health = 1;
    public int damage = 1;
    public float damageCooldown = 1.4f;
    private float lastDamageTime;

    [Header("移动参数")]
    public float moveSpeed = 3f;          // 移动速度
    public float verticalAmplitude = 3f;  // Y轴上下摆动的幅度
    public float verticalSpeed = 2f;      // 上下摆动速度
    private float waveTime;               // 当前波动的相位时间
    private float startY;                 // 出生时的Y位置
    private float spawnTime;              // 记录出生时间
    private float lifeTime = 30f;         // 存活时间限制
    private Rigidbody2D rb;
    public bool isFrozen = false;

    [Header("方向设置")]
    public bool moveLeft = true;          // 是否向左移动（默认 true）
    private int direction = -1;           // 实际方向控制（-1=左，1=右）

    [Header("音效 & 动画")]
    public AudioClip Damage;
    private AudioSource audioSource;
    public Animator animator;             // Animator 组件
    private SpriteRenderer sr;            // 精灵组件

    void Start()
    {
        waveTime = 0f;
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;

        startY = transform.position.y;
        spawnTime = Time.time;

        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        // 根据方向变量决定移动方向与朝向
        if (moveLeft)
        {
            direction = -1;     // 向左移动
            if (sr != null) sr.flipX = false;
        }
        else
        {
            direction = 1;      // 向右移动
            if (sr != null) sr.flipX = true;
        }
    }

    void FixedUpdate()
    {
        if (isFrozen) return;

        if (Time.time - spawnTime >= lifeTime)
        {
            Die();
            return;
        }

        // 手动累计波动时间
        waveTime += Time.fixedDeltaTime * verticalSpeed;

        // 上下波动
        float offsetY = Mathf.Sin(waveTime) * verticalAmplitude;

        // 水平移动方向由 direction 决定
        Vector2 newPos = new Vector2(
            transform.position.x + direction * moveSpeed * Time.fixedDeltaTime,
            startY + offsetY
        );
        rb.MovePosition(newPos);
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

        if (collision.CompareTag("Bullet"))
        {
            Bullet bullet = collision.GetComponent<Bullet>();
            TakeDamage(1);
            bullet.DestroyBullet();
        }
        if (collision.CompareTag("ChargeBullet"))
        {
            Bullet bullet = collision.GetComponent<Bullet>();
            TakeDamage(1);
        }
        if (collision.CompareTag("RollingCutter"))
        {
            RollingCutter cutter = collision.GetComponent<RollingCutter>();
            if (cutter != null && Time.time > cutter.lastDamageTime + cutter.damageCooldown)
            {
                TakeDamage(1);
                cutter.lastDamageTime = Time.time;
            }
            CShockWave shockWave = collision.GetComponent<CShockWave>();
            if (shockWave != null && Time.time > shockWave.lastDamageTime + shockWave.damageCooldown)
            {
                TakeDamage(1);
                shockWave.lastDamageTime = Time.time;
            }
        }
        if (collision.CompareTag("SuperArm")) TakeDamage(1);
        if (collision.CompareTag("IceArrow"))
        {
            RIceArrow iceArrow = collision.GetComponent<RIceArrow>();
            if (iceArrow != null)
            {
                if (!isFrozen)
                {
                    iceArrow.Freeze(this);
                    iceArrow.DestroyBullet();
                }
                else
                {
                    TakeDamage(1);
                    iceArrow.DestroyBullet();
                }
            }
        }
        if (collision.CompareTag("HyperBomb")) TakeDamage(1);
        if (collision.CompareTag("FireStorm")) TakeDamage(1);
        if (collision.CompareTag("ThunderBeam")) TakeDamage(1);
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
        if (collision.CompareTag("RollingCutter"))
        {
            RollingCutter cutter = collision.GetComponent<RollingCutter>();
            if (cutter != null && Time.time > cutter.lastDamageTime + cutter.damageCooldown)
            {
                TakeDamage(1);
                cutter.lastDamageTime = Time.time;
            }
            CShockWave shockWave = collision.GetComponent<CShockWave>();
            if (shockWave != null && Time.time > shockWave.lastDamageTime + shockWave.damageCooldown)
            {
                TakeDamage(1);
                shockWave.lastDamageTime = Time.time;
            }
        }
    }

    void TakeDamage(int amount)
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = Damage;
            audioSource.playOnAwake = false;
        }

        audioSource.Play();
        health -= amount;
        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        if (rb != null) rb.simulated = false;
        animator.SetTrigger("Die");
        EnemyAutoDrop drop = GetComponent<EnemyAutoDrop>();
        if (drop != null)
        {
            drop.TryDrop();
        }
        Destroy(gameObject, 0.18f);
    }
}
