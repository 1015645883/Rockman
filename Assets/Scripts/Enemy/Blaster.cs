using UnityEngine;
using System.Collections;

public class BlasterEnemy : MonoBehaviour
{
    public enum BlasterState
    {
        Sleep,
        Wake
    }

    public BlasterState currentState = BlasterState.Sleep;
    public int health = 1; // 新增血量
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float timeBetweenShots = 0.3f;
    public float bulletSpeed = 10f;
    public float[] attackAngles = { 60f, 30f, -30f, -60f };
    public float sleepBetweenAttacks = 2f;
    public int damage = 2;
    public float damageCooldown = 1.5f;
    private float lastDamageTime;
    public float detectionRadius = 5f;
    public LayerMask playerLayer;
    public Transform Player
    {
        get
        {
            SpecialLevelManager slm = FindObjectOfType<SpecialLevelManager>();
            if (slm != null && slm.currentPlayer != null)
                return slm.currentPlayer.transform; // 特殊关卡当前受控角色

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            return playerObj != null ? playerObj.transform : null; // 普通关卡
        }
    }


    public Animator animator;
    public string wakeTrigger = "Wake";
    public string sleepTrigger = "Sleep";
    public float attackAnimationDelay = 1f;
    private AudioSource audioSource;
    public AudioClip Damage;
    private SpriteRenderer spriteRenderer;
    private bool isAttacking;
    private bool playerInRange;
    public bool isFrozen = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();
        SetAnimationState(currentState);
    }


    void Update()
    {
        if (isFrozen)
        {
            return;
        }
        CheckPlayerInRange();

        if (playerInRange && currentState == BlasterState.Sleep && !isAttacking)
        {
            ChangeState(BlasterState.Wake);
            StartCoroutine(DelayedAttackCycle()); // 使用延迟启动的协程
        }
        else if (!playerInRange && currentState == BlasterState.Wake)
        {
            ChangeState(BlasterState.Sleep);
            StopAllCoroutines();
            isAttacking = false;
        }
    }

    IEnumerator DelayedAttackCycle()
    {
        // 等待苏醒延迟时间
        yield return new WaitForSeconds(attackAnimationDelay);

        // 然后开始正常攻击循环
        StartCoroutine(AttackCycle());
    }

    void CheckPlayerInRange()
    {
        if (Player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, Player.position);
        playerInRange = distanceToPlayer <= detectionRadius;
    }


    void ChangeState(BlasterState newState)
    {
        if (currentState != newState)
        {
            currentState = newState;
            SetAnimationState(newState);
        }
    }

    void SetAnimationState(BlasterState state)
    {
        if (animator == null) return;

        switch (state)
        {
            case BlasterState.Wake:
                animator.ResetTrigger(sleepTrigger);
                animator.SetTrigger(wakeTrigger);
                break;
            case BlasterState.Sleep:
                animator.ResetTrigger(wakeTrigger);
                animator.SetTrigger(sleepTrigger);
                break;
        }
    }

    IEnumerator AttackCycle()
    {
        while (playerInRange)
        {
            isAttacking = true;
            yield return StartCoroutine(AttackSequence());

            ChangeState(BlasterState.Sleep);
            yield return new WaitForSeconds(sleepBetweenAttacks);

            if (playerInRange)
            {
                ChangeState(BlasterState.Wake);
                yield return new WaitForSeconds(attackAnimationDelay); // 每次唤醒都等待动画播放
            }
        }

        ChangeState(BlasterState.Sleep);
        isAttacking = false;
    }

    IEnumerator AttackSequence()
    {
        foreach (float angle in attackAngles)
        {
            FireAtAngle(angle);
            yield return new WaitForSeconds(timeBetweenShots);
        }
    }

    void FireAtAngle(float angle)
    {
        if (!bulletPrefab || !firePoint) return;

        float worldAngle = transform.eulerAngles.z + angle;
        Quaternion rotation = Quaternion.Euler(0, 0, worldAngle);

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, rotation);
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb) rb.velocity = rotation * Vector2.right * bulletSpeed;
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
            // 只有在Wake状态下才会受到伤害
            if (currentState == BlasterState.Wake)
            {
                Bullet bullet = collision.GetComponent<Bullet>();
                TakeDamage(1);
                if (bullet != null)
                {
                    bullet.DestroyBullet();
                }
            }
            else
            {
                // Sleep状态下可以添加子弹被弹开的效果
                Bullet bullet = collision.GetComponent<Bullet>();
                bullet.Deflect(); // 假设Bullet有弹开的方法
            }
        }

        if (collision.CompareTag("ChargeBullet"))
        {
            // 只有在Wake状态下才会受到伤害
            if (currentState == BlasterState.Wake)
            {
                Bullet bullet = collision.GetComponent<Bullet>();
                TakeDamage(1);
            }
            else
            {
                // Sleep状态下可以添加子弹被弹开的效果
                Bullet bullet = collision.GetComponent<Bullet>();
                bullet.Deflect(); // 假设Bullet有弹开的方法
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
            if (shockWave != null)
            {
                TakeDamage(1);
                return;
            }
        }
        if (collision.CompareTag("IceArrow"))
        {
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
                    TakeDamage(1); // 可以根据实际需求调整伤害数值
                    iceArrow.DestroyBullet();
                }
            }
        }
        if (collision.CompareTag("SuperArm"))
        {
            TakeDamage(1);
        }
        if (collision.CompareTag("HyperBomb"))
        {
            TakeDamage(1);
        }
        if (collision.CompareTag("FireStorm"))
        {
            // 只有在Wake状态下才会受到伤害
            if (currentState == BlasterState.Wake)
            {
                TakeDamage(1);
            }
            else
            {
                FireStorm fire = collision.GetComponent<FireStorm>();
                if (fire != null) fire.DestroyBullet();
            }
        }
        if (collision.CompareTag("ThunderBeam"))
        {
            ThunderBeam thunder = collision.GetComponent<ThunderBeam>();
            SmallThunderBeam smallThunder = collision.GetComponent<SmallThunderBeam>();
            // 只有在Wake状态下才会受到伤害
            if (currentState == BlasterState.Wake)
            {
                if (thunder != null)
                {
                    TakeDamage(1);
                    return;
                }
                if (smallThunder != null)
                {
                    if (Time.time >= smallThunder.lastDamageTime + smallThunder.damageCooldown)
                    {
                        TakeDamage(1);
                        smallThunder.lastDamageTime = Time.time;
                    }
                    return;
                }
            }
            else
            {
                if (thunder != null)
                {
                    thunder.DestroyOnEnemyHit();
                }
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
        if (collision.CompareTag("SuperArm"))
        {
            TakeDamage(1);
        }
        if (collision.CompareTag("FireStorm"))
        {
            // 只有在Wake状态下才会受到伤害
            if (currentState == BlasterState.Wake)
            {
                TakeDamage(1);
            }
            else
            {
                FireStorm fire = collision.GetComponent<FireStorm>();
                if (fire != null) fire.DestroyBullet();
            }
        }
        if (collision.CompareTag("ThunderBeam"))
        {
            ThunderBeam thunder = collision.GetComponent<ThunderBeam>();
            SmallThunderBeam smallThunder = collision.GetComponent<SmallThunderBeam>();
            // 只有在Wake状态下才会受到伤害
            if (currentState == BlasterState.Wake)
            {
                if (thunder != null)
                {
                    TakeDamage(1);
                    return;
                }
                if (smallThunder != null)
                {
                    if (Time.time >= smallThunder.lastDamageTime + smallThunder.damageCooldown)
                    {
                        TakeDamage(1);
                        smallThunder.lastDamageTime = Time.time;
                    }
                    return;
                }
            }
            else
            {
                if (thunder != null)
                {
                    thunder.DestroyOnEnemyHit();
                }
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
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false;
        animator.SetTrigger("Die");
        EnemyAutoDrop drop = GetComponent<EnemyAutoDrop>();
        if (drop != null)
        {
            drop.TryDrop();
        }
        Destroy(gameObject, 0.18f);
    }
    void OnDrawGizmosSelected()
    {
        if (!firePoint) return;

        Gizmos.color = Color.red;
        foreach (float angle in attackAngles)
        {
            float worldAngle = transform.eulerAngles.z + angle;
            Quaternion rotation = Quaternion.Euler(0, 0, worldAngle);
            Vector2 direction = rotation * Vector2.right;
            Gizmos.DrawLine(firePoint.position, (Vector2)firePoint.position + direction * 2f);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}