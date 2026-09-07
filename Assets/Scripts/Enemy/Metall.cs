using UnityEngine;
using System.Collections;

public class Metall : MonoBehaviour
{
    public enum BlasterState
    {
        Sleep,
        Wake
    }

    [Header("基础参数")]
    public BlasterState currentState = BlasterState.Sleep;
    public int health = 1;
    public int damage = 2;
    public float damageCooldown = 1.5f;
    private float lastDamageTime;

    [Header("攻击相关")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float timeBetweenShots = 0.3f;
    public float bulletSpeed = 10f;
    public float[] attackAngles = { 60f, 30f, -30f, -60f };
    public float sleepBetweenAttacks = 2f;
    public float attackAnimationDelay = 1f;

    [Header("检测与追踪")]
    public float detectionRadius = 5f;
    public LayerMask playerLayer;

    [Header("动画与音效")]
    public Animator animator;
    public string wakeTrigger = "Wake";
    public string sleepTrigger = "Sleep";
    private AudioSource audioSource;
    public AudioClip Damage;
    private SpriteRenderer spriteRenderer;

    [Header("状态变量")]
    private bool isAttacking;
    private bool playerInRange;
    public bool isFrozen = false;

    // ✅ 动态绑定玩家
    private Transform Player
    {
        get
        {
            SpecialLevelManager slm = FindObjectOfType<SpecialLevelManager>();
            if (slm != null && slm.currentPlayer != null)
                return slm.currentPlayer.transform;

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            return playerObj != null ? playerObj.transform : null;
        }
    }

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();
        SetAnimationState(currentState);
    }

    void Update()
    {
        if (isFrozen) return;
        if (Player == null) return;

        CheckPlayerInRange();

        if (playerInRange && currentState == BlasterState.Sleep && !isAttacking)
        {
            ChangeState(BlasterState.Wake);
            StartCoroutine(DelayedAttackCycle());
        }
        else if (!playerInRange && currentState == BlasterState.Wake)
        {
            ChangeState(BlasterState.Sleep);
            StopAllCoroutines();
            isAttacking = false;
        }
    }

    void CheckPlayerInRange()
    {
        if (Player == null) return;
        float distanceToPlayer = Vector2.Distance(transform.position, Player.position);
        playerInRange = distanceToPlayer <= detectionRadius;
    }

    IEnumerator DelayedAttackCycle()
    {
        yield return new WaitForSeconds(attackAnimationDelay);
        StartCoroutine(AttackCycle());
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
                yield return new WaitForSeconds(attackAnimationDelay);
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

        HandleBulletDamage(collision);
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

    void HandleBulletDamage(Collider2D collision)
    {
        string tag = collision.tag;

        switch (tag)
        {
            case "Bullet":
                if (currentState == BlasterState.Wake)
                {
                    TakeDamage(1);
                    Bullet b = collision.GetComponent<Bullet>();
                    if (b != null) b.DestroyBullet();
                }
                else
                {
                    Bullet b = collision.GetComponent<Bullet>();
                    b?.Deflect();
                }
                break;

            case "ChargeBullet":
                TakeDamage(1);
                break;

            case "RollingCutter":

                TakeDamage(1);

                break;

            case "SuperArm":
                TakeDamage(1);
                break;

            case "IceArrow":
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
                break;

            case "HyperBomb":
                TakeDamage(1);
                break;

            case "FireStorm":
                if (currentState == BlasterState.Wake)
                {
                    TakeDamage(1);
                }
                else
                {
                    FireStorm fire = collision.GetComponent<FireStorm>();
                    if (fire != null) fire.DestroyBullet();
                }
                break;

            case "ThunderBeam":
                ThunderBeam thunder = collision.GetComponent<ThunderBeam>();
                SmallThunderBeam smallThunder = collision.GetComponent<SmallThunderBeam>();
                if (currentState == BlasterState.Wake)
                {
                    if (thunder != null)
                    {
                        TakeDamage(2);
                        return;
                    }
                    if (smallThunder != null && Time.time >= smallThunder.lastDamageTime + smallThunder.damageCooldown)
                    {
                        TakeDamage(2);
                        smallThunder.lastDamageTime = Time.time;
                    }
                }
                else
                {
                    if (thunder != null)
                        thunder.DestroyOnEnemyHit();
                }
                break;
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
            drop.TryDrop();

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
