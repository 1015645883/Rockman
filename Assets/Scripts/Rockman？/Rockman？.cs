using UnityEngine;
using System.Collections;

public class RockmanBossController : MonoBehaviour, IBossController
{
    [Header("基础组件")]
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    public SpriteRenderer overlayRenderer;
    private AudioSource audioSource;
    private PlayerDamageEffect damageEffect;
    private BossHealthSystem healthSystem;
    private Material playerMaterial; // 用于控制Shader的材质实例

    [Header("玩家与检测")]
    public Transform player;
    public Transform groundCheck;
    public LayerMask groundLayer;
    private bool isGrounded;

    [Header("移动点")]
    public Vector2 leftPoint = new Vector2(-8f, -30.2f);
    public Vector2 middlePoint = new Vector2(0f, -30.2f);
    public Vector2 rightPoint = new Vector2(8f, -30.2f);
    private int[] patrolOrder = new int[] { 0, 1, 2 }; // 左 中 右
    private int patrolOrderIndex = 2; // 初始在右边
    private bool forward = false; // false=往左, true=往右
    [Header("攻击")]
    public int damage = 2;
    private float lastDamageTime = 0f;
    private float damageCooldown = 1.5f;
    public GameObject bulletPrefab;
    public GameObject chargeBulletPrefab;
    public Transform firePoint;
    public int walkAttackCount = 3;
    public int jumpAttackCount = 2;

    private bool isChargingAttack = false;

    [Header("Boss属性")]
    public float walkSpeed = 3f;
    public float dashSpeed = 6f;
    public float jumpForce = 10f;
    public int health = 28;

    [Header("受伤与无敌")]
    private bool isHurt = false;
    public bool IsHurt => isHurt;
    public bool isDead = false;
    public float invincibilityTime = 1.5f;
    private float invincibilityTimer;
    public float flashInterval = 0.1f;

    [Header("行为控制")]
    public bool isInDialogue = true;

    [Header("音效与语音")]
    public AudioClip damageSound;
    public AudioClip jumpSound;
    public AudioClip attackSound;
    public AudioClip chargeAttackSound;
    public AudioClip[] jumpVoices;
    public AudioClip[] attackVoices;
    public AudioClip[] hurtVoices;
    public AudioClip[] deadVoices;
    public AudioClip[] DeadVoices => deadVoices;

    [Range(0, 1)] public float voiceProbability = 0.7f;
    [Range(0, 1)] public float hurtVoiceProbability = 0.2f;

