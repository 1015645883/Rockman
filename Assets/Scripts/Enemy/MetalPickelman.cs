using UnityEngine;
using System.Collections;


public class MetalPickelman : MonoBehaviour
{
    [Header("属性")]
    public int health = 3;
    public float moveSpeed = 2f;
    public int damage = 3;
    public float damageCooldown = 1.5f;
    private float lastDamageTime;
    public bool isFrozen = false;
    [Header("巡逻参数")]
    public float movementRange = 4f; // 左右活动范围
    private float originX;           // 出生点
    private Vector2 direction = Vector2.left;

    [Header("组件")]
    private Rigidbody2D rb;
    private Animator animator;
    private AudioSource audioSource;
    private SpriteRenderer spriteRenderer;
    [Header("碰撞体引用")]
    public Collider2D headCollider;   // 在Inspector拖拽“头部Collider”
    public Collider2D bodyCollider;   // 在Inspector拖拽“身体Collider”

    [Header("音效")]
    public AudioClip hitSfx;
    [Header("特效")]
    public GameObject explosionPrefab; 

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        originX = transform.position.x;
    }

    private void Update()
    {
        if (isFrozen) return;
        PatrolMove();
    }

    private void PatrolMove()
    {
        // 判断是否到达边界，需要转向
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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 攻击玩家
        if (collision.CompareTag("Player") && Time.time > lastDamageTime + damageCooldown)
        {
            HealthSystem playerHealth = collision.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                lastDamageTime = Time.time;
            }
        }
        // SuperArm 和 HyperBomb → 不管头还是身体都受伤（只判定一次）
        if (collision.CompareTag("SuperArm"))
        {
            TakeDamage(4);
            return;
        }
        if (collision.CompareTag("HyperBomb"))
        {
            TakeDamage(2);
            return;
        }
        // 处理子弹命中
        if (collision.CompareTag("Bullet") ||
            collision.CompareTag("ChargeBullet") ||
            collision.CompareTag("RollingCutter") ||
            collision.CompareTag("IceArrow") ||
            collision.CompareTag("FireStorm") ||
            collision.CompareTag("ThunderBeam"))
        {
            // 命中身体 → 不掉血（反弹/销毁）
            if (bodyCollider != null && collision.IsTouching(bodyCollider))
            {
                Bullet bullet = collision.GetComponent<Bullet>();
                if (bullet != null) bullet.Deflect();
                RIceArrow iceArrow = collision.GetComponent<RIceArrow>();
                if (iceArrow != null)
                {
                    if (!isFrozen) // 如果敌人未被冻结
                    {
                        iceArrow.Freeze(this); // 调用冰冻方法
                        iceArrow.DestroyBullet();

                    }
                    else // 已被冻结，再次接触造成大量伤害
                    {
                        iceArrow.DestroyBullet();
                    }
                }
                FireStorm fire = collision.GetComponent<FireStorm>();
                if (fire != null) fire.DestroyBullet();
                ThunderBeam thunder = collision.GetComponent<ThunderBeam>();
                if (thunder != null)
                {
                    thunder.DestroyOnEnemyHit();
                }
                return;
            }

            // 命中头部 → 扣血
            if (headCollider != null && collision.IsTouching(headCollider))
            {
                // RollingCutter
                if (collision.CompareTag("RollingCutter"))
                {
                    RollingCutter cutter = collision.GetComponent<RollingCutter>();
                    if (cutter != null && Time.time > cutter.lastDamageTime + cutter.damageCooldown)
                    {
                        TakeDamage(1);
                        cutter.lastDamageTime = Time.time;
                    }
                    CShockWave shockWave = collision.GetComponent<CShockWave>();
                    if (shockWave != null)
                    {
                        TakeDamage(1);
                        return;
                    }
                }
                // IceArrow
                else if (collision.CompareTag("IceArrow"))
                {
                    RIceArrow iceArrow = collision.GetComponent<RIceArrow>();
                    if (iceArrow != null)
                    {
                        if (!isFrozen) // 你可以在RIceArrow里加方法判断
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
                // FireStorm
                else if (collision.CompareTag("FireStorm"))
                {
                    TakeDamage(2);
                }
                // ThunderBeam
                else if (collision.CompareTag("ThunderBeam"))
                {
                    ThunderBeam thunder = collision.GetComponent<ThunderBeam>();
                    SmallThunderBeam smallThunder = collision.GetComponent<SmallThunderBeam>();

                    if (thunder != null)
                    {
                        TakeDamage(3);
                        thunder.DestroyOnEnemyHit();
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
                // 普通子弹
                else if (collision.CompareTag("Bullet"))
                {
                    Bullet bullet = collision.GetComponent<Bullet>();
                    TakeDamage(1);
                    bullet.DestroyBullet();
                }
                else if (collision.CompareTag("ChargeBullet"))
                {
                    Bullet bullet = collision.GetComponent<Bullet>();
                    TakeDamage(4);
                    bullet.DestroyBullet();
                }
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

    private void TakeDamage(int amount)
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = hitSfx;
            audioSource.playOnAwake = false;
        }

        if (hitSfx != null)
            audioSource.Play();

        health -= amount;

        // ✅ 播放受击闪烁效果
        if (spriteRenderer != null)
            StartCoroutine(HitFlash());

        if (health <= 0)
        {
            Die();
        }
    }
    private IEnumerator HitFlash()
    {
        if (spriteRenderer == null) yield break;

        // 先隐藏
        spriteRenderer.enabled = false;

        // 等待0.1秒
        yield return new WaitForSeconds(0.06f);

        // 显示回来
        spriteRenderer.enabled = true;

        yield return null;
    }

    private void Die()
    {
        // 生成爆炸特效
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        // 禁用碰撞和物理
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
