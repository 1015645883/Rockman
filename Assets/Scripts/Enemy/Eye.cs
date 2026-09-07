using System.Collections;
using UnityEngine;

public class EyeEnemy : MonoBehaviour
{
    public Animator animator;  // Animator 组件
    public float moveSpeed = 2f;   // 移动速度
    public bool moveOnXAxis = true; // true: 沿 X 轴移动, false: 沿 Y 轴移动
    public LayerMask wallLayer;    // 墙壁的 Layer
    public float checkSize = 0.1f; // 正方形检测区域的半边长（减小值以提高灵敏度）
    public float checkDistance = 0.15f; // 检测前方的距离（增加值确保提前检测）
    public int damage = 2;
    public float damageCooldown = 1.4f;
    private float lastDamageTime;
    public float selfDamageCooldown = 0.5f;
    private float lastSelfDamageTime;


    public int health = 5; // 新增血量
    private bool isSleeping = false;
    private bool wasTouchingWall = false; // 记录上一次是否检测到墙壁
    private Vector2 moveDirection;
    private Rigidbody2D rb;
    private AudioSource audioSource;
    public AudioClip Damage;
    private SpriteRenderer spriteRenderer;
    public bool isFrozen = false;
    public bool isDead = false;
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        // 根据选择的移动方向设置初始移动方向
        moveDirection = moveOnXAxis ? Vector2.right : Vector2.up;
        animator = GetComponent<Animator>(); // 获取 Animator 组件
    }

    void FixedUpdate()
    {
        if (isFrozen || isDead)
        {
            return;
        }
        bool isTouchingWall = CheckWallCollision();

        // 如果检测到墙壁，并且之前没有靠墙
        if (isTouchingWall && !wasTouchingWall)
        {
            StartCoroutine(SleepRoutine()); // 进入休眠状态
        }
        // 如果脱离墙壁，并且之前是靠墙状态
        else if (!isTouchingWall && wasTouchingWall)
        {
            WakeUp(); // 解除休眠
        }

        if (!isSleeping)
        {
            transform.Translate(moveDirection * moveSpeed * Time.deltaTime);
        }
        // 记录当前的墙壁状态，用于下次对比
        wasTouchingWall = isTouchingWall;
    }

    /// <summary>
    /// 正方形区域检测墙壁
    /// </summary>
    bool CheckWallCollision()
    {
        Vector2 checkPosition = (Vector2)transform.position + moveDirection * checkDistance;
        Collider2D hit = Physics2D.OverlapBox(checkPosition, new Vector2(checkSize * 2, checkSize * 2), 0f, wallLayer);
        return hit != null;
    }

    /// <summary>
    /// 进入休眠状态（播放 Sleep 动画，并在 5 秒后恢复）
    /// </summary>
    IEnumerator SleepRoutine()
    {
        isSleeping = true;
        animator.SetTrigger("SleepTrigger"); // 播放 Sleep 动画
        yield return new WaitForSeconds(5f);

        // 反转方向，确保 X/Y 轴方向反转都适用
        moveDirection *= -1;
    }

    /// <summary>
    /// 解除休眠（播放 Wake 动画，并恢复移动）
    /// </summary>
    void WakeUp()
    {
        isSleeping = false;
        animator.SetTrigger("WakeTrigger"); // 播放 Wake 动画
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
            TakeDamage(5);
        }

        if (collision.CompareTag("RollingCutter"))
        {
            RollingCutter cutter = collision.GetComponent<RollingCutter>();
            if (cutter != null && Time.time > cutter.lastDamageTime + cutter.damageCooldown)
            {
                TakeDamage(2);
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
            TakeDamage(5);
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
                    TakeDamage(2); // 可以根据实际需求调整伤害数值
                    iceArrow.DestroyBullet();
                }
            }
        }
        if (collision.CompareTag("HyperBomb"))
        {
            TakeDamage(5);
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
                TakeDamage(5);
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
        // 玩家与敌人持续重叠时才伤害玩家
        if (collision.CompareTag("Player") && Time.time > lastDamageTime + damageCooldown)
        {
            HealthSystem playerHealth = collision.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                lastDamageTime = Time.time;  // 更新最后一次伤害时间
            }
        }

        if (collision.CompareTag("RollingCutter"))
        {
            RollingCutter cutter = collision.GetComponent<RollingCutter>();
            if (cutter != null && Time.time > cutter.lastDamageTime + cutter.damageCooldown)
            {
                TakeDamage(2);
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
        if (spriteRenderer != null)
            StartCoroutine(HitFlash());
    }

    private IEnumerator HitFlash()
    {
        if (spriteRenderer == null) yield break;

        spriteRenderer.enabled = false;
        yield return new WaitForSeconds(0.06f);
        spriteRenderer.enabled = true;
    }

    void Die()
    {
        isDead = true;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false;
        animator.SetTrigger("Die");
        EnemyAutoDrop drop = GetComponent<EnemyAutoDrop>();
        if (drop != null)
        {
            drop.TryDrop();
        }
        Destroy(gameObject, 0.3f);
    }
    // 可选：绘制检测范围（方便调试）
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Vector2 checkPosition = (Vector2)transform.position + moveDirection * checkDistance;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(checkPosition, new Vector2(checkSize * 2, checkSize * 2));
    }
}
