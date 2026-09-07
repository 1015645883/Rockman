using UnityEngine;
using System.Collections;

public class Carricarry : MonoBehaviour
{
    [Header("基础参数")]
    public float moveSpeed = 2f;          // 移动速度
    public float movementRange = 3f;      // 左右移动范围
    public int health = 4;                // 血量
    public int damage = 2;
    public float damageCooldown = 1.5f;
    private float lastDamageTime;

    [Header("引用")]
    public Rigidbody2D rb;
    public Animator animator;
    public AudioSource audioSource;
    private SpriteRenderer spriteRenderer;

    [Header("音效")]
    public AudioClip hitSfx;
    public AudioClip throwSfx;
    public GameObject explosionPrefab;

    [Header("投掷相关")]
    public GameObject rockPrefab;          // 石块预制体
    public Transform throwPoint;           // 投掷位置
    public float throwForce = 8f;          // 投掷力度
    public float throwInterval = 1f;       // 投掷间隔
    public float throwAngle = 45f;         // 投掷角度（度数）

    private Vector2 direction = Vector2.left; // 初始方向
    private float originX;                  // 起始点 X
    private bool isAwake = false;           // 是否进入攻击状态
    public bool isFrozen = false;          // 冰冻状态

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originX = transform.position.x;

        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (isFrozen) return;

        if (!isAwake)  // 只在睡眠状态下巡逻
        {
            PatrolMove();
        }
    }

    private void PatrolMove()
    {
        if (ShouldTurnByRange())
        {
            Flip();
        }

        rb.velocity = new Vector2(direction.x * moveSpeed, rb.velocity.y);
    }

    private bool ShouldTurnByRange()
    {
        float posX = transform.position.x;
        if (direction.x < 0 && posX <= originX - movementRange)
            return true;
        if (direction.x > 0 && posX >= originX + movementRange)
            return true;
        return false;
    }

    private void Flip()
    {
        direction = -direction;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1;
        transform.localScale = localScale;
    }

    private void ThrowRock()
    {
        if (rockPrefab == null || throwPoint == null) return;
        if (isFrozen) return;
        // 同时朝左右方向丢石头
        ThrowOneRock(Vector2.left);
        ThrowOneRock(Vector2.right);

        if (audioSource != null && throwSfx != null)
            audioSource.PlayOneShot(throwSfx);
    }

    private void ThrowOneRock(Vector2 dir)
    {
        GameObject rock = Instantiate(rockPrefab, throwPoint.position, Quaternion.identity);

        Rigidbody2D rockRb = rock.GetComponent<Rigidbody2D>();
        if (rockRb != null)
        {
            float angleRad = throwAngle * Mathf.Deg2Rad;
            Vector2 force = new Vector2(Mathf.Cos(angleRad) * dir.x, Mathf.Sin(angleRad)) * throwForce;
            rockRb.velocity = force;
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

        if (collision.CompareTag("Bullet"))
        {
            Bullet bullet = collision.GetComponent<Bullet>(); // 获取 Bullet 组件
            TakeDamage(1);
            bullet.DestroyBullet(); // 调用 Bullet 的 DestroyBullet() 方法
        }

        if (collision.CompareTag("ChargeBullet"))
        {
            Bullet bullet = collision.GetComponent<Bullet>(); // 获取 Bullet 组件
            TakeDamage(4);
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
                TakeDamage(2);
                shockWave.lastDamageTime = Time.time;
            }
        }
        if (collision.CompareTag("SuperArm"))
        {
            TakeDamage(4);
        }
        if (collision.CompareTag("IceArrow"))
        {
            RIceArrow iceArrow = collision.GetComponent<RIceArrow>();
            if (iceArrow != null)
            {
                if (!isFrozen) // 如果敌人未被冻结
                {
                    iceArrow.Freeze(this); // 调用冰冻方法
                    TakeDamage(1);
                    iceArrow.DestroyBullet();
                }
                else // 已被冻结，再次接触造成大量伤害
                {
                    TakeDamage(1); // 可以根据实际需求调整伤害数值
                    iceArrow.DestroyBullet();
                }
            }
        }
        if (collision.CompareTag("HyperBomb"))
        {
            TakeDamage(4);
        }
        if (collision.CompareTag("FireStorm"))
        {
            TakeDamage(2);
        }
        if (collision.CompareTag("ThunderBeam"))
        {
            ThunderBeam thunder = collision.GetComponent<ThunderBeam>();
            SmallThunderBeam smallThunder = collision.GetComponent<SmallThunderBeam>();
            // 只有在Wake状态下才会受到伤害
            if (thunder != null)
            {
                TakeDamage(4);
                return;
            }
            if (smallThunder != null)
            {
                if (Time.time >= smallThunder.lastDamageTime + smallThunder.damageCooldown)
                {
                    TakeDamage(2);
                    smallThunder.lastDamageTime = Time.time;
                }
                return;
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
                TakeDamage(2);
                shockWave.lastDamageTime = Time.time;
            }
        }
    }

    public void TakeDamage(int amount)
    {
        if (health <= 0) return;

        health -= amount;

        if (audioSource != null && hitSfx != null)
            audioSource.PlayOneShot(hitSfx);

        if (spriteRenderer != null)
            StartCoroutine(HitFlash());

        if (!isAwake) // 第一次受伤，进入觉醒状态
        {
            if (!isFrozen)
            {
                isAwake = true;
                rb.velocity = Vector2.zero;
                animator.SetTrigger("Wake");
                InvokeRepeating(nameof(ThrowRock), 1f, throwInterval);
            }
        }

        if (health <= 0)
        {
            Die();
        }
    }

    private IEnumerator HitFlash()
    {
        if (spriteRenderer == null) yield break;

        spriteRenderer.enabled = false;
        yield return new WaitForSeconds(0.06f);
        spriteRenderer.enabled = true;
    }

    private void Die()
    {
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        if (rb != null) rb.simulated = false;
        EnemyAutoDrop drop = GetComponent<EnemyAutoDrop>();
        if (drop != null)
        {
            drop.TryDrop();
        }
        Destroy(gameObject, 0.02f);
    }
}
