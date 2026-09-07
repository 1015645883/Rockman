using UnityEngine;
using System.Collections;

public class ScrewDriverEnemy : MonoBehaviour
{
    public enum State
    {
        Sleep,
        Wake
    }

    public State currentState = State.Sleep;
    public int health = 5;
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float timeBetweenShots = 0.3f;
    public float[] attackAngles = { 90f, 45f, 0f, -45f, -90f };
    public float sleepBetweenAttacks = 2f;
    public int damage = 2;
    public float damageCooldown = 1.5f;
    private float lastDamageTime;
    public float detectionRadius = 5f;
    public LayerMask playerLayer;

    public Animator animator;
    public string wakeTrigger = "Wake";
    public string sleepTrigger = "Sleep";
    public float attackAnimationDelay = 1f;
    private AudioSource audioSource;
    public AudioClip Damage;
    private SpriteRenderer spriteRenderer;
    private bool isAttacking;
    private bool playerInRange;
    [Tooltip("Wake状态下碰撞体变高的方向：勾选表示向下扩展，取消表示向上扩展")]
    public bool expandColliderDownward = false;
    public bool isFrozen = false;

    // 碰撞体逻辑
    private BoxCollider2D boxCollider;
    private float originalColliderHeight;
    private float originalColliderOffsetY;

    // 动态获取当前受控玩家
    private Transform Player
    {
        get
        {
            SpecialLevelManager slm = FindObjectOfType<SpecialLevelManager>();
            if (slm != null && slm.currentPlayer != null)
                return slm.currentPlayer.transform;  // 注意加 .transform

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            return playerObj != null ? playerObj.transform : null;
        }
    }




    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();

        boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            originalColliderHeight = boxCollider.size.y;
            originalColliderOffsetY = boxCollider.offset.y;
        }

        SetAnimationState(currentState);
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
    }

    IEnumerator DelayedAttackCycle()
    {
        yield return new WaitForSeconds(attackAnimationDelay);
        StartCoroutine(AttackCycle());
    }

    void CheckPlayerInRange()
    {
        if (Player == null)
        {
            playerInRange = false;
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, Player.position);
        playerInRange = distanceToPlayer <= detectionRadius;
    }

    void ChangeState(State newState)
    {
        if (currentState != newState)
        {
            currentState = newState;
            SetAnimationState(newState);
            AdjustColliderHeight(newState);
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

    void AdjustColliderHeight(State state)
    {
        if (boxCollider == null) return;

        Vector2 size = boxCollider.size;
        Vector2 offset = boxCollider.offset;

        switch (state)
        {
            case State.Wake:
                size.y = originalColliderHeight * 2f;
                offset.y = originalColliderOffsetY + (expandColliderDownward ? -originalColliderHeight / 2f : originalColliderHeight / 2f);
                break;
            case State.Sleep:
                size.y = originalColliderHeight;
                offset.y = originalColliderOffsetY;
                break;
        }

        boxCollider.size = size;
        boxCollider.offset = offset;
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
        if (rb) rb.velocity = rotation * Vector2.right * 10f; // bulletSpeed
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (Player != null && collision.transform == Player && Time.time > lastDamageTime + damageCooldown)
        {
            HealthSystem playerHealth = collision.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                lastDamageTime = Time.time;
            }
        }

        HandleDamageFromOtherObjects(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (Player != null && collision.transform == Player && Time.time > lastDamageTime + damageCooldown)
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
            if (shockWave != null)
            {
                TakeDamage(2);
            }
        }
    }

    private void HandleDamageFromOtherObjects(Collider2D collision)
    {
        if (collision.CompareTag("Bullet"))
        {
            Bullet bullet = collision.GetComponent<Bullet>();
            TakeDamage(1);
            bullet?.DestroyBullet();
        }

        if (collision.CompareTag("ChargeBullet"))
        {
            Bullet bullet = collision.GetComponent<Bullet>();
            TakeDamage(5);
        }

        if (collision.CompareTag("SuperArm"))
        {
            TakeDamage(3);
        }

        if (collision.CompareTag("IceArrow"))
        {
            RIceArrow iceArrow = collision.GetComponent<RIceArrow>();
            if (iceArrow != null)
            {
                if (!isFrozen)
                {
                    iceArrow.Freeze(this);
                    TakeDamage(1);
                    iceArrow.DestroyBullet();
                }
                else
                {
                    TakeDamage(2);
                    iceArrow.DestroyBullet();
                }
            }
        }

        if (collision.CompareTag("HyperBomb"))
        {
            TakeDamage(3);
        }

        if (collision.CompareTag("FireStorm"))
        {
            TakeDamage(2);
        }

        if (collision.CompareTag("ThunderBeam"))
        {
            ThunderBeam thunder = collision.GetComponent<ThunderBeam>();
            SmallThunderBeam smallThunder = collision.GetComponent<SmallThunderBeam>();
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
        yield return null;
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
