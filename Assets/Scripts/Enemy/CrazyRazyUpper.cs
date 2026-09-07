using System.Collections;
using UnityEngine;

public class CrazyRazyUpper : MonoBehaviour
{
    [Header("基础属性")]
    public int health = 4;
    public int damage = 3;
    public float damageCooldown = 1.4f;
    private float lastDamageTime;

    public float moveSpeed = 3f;       // 普通水平移动速度
    public float diveSpeed = 6f;       // 俯冲速度
    public float attackCooldown = 2f;  // 攻击冷却时间
    public float xAttackRange = 1.5f;  // 触发俯冲的X轴距离

    private float lastAttackTime;
    private Transform player;
    private SpriteRenderer sr;
    private Animator animator;

    private bool isInvincible = true;
    private float invincibleTime = 0.5f;

    [Header("音效 & 动画")]
    public AudioClip Damage;
    private AudioSource audioSource;
    private SpriteRenderer spriteRenderer;
    [Header("特效")]
    public GameObject explosionPrefab;
    private float baseY;               // 固定基准飞行高度 (出生点.y + 2)
    public bool isDiving = false;     // 是否在俯冲攻击
    public bool isFrozen = false;
    public bool diveInterrupted = false;
    private bool hasReachedBase = false; // 是否到达基准高度

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        // 固定基准飞行高度
        baseY = transform.position.y + 3f;

        // 出生无敌 0.5s
        StartCoroutine(InvincibleCoroutine());

        // 出生时先飞到基准高度
        StartCoroutine(MoveToBasePosition());
    }

    private void Update()
    {
        if (!hasReachedBase) return; // 没到基准高度，不跟踪
        if (isFrozen)
        {
            return;
        }
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            return;
        }

        if (isDiving) return;

        // 翻转朝向
        if (sr != null)
            sr.flipX = player.position.x > transform.position.x;

        // X轴跟随玩家，Y保持基准高度
        Vector3 targetPos = new Vector3(player.position.x, baseY, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        // 检查是否满足俯冲条件
        if (Mathf.Abs(player.position.x - transform.position.x) < xAttackRange
            && Time.time > lastAttackTime + attackCooldown)
        {
            StartCoroutine(DiveAttack());
        }
    }

    IEnumerator DiveAttack()
    {
        isDiving = true;
        lastAttackTime = Time.time;
        animator?.SetBool("isAttacking", true);

        // 俯冲目标
        Vector3 diveTarget = new Vector3(player.position.x, player.position.y - 0.5f, transform.position.z);

        while (Vector3.Distance(transform.position, diveTarget) > 0.1f)
        {
            if (isFrozen || diveInterrupted)
            {
                Debug.Log("1");
                isDiving = false;
                diveInterrupted = false;
                animator?.SetBool("isAttacking", false);
                break; // 被冰冻或被强制中断
            }
            transform.position = Vector3.MoveTowards(transform.position, diveTarget, diveSpeed * Time.deltaTime);
            yield return null;
        }

        // 回升
        Vector3 returnTarget = new Vector3(player.position.x, baseY, transform.position.z);
        while (Vector3.Distance(transform.position, returnTarget) > 0.05f)
        {
            if (isFrozen || diveInterrupted)
            {
                Debug.Log("1");
                isDiving = false;
                diveInterrupted = false;
                animator?.SetBool("isAttacking", false);
                break; // 被冰冻或被强制中断
            }
            transform.position = Vector3.MoveTowards(transform.position, returnTarget, moveSpeed * Time.deltaTime);
            yield return null;
        }

        isDiving = false;
        animator?.SetBool("isAttacking", false);
    }


    IEnumerator InvincibleCoroutine()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibleTime);
        isInvincible = false;
    }

    IEnumerator MoveToBasePosition()
    {
        Vector3 target = new Vector3(transform.position.x, baseY, transform.position.z);
        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }

        hasReachedBase = true; // 到达基准高度后才开始跟踪玩家
    }

    // ====== 碰撞伤害 & 受击逻辑 ======
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
            TakeDamage(4);
            bullet.DestroyBullet();
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
        if (collision.CompareTag("SuperArm")) TakeDamage(4);
        if (collision.CompareTag("IceArrow"))
        {
            RIceArrow iceArrow = collision.GetComponent<RIceArrow>();
            if (iceArrow != null)
            {
                if (!isFrozen && isInvincible == false)
                {
                    TakeDamage(1);
                    iceArrow.Freeze(this);
                    iceArrow.DestroyBullet();
                }
                else
                {
                    TakeDamage(2);
                    iceArrow.DestroyBullet();
                }
            }
        }
        if (collision.CompareTag("HyperBomb")) TakeDamage(4);
        if (collision.CompareTag("FireStorm")) TakeDamage(2);
        if (collision.CompareTag("ThunderBeam"))
        {
            ThunderBeam thunder = collision.GetComponent<ThunderBeam>();
            SmallThunderBeam smallThunder = collision.GetComponent<SmallThunderBeam>();
            if (thunder != null)
            {
                TakeDamage(4);
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
        if (isInvincible) return;

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
        EnemyAutoDrop drop = GetComponent<EnemyAutoDrop>();
        if (drop != null)
        {
            drop.TryDrop();
        }
        Destroy(gameObject);
        // 在当前位置生成爆炸特效
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }
    }
}
