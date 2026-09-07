using UnityEngine;
using System.Collections;

public class FlyingShell : MonoBehaviour
{
    public enum State
    {
        Sleep,
        Wake
    }

    public State currentState = State.Sleep;
    public int health = 1;
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float timeBetweenShots = 0.3f;
    public float bulletSpeed = 10f;
    public float sleepBetweenAttacks = 2f;
    public int damage = 2;
    public float damageCooldown = 1.5f;
    private float lastDamageTime;

    public float detectionRadius = 5f;
    public LayerMask playerLayer;
    public Transform player;

    public Animator animator;
    public string wakeTrigger = "Wake";
    public string sleepTrigger = "Sleep";
    public float attackAnimationDelay = 1f;
    private AudioSource audioSource;
    public AudioClip Damage;
    private SpriteRenderer spriteRenderer;

    private bool isAttacking;
    private bool playerInRange;

    [Header("Movement")]
    public float moveSpeed = 2f;
    private int moveDirection = 1;
    public float moveChangeInterval = 2f;

    private Rigidbody2D rb;
    private float moveTimer;
    private Vector2 initialPosition; // 记录出生点位置
    public bool isFrozen = false;

    private readonly float[] attackAngles8Dir = {
        0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f
    };

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();

        StartCoroutine(FindPlayerWithDelay(0.5f));

        initialPosition = transform.position;

        SetAnimationState(currentState);
    }

    IEnumerator FindPlayerWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("延迟后仍未找到标签为 Player 的对象！");
            }
        }
    }


    void Update()
    {
        if (isFrozen)
        {
            return;
        }
        CheckPlayerInRange();

        if (playerInRange && currentState == State.Sleep && !isAttacking)
        {
            ChangeState(State.Wake);
            StartCoroutine(DelayedAttackCycle());
        }
        else if (!playerInRange && currentState == State.Wake)
        {
            ChangeState(State.Sleep);
            StopAllCoroutines();
            isAttacking = false;
        }

        if (currentState == State.Wake)
        {
            MovePattern();
        }
    }

    void MovePattern()
    {
        moveTimer += Time.deltaTime;
        if (moveTimer >= moveChangeInterval)
        {
            moveDirection *= -1;
            moveTimer = 0f;
        }

        Vector2 newPosition = rb.position + Vector2.right * moveDirection * moveSpeed * Time.deltaTime;

        if (Mathf.Abs(newPosition.x - initialPosition.x) <= 5f)
        {
            rb.MovePosition(newPosition);
            spriteRenderer.flipX = moveDirection < 0;
        }
        else
        {
            moveDirection *= -1;
            moveTimer = 0f;
        }
    }

    IEnumerator DelayedAttackCycle()
    {
        yield return new WaitForSeconds(attackAnimationDelay);
        StartCoroutine(AttackCycle());
    }

    void CheckPlayerInRange()
    {
        if (player == null) return;
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        playerInRange = distanceToPlayer <= detectionRadius;
    }

    void ChangeState(State newState)
    {
        if (currentState != newState)
        {
            currentState = newState;
            SetAnimationState(newState);
        }
    }

    void SetAnimationState(State state)
    {
        if (animator == null) return;

        switch (state)
        {
            case State.Wake:
                animator.ResetTrigger(sleepTrigger);
                animator.SetTrigger(wakeTrigger);
                break;
            case State.Sleep:
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

            ChangeState(State.Sleep);
            yield return new WaitForSeconds(sleepBetweenAttacks);

            if (playerInRange)
            {
                ChangeState(State.Wake);
                yield return new WaitForSeconds(attackAnimationDelay);
            }
        }

        ChangeState(State.Sleep);
        isAttacking = false;
    }

    IEnumerator AttackSequence()
    {
        foreach (float angle in attackAngles8Dir)
        {
            FireAtAngle(angle);
        }
        yield return null;
    }

    void FireAtAngle(float angle)
    {
        if (!bulletPrefab || !firePoint) return;

        Quaternion rotation = Quaternion.Euler(0, 0, angle);
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, rotation);
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb) rb.velocity = rotation * Vector2.right * bulletSpeed;
    }

    void OnTriggerEnter2D(Collider2D collision)
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
            if (currentState == State.Wake)
            {
                Bullet bullet = collision.GetComponent<Bullet>();
                TakeDamage(1);
                bullet.DestroyBullet();
            }
            else
            {
                Bullet bullet = collision.GetComponent<Bullet>();
                bullet.Deflect();
            }
        }

        if (collision.CompareTag("ChargeBullet"))
        {
            if (currentState == State.Wake)
            {
                Bullet bullet = collision.GetComponent<Bullet>();
                TakeDamage(1);
            }
            else
            {
                Bullet bullet = collision.GetComponent<Bullet>();
                bullet.Deflect();
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
        if (collision.CompareTag("FireStorm"))
        {
            // 只有在Wake状态下才会受到伤害
            if (currentState == State.Wake)
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
            if (currentState == State.Wake)
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

    void OnTriggerStay2D(Collider2D collision)
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
            if (shockWave != null)
            {
                TakeDamage(1);
                return;
            }
        }

        if (collision.CompareTag("FireStorm"))
        {
            // 只有在Wake状态下才会受到伤害
            if (currentState == State.Wake)
            {
                TakeDamage(1);
            }
            else
            {
                FireStorm fire = collision.GetComponent<FireStorm>();
                if (fire != null) fire.DestroyBullet();
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
        foreach (float angle in attackAngles8Dir)
        {
            Quaternion rotation = Quaternion.Euler(0, 0, angle);
            Vector2 direction = rotation * Vector2.right;
            Gizmos.DrawLine(firePoint.position, (Vector2)firePoint.position + direction * 2f);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position + Vector3.left * 5f, transform.position + Vector3.right * 5f);
    }
}