    private Vector2[] patrolPoints;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        overlayRenderer.enabled = false;
        healthSystem = GetComponent<BossHealthSystem>();
        playerMaterial = spriteRenderer.material;
        patrolPoints = new Vector2[] { leftPoint, middlePoint, rightPoint };
    }

    private IEnumerator FindPlayer()
    {
        while (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) { player = p.transform; break; }
            yield return null;
        }
    }

    void Update()
    {
        if (!isDead && !isInDialogue)
        {
            FlipFirePoint();
            CheckGround();
        }

        if (isDead) StopAllCoroutines();

    }


    void CheckGround()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapBox(groundCheck.position, new Vector2(0.5f, 1.7f), 0f, groundLayer);
        }
        else
        {
            isGrounded = false;
        }
    }

    public void PlayRandomVoice(AudioClip[] voices)
    {
        if (voices == null || voices.Length == 0 || Random.value > voiceProbability) return;
        AudioClip clip = voices[Random.Range(0, voices.Length)];
        if (clip != null) audioSource.PlayOneShot(clip);
    }

    public void SetDialogueState(bool state)
    {
        isInDialogue = state;
        if (!isInDialogue) StartCoroutine(BehaviorLoop());
    }

    private IEnumerator BehaviorLoop()
    {
        yield return new WaitForSeconds(1.5f);

        while (!isDead)
        {
            Vector2 target = GetNextTarget();
            int moveType = Random.Range(0, 3); // 0=Walk,1=Jump,2=Dash

            // 决定普通攻击或蓄力攻击
            isChargingAttack = Random.value < 0.5f; // 50%概率蓄力

            if (isChargingAttack)
            {
                // 蓄力表现开始
                StartCoroutine(StartChargeVisual(playerMaterial));
            }

            yield return StartCoroutine(MoveToTarget(target, moveType, isChargingAttack));
            yield return new WaitForSeconds(0.3f);
        }
    }


    public IEnumerator ChooseAction()
    {
        yield return new WaitForSeconds(0.5f);
        yield break;
    }

    private Vector2 GetNextTarget()
    {
        if (forward)
        {
            patrolOrderIndex++;
            if (patrolOrderIndex >= patrolOrder.Length)
            {
                patrolOrderIndex = patrolOrder.Length - 2;
                forward = false;
            }
        }
        else
        {
            patrolOrderIndex--;
            if (patrolOrderIndex < 0)
            {
                patrolOrderIndex = 1;
                forward = true;
            }
        }

        return patrolPoints[patrolOrder[patrolOrderIndex]];
    }

    private IEnumerator MoveToTarget(Vector2 target, int moveType, bool chargeAttack)
    {
        animator.SetBool("IsWalking", false);
        animator.SetBool("IsJumping", false);
        animator.SetBool("Dash", false);

        // 设置动画
        switch (moveType)
        {
            case 0: animator.SetBool("IsWalking", true); break;
            case 1:
                animator.SetBool("IsJumping", true);
                if (jumpSound != null) audioSource.PlayOneShot(jumpSound);
                break;
            case 2: animator.SetBool("Dash", true); break;
        }

        if (moveType == 1) // Jump
        {
            PlayRandomVoice(jumpVoices);
            Vector2 startPos = transform.position;
            float g = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
            float jumpHeight = 5f;
            float vy = Mathf.Sqrt(2 * g * jumpHeight);
            float timeUp = vy / g;
            float totalTime = timeUp + Mathf.Sqrt(2 * (startPos.y - target.y + jumpHeight) / g);
            float vx = (target.x - startPos.x) / totalTime;

            spriteRenderer.flipX = (vx > 0);
            rb.velocity = new Vector2(vx, vy);

            float elapsed = 0f;

            // 按百分比设置攻击时刻，比如攻击两次就在 33% 和 66%
            float[] attackPercentages = chargeAttack ? new float[0] : new float[] { 0.10f, 0.90f };
            int attackIndex = 0;

            while (elapsed < totalTime)
            {
                elapsed += Time.deltaTime;

                if (!chargeAttack && attackIndex < attackPercentages.Length)
                {
                    if (elapsed / totalTime >= attackPercentages[attackIndex])
                    {
                        // 空中攻击使用动画，但不切换 ShootingLayer
                        StartCoroutine(PlayShootAnimation(() =>
                        {
                            FireBulletInternal();
                        }, useShootingLayer: false));

                        attackIndex++;
                    }
                }

                yield return null;
            }

            transform.position = target;
            rb.velocity = Vector2.zero;

        }
        else // Walk / Dash
        {
            float speed = (moveType == 0) ? walkSpeed : dashSpeed;
            spriteRenderer.flipX = (target.x - transform.position.x > 0);

            int attackCount = (!chargeAttack && moveType == 0) ? (int)walkAttackCount : 0;

            // 根据攻击次数设置百分比触发点，例如两次攻击 -> 33%, 66%
            float[] attackPercentages = new float[] { 0.2f, 0.5f, 0.8f };


            int attackIndex = 0;
            Vector2 startPos = transform.position;
            float totalDistance = Vector2.Distance(startPos, target);

            while (Vector2.Distance(transform.position, target) > 0.05f && !isDead)
            {
                Vector2 dir = (target - (Vector2)transform.position).normalized;
                rb.velocity = new Vector2(dir.x * speed, rb.velocity.y);

                if (!chargeAttack && attackCount > 0 && attackIndex < attackPercentages.Length)
                {
                    float traveled = Vector2.Distance(startPos, transform.position);
                    float traveledPercent = traveled / totalDistance;

                    if (traveledPercent >= attackPercentages[attackIndex])
                    {
                        // Walk 普通走路，不翻转，直接攻击
                        StartCoroutine(PlayShootAnimation(() =>
                        {
                            FireBulletInternal();
                        }, useShootingLayer: true));

                        attackIndex++;
                    }
                }

                yield return null;
            }

            transform.position = target;
            rb.velocity = Vector2.zero;
        }

        // 翻转检查：左右端点
        FlipAtEndpoints();
        // 蓄力攻击在终点释放一次，不用 ShootingLayer
        animator.SetBool("IsWalking", false);
        animator.SetBool("IsJumping", false);
        animator.SetBool("Dash", false);

        if (chargeAttack)
        {
            yield return new WaitForSeconds(0.3f);
            PlayRandomVoice(attackVoices);
            FireChargeBullet();
            // 蓄力释放后重置 Shader
            if (playerMaterial != null)
                playerMaterial.SetFloat("_ChargeLevel", 0f);
        }
    }

    /// <summary>
    /// 当到达左右端点时翻转角色
    /// </summary>
    private void FlipAtEndpoints()
    {
        int currentPointIndex = patrolOrder[patrolOrderIndex];
        if (currentPointIndex == 0 || currentPointIndex == patrolOrder.Length - 1) // 左右端点
        {
            spriteRenderer.flipX = !spriteRenderer.flipX;
            FlipFirePoint();
        }
    }


    private void FlipFirePoint()
    {
        if (spriteRenderer.flipX) // 面向右
        {
            firePoint.localPosition = new Vector3(Mathf.Abs(firePoint.localPosition.x),
                                                  firePoint.localPosition.y,
                                                  firePoint.localPosition.z);
        }
        else // 面向左
        {
            firePoint.localPosition = new Vector3(-Mathf.Abs(firePoint.localPosition.x),
                                                  firePoint.localPosition.y,
                                                  firePoint.localPosition.z);
        }
    }

    // 原子发射子弹方法
    private void FireBulletInternal()
    {
        PlayRandomVoice(attackVoices);
        if (bulletPrefab != null && firePoint != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
            EnemyBullet bulletScript = bullet.GetComponent<EnemyBullet>();
            if (bulletScript != null)
            {
                float dir = spriteRenderer.flipX ? 1f : -1f;
                bulletScript.SetDirection(new Vector2(dir, 0));
            }
            if (attackSound != null) audioSource.PlayOneShot(attackSound);
        }
    }

    private void FireBullet()
    {
        StartCoroutine(PlayShootAnimation(() =>
        {
            if (bulletPrefab != null && firePoint != null)
            {
                GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
                EnemyBullet bulletScript = bullet.GetComponent<EnemyBullet>();
                if (bulletScript != null)
                {
                    float dir = spriteRenderer.flipX ? 1f : -1f;
                    bulletScript.SetDirection(new Vector2(dir, 0));
                }
                if (attackSound != null) audioSource.PlayOneShot(attackSound);
            }
        }));
    }

    // 更新的 PlayShootAnimation
    private IEnumerator PlayShootAnimation(System.Action onShoot, bool useShootingLayer = false)
    {
        int shootingLayerIndex = animator.GetLayerIndex("ShootingLayer");

        if (useShootingLayer)
            animator.SetLayerWeight(shootingLayerIndex, 1f);

        animator.SetBool("IsShooting", true);

        yield return null;

        onShoot?.Invoke();

        yield return new WaitForSeconds(0.2f);

        animator.SetBool("IsShooting", false);

        if (useShootingLayer)
            animator.SetLayerWeight(shootingLayerIndex, 0f);
    }



    private void FireChargeBullet()
    {
        StartCoroutine(PlayShootAnimation(() =>
        {
            if (chargeBulletPrefab != null && firePoint != null)
            {
                GameObject bullet = Instantiate(chargeBulletPrefab, firePoint.position, Quaternion.identity);
                EnemyChargeBullet bulletScript = bullet.GetComponent<EnemyChargeBullet>();
                if (bulletScript != null)
                {
                    float dir = spriteRenderer.flipX ? 1f : -1f;
                    bulletScript.SetDirection(new Vector2(dir, 0));
                }
                if (chargeAttackSound != null) audioSource.PlayOneShot(chargeAttackSound);
            }
        }));
    }

    // 新增协程：开始蓄力表现（Shader）
    private IEnumerator StartChargeVisual(Material mat)
    {
        if (mat == null) yield break;

        mat.SetFloat("_ChargeLevel", 1.0f); // 开始蓄力
        yield return new WaitForSeconds(1.0f); // 蓄力持续 1 秒
        mat.SetFloat("_ChargeLevel", 2.0f);   // 蓄力完成
                                              // 注意这里不要直接释放攻击，攻击在 MoveToTarget 终点处理
    }



    void TakeDamage(Vector2 enemyPosition)
    {
        if (isHurt) return;
        isHurt = true;
        invincibilityTimer = invincibilityTime;

        if (healthSystem != null && healthSystem.CurrentHealth > 0)
        {
            StartCoroutine(InvincibilityFlash(invincibilityTime));
        }

        if (damageEffect != null) damageEffect.PlayDamageEffects();

        if (damageSound != null) audioSource.PlayOneShot(damageSound);
        if (hurtVoices.Length > 0 && Random.value <= hurtVoiceProbability)
        {
            PlayRandomVoice(hurtVoices);
        }

        StartCoroutine(ResetInvincibility());
    }

    IEnumerator ResetInvincibility()
    {
        yield return new WaitForSeconds(invincibilityTime);
        isHurt = false;
    }

    IEnumerator InvincibilityFlash(float duration)
    {
        float timer = 0f;
        int state = 0;

        while (timer < duration)
        {
            switch (state % 3)
            {
                case 0: spriteRenderer.enabled = true; overlayRenderer.enabled = false; break;
                case 1: spriteRenderer.enabled = false; overlayRenderer.enabled = true; break;
                case 2: spriteRenderer.enabled = false; overlayRenderer.enabled = false; break;
            }
            state++;
            timer += flashInterval;
            yield return new WaitForSeconds(flashInterval);
        }

        spriteRenderer.enabled = true;
        overlayRenderer.enabled = false;
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
        // 处理子弹碰撞
        if (collision.CompareTag("Bullet"))
        {
            // 先获取子弹组件
            Bullet bullet = collision.GetComponent<Bullet>();
            EnemyFireStorm fire = collision.GetComponent<EnemyFireStorm>();

            // 再获取BossHealthSystem组件
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>(); // 注意这里改为从自身获取
            if (bossHealth == null) return; // 如果没有BossHealthSystem则返回

            if (!isHurt)
            {
                // 如果 Boss 没有处于无敌状态，造成伤害并销毁子弹
                bossHealth.TakeDamage(1);
                if (bullet != null)
                {
                    TakeDamage(bullet.transform.position);
                    bullet.DestroyBullet();
                }
                if (fire != null)
                {
                    TakeDamage(fire.transform.position);
                    fire.DestroyBullet();
                }
            }
            else
            {
                // 如果 Boss 处于无敌状态，子弹穿过 Boss
                Debug.Log("Boss 处于无敌状态，子弹穿过");
                // 可以在这里添加子弹穿过效果
            }
        }
        // 处理子弹碰撞
        if (collision.CompareTag("ChargeBullet"))
        {
            // 先获取子弹组件
            Bullet bullet = collision.GetComponent<Bullet>();
            if (bullet == null) return; // 如果没有Bullet组件则直接返回

            // 再获取BossHealthSystem组件
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>(); // 注意这里改为从自身获取
            if (bossHealth == null) return; // 如果没有BossHealthSystem则返回

            if (!isHurt)
            {
                // 如果 Boss 没有处于无敌状态，造成伤害并销毁子弹
                bossHealth.TakeDamage(3);
                TakeDamage(bullet.transform.position);
                bullet.DestroyBullet();
            }
            else
            {
                // 如果 Boss 处于无敌状态，子弹穿过 Boss
                Debug.Log("Boss 处于无敌状态，子弹穿过");
                // 可以在这里添加子弹穿过效果
            }
        }
        // 处理子弹碰撞
        if (collision.CompareTag("RollingCutter"))
        {
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>();
            if (bossHealth == null) return;

            if (isHurt)
            {
                Debug.Log("Boss 处于无敌状态，子弹穿过");
                return;
            }

            // 尝试获取 RollingCutter 组件
            RollingCutter cutter = collision.GetComponent<RollingCutter>();
            if (cutter != null)
            {
                bossHealth.TakeDamage(1); // cutter 的伤害
                TakeDamage(cutter.transform.position);
                return;
            }

            // 尝试获取 CShockWave 组件
            CShockWave shockWave = collision.GetComponent<CShockWave>();
            if (shockWave != null)
            {
                bossHealth.TakeDamage(2); // shockWave 的伤害
                TakeDamage(shockWave.transform.position);
                return;
            }

            Debug.LogWarning("RollingCutter 标签命中但没有匹配的脚本组件");
        }
        if (collision.CompareTag("SuperArm"))
        {
            // 尝试获取四种可能的脚本
            SuperArm1 arm1 = collision.GetComponent<SuperArm1>();
            SuperArm2 arm2 = collision.GetComponent<SuperArm2>();
            SuperArmFoot foot = collision.GetComponent<SuperArmFoot>();
            GStone gstone = collision.GetComponent<GStone>();

            // 获取BossHealthSystem
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>();
            if (bossHealth == null) return;

            Vector3 hitPos = collision.transform.position;

            if (arm1 != null || foot != null)
            {
                // SuperArm1 和 SuperArmFoot 处理同样伤害，受无敌影响
                if (!isHurt)
                {
                    bossHealth.TakeDamage(2);
                    TakeDamage(hitPos);
                }
                else
                {
                    Debug.Log("Boss 处于无敌状态，SuperArm1/SuperArmFoot 攻击无效");
                }
            }
            else if (arm2 != null)
            {
                // SuperArm2 忽略无敌，造成伤害，适合连击
                bossHealth.TakeDamage(2, ignoreInvincibility: true);
                TakeDamage(hitPos);
            }
            else if (gstone != null)
            {
                // GStone 造成较低伤害，受无敌影响
                if (!isHurt)
                {
                    bossHealth.TakeDamage(1);
                    TakeDamage(hitPos);
                }
                else
                {
                    Debug.Log("Boss 处于无敌状态，GStone 攻击无效");
                }
            }
        }

        if (collision.CompareTag("IceArrow"))
        {
            RIceArrow iceArrow = collision.GetComponent<RIceArrow>();
            if (iceArrow == null) return;

            // 再获取BossHealthSystem组件
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>(); // 注意这里改为从自身获取
            if (bossHealth == null) return; // 如果没有BossHealthSystem则返回

            if (!isHurt)
            {
                // 如果 Boss 没有处于无敌状态，造成伤害
                bossHealth.TakeDamage(1);
                TakeDamage(iceArrow.transform.position);
            }
            else
            {
                // 如果 Boss 处于无敌状态，子弹穿过 Boss
                Debug.Log("Boss 处于无敌状态，子弹穿过");
                // 可以在这里添加子弹穿过效果
            }
        }
        if (collision.CompareTag("FireStorm"))
        {
            // 获取 Boss 生命系统
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>();
            if (bossHealth == null) return;

            // 飞行型 FireStorm
            FireStorm fire = collision.GetComponent<FireStorm>();

            // 护盾型 FireStorm
            FireStormShield shield = collision.GetComponent<FireStormShield>();

            // Boss 无敌判定
            if (isHurt)
            {
                Debug.Log("Boss 处于无敌状态，FireStorm 穿过");
                return;
            }

            // 飞行型 FireStorm：造成伤害并销毁
            if (fire != null && shield == null)
            {
                bossHealth.TakeDamage(2);
                TakeDamage(fire.transform.position);
                fire.DestroyBullet();
                return;
            }

            // 护盾型 FireStorm：有攻击冷却，不销毁
            if (shield != null)
            {
                // 检查冷却
                if (Time.time >= shield.lastDamageTime + shield.damageCooldown)
                {
                    bossHealth.TakeDamage(2);
                    TakeDamage(shield.transform.position);
                    shield.lastDamageTime = Time.time;
                }
                return;
            }
        }
        if (collision.CompareTag("HyperBomb"))
        {
            // 先获取子弹组件
            HyperBombExplosion bomb = collision.GetComponent<HyperBombExplosion>();
            if (bomb == null) return; // 如果没有Bullet组件则直接返回

            // 再获取BossHealthSystem组件
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>(); // 注意这里改为从自身获取
            if (bossHealth == null) return; // 如果没有BossHealthSystem则返回

            if (!isHurt)
            {
                // 如果 Boss 没有处于无敌状态，造成伤害并销毁子弹
                bossHealth.TakeDamage(2);
                TakeDamage(bomb.transform.position);
            }
            else
            {
                // 如果 Boss 处于无敌状态，子弹穿过 Boss
                Debug.Log("Boss 处于无敌状态，子弹穿过");
                // 可以在这里添加子弹穿过效果
            }
        }
        if (collision.CompareTag("ThunderBeam"))
        {
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>();
            if (bossHealth == null) return;

            // 飞行型 ThunderBeam
            ThunderBeam thunder = collision.GetComponent<ThunderBeam>();

            // 小Beam
            SmallThunderBeam smallThunder = collision.GetComponent<SmallThunderBeam>();

            // Boss 无敌判定
            if (isHurt)
            {
                Debug.Log("Boss 处于无敌状态，ThunderBeam 穿过");
                return;
            }

            // 飞行型 ThunderBeam：造成伤害并销毁
            if (thunder != null && smallThunder == null)
            {
                bossHealth.TakeDamage(2);
                TakeDamage(thunder.transform.position);
                thunder.DestroyOnEnemyHit();
                return;
            }

            // 小Beam：有伤害冷却，不销毁
            if (smallThunder != null)
            {
                if (Time.time >= smallThunder.lastDamageTime + smallThunder.damageCooldown)
                {
                    bossHealth.TakeDamage(1); // 小Beam伤害可单独设置
                    TakeDamage(smallThunder.transform.position);
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
        // 处理子弹碰撞
        if (collision.CompareTag("Bullet"))
        {
            // 先获取子弹组件
            Bullet bullet = collision.GetComponent<Bullet>();
            EnemyFireStorm fire = collision.GetComponent<EnemyFireStorm>();

            // 再获取BossHealthSystem组件
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>(); // 注意这里改为从自身获取
            if (bossHealth == null) return; // 如果没有BossHealthSystem则返回

            if (!isHurt)
            {
                // 如果 Boss 没有处于无敌状态，造成伤害并销毁子弹
                bossHealth.TakeDamage(1);
                if (bullet != null)
                {
                    TakeDamage(bullet.transform.position);
                    bullet.DestroyBullet();
                }
                if (fire != null)
                {
                    TakeDamage(fire.transform.position);
                    fire.DestroyBullet();
                }
            }
            else
            {
                // 如果 Boss 处于无敌状态，子弹穿过 Boss
                Debug.Log("Boss 处于无敌状态，子弹穿过");
                // 可以在这里添加子弹穿过效果
            }
        }
        // 处理子弹碰撞
        if (collision.CompareTag("ChargeBullet"))
        {
            // 先获取子弹组件
            Bullet bullet = collision.GetComponent<Bullet>();
            if (bullet == null) return; // 如果没有Bullet组件则直接返回

            // 再获取BossHealthSystem组件
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>(); // 注意这里改为从自身获取
            if (bossHealth == null) return; // 如果没有BossHealthSystem则返回

            if (!isHurt)
            {
                // 如果 Boss 没有处于无敌状态，造成伤害并销毁子弹
                bossHealth.TakeDamage(3);
                TakeDamage(bullet.transform.position);
                bullet.DestroyBullet();
            }
            else
            {
                // 如果 Boss 处于无敌状态，子弹穿过 Boss
                Debug.Log("Boss 处于无敌状态，子弹穿过");
                // 可以在这里添加子弹穿过效果
            }
        }
        // 处理子弹碰撞
        if (collision.CompareTag("RollingCutter"))
        {
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>();
            if (bossHealth == null) return;

            if (isHurt)
            {
                Debug.Log("Boss 处于无敌状态，子弹穿过");
                return;
            }

            // 尝试获取 RollingCutter 组件
            RollingCutter cutter = collision.GetComponent<RollingCutter>();
            if (cutter != null)
            {
                bossHealth.TakeDamage(1); // cutter 的伤害
                TakeDamage(cutter.transform.position);
                return;
            }

            // 尝试获取 CShockWave 组件
            CShockWave shockWave = collision.GetComponent<CShockWave>();
            if (shockWave != null)
            {
                bossHealth.TakeDamage(2); // shockWave 的伤害
                TakeDamage(shockWave.transform.position);
                return;
            }

            Debug.LogWarning("RollingCutter 标签命中但没有匹配的脚本组件");
        }
        if (collision.CompareTag("IceArrow"))
        {
            RIceArrow iceArrow = collision.GetComponent<RIceArrow>();
            if (iceArrow == null) return;

            // 再获取BossHealthSystem组件
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>(); // 注意这里改为从自身获取
            if (bossHealth == null) return; // 如果没有BossHealthSystem则返回

            if (!isHurt)
            {
                // 如果 Boss 没有处于无敌状态，造成伤害
                bossHealth.TakeDamage(1);
                TakeDamage(iceArrow.transform.position);
            }
            else
            {
                // 如果 Boss 处于无敌状态，子弹穿过 Boss
                Debug.Log("Boss 处于无敌状态，子弹穿过");
                // 可以在这里添加子弹穿过效果
            }
        }
        if (collision.CompareTag("FireStorm"))
        {
            // 获取 Boss 生命系统
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>();
            if (bossHealth == null) return;

            // 飞行型 FireStorm
            FireStorm fire = collision.GetComponent<FireStorm>();

            // 护盾型 FireStorm
            FireStormShield shield = collision.GetComponent<FireStormShield>();

            // Boss 无敌判定
            if (isHurt)
            {
                Debug.Log("Boss 处于无敌状态，FireStorm 穿过");
                return;
            }

            // 飞行型 FireStorm：造成伤害并销毁
            if (fire != null && shield == null)
            {
                bossHealth.TakeDamage(2);
                TakeDamage(fire.transform.position);
                fire.DestroyBullet();
                return;
            }

            // 护盾型 FireStorm：有攻击冷却，不销毁
            if (shield != null)
            {
                // 检查冷却
                if (Time.time >= shield.lastDamageTime + shield.damageCooldown)
                {
                    bossHealth.TakeDamage(2);
                    TakeDamage(shield.transform.position);
                    shield.lastDamageTime = Time.time;
                }
                return;
            }
        }
        if (collision.CompareTag("HyperBomb"))
        {
            // 先获取子弹组件
            HyperBombExplosion bomb = collision.GetComponent<HyperBombExplosion>();
            if (bomb == null) return; // 如果没有Bullet组件则直接返回

            // 再获取BossHealthSystem组件
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>(); // 注意这里改为从自身获取
            if (bossHealth == null) return; // 如果没有BossHealthSystem则返回

            if (!isHurt)
            {
                // 如果 Boss 没有处于无敌状态，造成伤害并销毁子弹
                bossHealth.TakeDamage(2);
                TakeDamage(bomb.transform.position);
            }
            else
            {
                // 如果 Boss 处于无敌状态，子弹穿过 Boss
                Debug.Log("Boss 处于无敌状态，子弹穿过");
                // 可以在这里添加子弹穿过效果
            }
        }
        if (collision.CompareTag("ThunderBeam"))
        {
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>();
            if (bossHealth == null) return;

            // 飞行型 ThunderBeam
            ThunderBeam thunder = collision.GetComponent<ThunderBeam>();

            // 小Beam
            SmallThunderBeam smallThunder = collision.GetComponent<SmallThunderBeam>();

            // Boss 无敌判定
            if (isHurt)
            {
                Debug.Log("Boss 处于无敌状态，ThunderBeam 穿过");
                return;
            }

            // 飞行型 ThunderBeam：造成伤害并销毁
            if (thunder != null && smallThunder == null)
            {
                bossHealth.TakeDamage(2);
                TakeDamage(thunder.transform.position);
                thunder.DestroyOnEnemyHit();
                return;
            }

            // 小Beam：有伤害冷却，不销毁
            if (smallThunder != null)
            {
                if (Time.time >= smallThunder.lastDamageTime + smallThunder.damageCooldown)
                {
                    bossHealth.TakeDamage(1); // 小Beam伤害可单独设置
                    TakeDamage(smallThunder.transform.position);
                    smallThunder.lastDamageTime = Time.time;
                }
                return;
            }
        }
    }
    public void CheckAndStopCoroutines()
    {
        if (isDead) StopAllCoroutines();
    }
}
