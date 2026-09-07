using UnityEngine;

public class FlyingBomb : MonoBehaviour
{
    public int health = 1;

    [Header("移动参数")]
    public float moveSpeed = 3f;          // 飞行速度
    public float verticalAmplitude = 3f;  // Y轴上下摆动的幅度
    public float verticalSpeed = 2f;      // 上下摆动速度
    private float waveTime;               // 当前波动的相位时间

    private float startY;                 // 出生时的Y位置
    private float spawnTime;              // 出生时间
    private float lifeTime = 30f;         // 存活时间限制
    private Rigidbody2D rb;
    public bool isFrozen = false;

    [Header("方向设置")]
    public bool moveLeft = true;          // 是否向左移动（默认true）
    private SpriteRenderer sr;            // SpriteRenderer引用

    [Header("音效 & 爆炸")]
    public AudioClip Damage;
    public GameObject explosionPrefab;
    private AudioSource audioSource;

    void Start()
    {
        waveTime = 0f;
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;

        sr = GetComponent<SpriteRenderer>();
        if (moveLeft)
        {
            if (sr != null) sr.flipX = false;
        }
        else
        {
            if (sr != null) sr.flipX = true;
        }

        startY = transform.position.y;
        spawnTime = Time.time;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.clip = Damage;
    }

    void FixedUpdate()
    {
        if (isFrozen) return;

        if (Time.time - spawnTime >= lifeTime)
        {
            Die();
            return;
        }

        waveTime += Time.fixedDeltaTime * verticalSpeed;
        float offsetY = Mathf.Sin(waveTime) * verticalAmplitude;

        float direction = moveLeft ? -1f : 1f;
        Vector2 newPos = new Vector2(
            transform.position.x + direction * moveSpeed * Time.fixedDeltaTime,
            startY + offsetY
        );

        rb.MovePosition(newPos);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        ApplyCollisionDamage(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        ApplyCollisionDamage(collision);
    }

    private void ApplyCollisionDamage(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Die();
        }

        if (collision.CompareTag("Bullet") || collision.CompareTag("ChargeBullet"))
        {
            TakeDamage(1);
            Bullet bullet = collision.GetComponent<Bullet>();
            if (bullet != null) bullet.DestroyBullet();
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

        if (collision.CompareTag("SuperArm") || collision.CompareTag("HyperBomb") ||
            collision.CompareTag("FireStorm") || collision.CompareTag("ThunderBeam"))
        {
            TakeDamage(1);
        }

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
    }

    void TakeDamage(int amount)
    {
        if (audioSource != null) audioSource.Play();
        health -= amount;
        if (health <= 0) Die();
    }

    void Die()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (rb != null) rb.simulated = false;

        // 生成爆炸特效
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        EnemyAutoDrop drop = GetComponent<EnemyAutoDrop>();
        if (drop != null)
        {
            drop.TryDrop();
        }

        Destroy(gameObject, 0.18f);
    }
}
