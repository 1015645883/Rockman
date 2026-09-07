using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))] // 确保有AudioSource组件
public class ElecmanController : MonoBehaviour, IBossController
{
    [Header("Movement Settings")]
    public int health = 28;
    public float moveSpeed = 2f;
    public float walkTime = 4f;
    public float jumpForce = 7f;
    private int moveDirection = 1; // Elecman 当前移动方向（1 表示右，-1 表示左）


    [Header("Attack Settings")]
    public GameObject thunderFrontPrefab;
    public GameObject thunderVerticalPrefab;
    public GameObject thunder3Prefab;
    public Transform attackSpawnPoint;
    public Transform[] blinkPositions; // 7个预设空中位置
    public float normalGravity = 1.8f;
    public float[] gravityScales = { 1f, 1.5f, 2f, 2f }; // 每次出现的重力缩放
    public float blinkDelayMin = 0.5f;
    public float blinkDelayMax = 1f;
    public float poseDuration = 0.3f;

    public int damage = 2;
    private float damageCooldown = 1.4f;
    private float lastDamageTime;
    [Header("Blink Effects")]
    public GameObject blinkDisappearEffect; // 消失位置特效
    public GameObject blinkWarningEffect;   // 闪现预警特效
    [Header("Audio Settings")]
    public AudioClip attackSound;
    public AudioClip damageSound;
    public AudioClip[] jumpVoices;
    public AudioClip[] attackVoices;
    public AudioClip[] hurtVoices;
    public AudioClip[] deadVoices;
    public AudioClip[] DeadVoices => deadVoices;
    [Range(0, 1)] public float voiceProbability = 0.7f;
    [Range(0, 1)] public float hurtVoiceProbability = 0.5f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    public Transform wallCheck; // 用来检测墙壁的 Transform 位置
    public LayerMask wallLayer; // 墙壁所在的 Layer

    // 组件引用
    private Rigidbody2D rb;
    private Transform player;
    private Animator anim;
    private AudioSource audioSource;
    private SpriteRenderer spriteRenderer;
    public SpriteRenderer overlayRenderer;     // 叠加 sprite renderer (DamageEffect2)
    public float flashInterval = 0.1f;      // 每帧闪烁间隔时间
    private BossHealthSystem healthSystem;
    private PlayerShootDetector playerShootDetector;
    private PlayerDamageEffect damageEffect;

    // 状态变量
    private bool isGrounded;
    private bool isWalking = false;
    private bool isJumping = false;
    private bool isAgainstWall = false; // 是否与墙壁接触
    public bool isHurt = false;
    public bool IsHurt => isHurt;
    public bool isDead = false;
    private float invincibilityTime = 1.5f;
    private float invincibilityTimer = 0f;
    private bool isInDialogue = true;

    IEnumerator FindPlayer()
    {
        while (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                yield break;
            }
            yield return null;
        }
    }

    void Start()
    {
        // 获取组件引用
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        overlayRenderer.enabled = false;  // 初始隐藏
        damageEffect = GetComponent<PlayerDamageEffect>();
        healthSystem = GetComponent<BossHealthSystem>();
        playerShootDetector = GetComponent<PlayerShootDetector>();

        // 初始化音频源
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0; // 2D音效

        // 检查必要组件
        if (groundCheck == null)
        {
            groundCheck = transform.Find("GroundCheck");
            if (groundCheck == null)
                Debug.LogError("GroundCheck is missing!");
        }

        StartCoroutine(FindPlayer());
        StartCoroutine(DelayedStart(1f));
    }

    public void CheckAndStopCoroutines()
    {
        if (isDead)
        {
            StopAllCoroutines();
            Debug.Log($"{gameObject.name} 的所有协程已停止（因为 isDead = true）");
        }
    }

    IEnumerator DelayedStart(float delayTime)
    {
        // 初始等待时间
        yield return new WaitForSeconds(delayTime);

        // 确保初始状态正确
        isHurt = false;

        // 开始行为选择循环
        
    }
    private void OnEnable()
    {
        if (healthSystem != null)
            healthSystem.OnBossDeath += OnBossDeath;
    }

    private void OnDisable()
    {
        if (healthSystem != null)
            healthSystem.OnBossDeath -= OnBossDeath;
    }
    private void OnBossDeath()
    {
        isDead = true;
    }
    void Update()
    {
        if (isHurt)
        {
            invincibilityTimer -= Time.deltaTime;
            if (invincibilityTimer <= 0f)
                isHurt = false;
        }

        CheckGround();

        if (isGrounded)
        {
            isJumping = false;
            anim.SetBool("IsJumping", false);
        }
        else
        {
            isJumping = true;
            anim.SetBool("IsJumping", true);
        }

        if (Mathf.Abs(rb.velocity.x) > 0.1f && !isJumping)
        {
            isWalking = true;
            anim.SetBool("IsWalking", true);
        }
        else
        {
            isWalking = false;
            anim.SetBool("IsWalking", false);
        }
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

        if (isGrounded)
        {
            isJumping = false;
            anim.SetBool("IsJumping", false);
        }
    }

    void CheckWall()
    {
        if (wallCheck != null)
        {
            isAgainstWall = Physics2D.OverlapBox(wallCheck.position, new Vector2(1.5f, 0.5f), 0f, wallLayer);
        }
        else
        {
            isAgainstWall = false;
        }
        
    }



    void UpdateAnimationStates()
    {
        bool shouldWalk = Mathf.Abs(rb.velocity.x) > 0.1f && !isJumping;
        if (shouldWalk != isWalking)
        {
            isWalking = shouldWalk;
            anim.SetBool("IsWalking", isWalking);
        }
    }

    public IEnumerator ChooseAction()
    {
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(RunToPlayerAndAttack());
        yield break;
    }

    IEnumerator RunToPlayerAndAttack()
    {
        Debug.Log("进入跑攻击协程");
        if (isInDialogue || isDead) yield break;
        if (!isDead)
        {
            isWalking = true;
            anim.SetBool("IsWalking", true);
            anim.SetBool("IsAttacking", false);

            // 朝玩家方向移动
            moveDirection = player.position.x > transform.position.x ? 1 : -1;

            // 设置角色朝向
            GetComponent<SpriteRenderer>().flipX = moveDirection > 0;

            isAgainstWall = false;

            // 持续奔跑直到接近平台边缘
            while (true)
            {
                // 角色持续朝玩家方向移动
                rb.velocity = new Vector2(moveDirection * moveSpeed, rb.velocity.y);

                // 检测到墙壁，停止奔跑
                CheckWall(); // 调用 CheckWall 方法进行墙壁检测
                if (isAgainstWall) // 如果检测到墙壁
                {
                    rb.velocity = new Vector2(0, rb.velocity.y); // 停止水平移动
                    break; // 结束奔跑，进入后续逻辑
                }

                // 如果在地面上并检测到玩家攻击，立刻跳跃
                if (isGrounded && playerShootDetector != null && playerShootDetector.IsPlayerShooting())
                {
                    Jump(); // 使用已修改的 Jump 函数
                            //break;  // 中断奔跑，交给 Jump 逻辑处理
                }


                yield return null;
            }

            // 停止移动
            rb.velocity = new Vector2(0, rb.velocity.y);
            isWalking = false;
            anim.SetBool("IsWalking", false);

            yield return new WaitForSeconds(0.2f); // 稍作停顿

            // 攻击玩家
            anim.SetBool("IsAttacking", true);
            Attack(); // 你已有的攻击函数
            yield return new WaitForSeconds(0.5f); // 攻击持续时间（建议你声明）
            yield return StartCoroutine(BlinkAttackRoutine());
        }
    }

    IEnumerator BlinkAttackRoutine()
    {
        Debug.Log("进入闪现协程");
        if (isInDialogue || isDead) yield break;

        // 消失时生成消失特效
        if (blinkDisappearEffect != null)
            Instantiate(blinkDisappearEffect, transform.position, Quaternion.identity);

        // 消失（移出场地）
        transform.position = new Vector3(999f, 999f, 0); // 可换为不可触碰区域
        rb.gravityScale = 0;
        rb.velocity = Vector2.zero;

        for (int i = 0; i < 4; i++)
        {
            // 等待随机时间
            float waitTime = Random.Range(blinkDelayMin, blinkDelayMax) - 0.4f; // 提前 0.4 秒生成预警
            yield return new WaitForSeconds(waitTime);

            // 随机选取一个位置
            Transform targetPos = blinkPositions[Random.Range(0, blinkPositions.Length)];

            // 生成闪现预警效果
            if (blinkWarningEffect != null)
                Instantiate(blinkWarningEffect, targetPos.position, Quaternion.identity);

            // 等待 0.4 秒再真正闪现
            yield return new WaitForSeconds(0.5f);

            transform.position = targetPos.position;

            PlayRandomVoice(jumpVoices);

            // 设置重力并下落
            rb.gravityScale = gravityScales[i];
            rb.velocity = Vector2.zero; // 重置速度

            // 先确保离开地面
            yield return new WaitUntil(() => !isGrounded);

            // 再等落地
            yield return new WaitUntil(() => isGrounded);

            if (i < 3)
            {
                ReleaseThunder3();
            }
        }

        // 最后一次落地后的攻击
        rb.gravityScale = normalGravity;
        anim.SetBool("IsAttacking", true);
        Attack();
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(RunToPlayerAndAttack());
    }

    public void ReleaseThunder3()
    {
        float spawnY = 322f;
        float spawnX = transform.position.x;

        Vector2 spawnPos = new Vector2(spawnX, spawnY);

        // 左
        GameObject left = Instantiate(thunder3Prefab, spawnPos, Quaternion.identity);
        left.GetComponent<Thunder3>().Initialize(-1);

        // 右
        GameObject right = Instantiate(thunder3Prefab, spawnPos, Quaternion.identity);
        right.GetComponent<Thunder3>().Initialize(1);
    }

    IEnumerator Walk()
    {
        isWalking = true;
        anim.SetBool("IsWalking", true);
        anim.SetBool("IsAttacking", false);

        // 随机决定移动方向（左 -1，右 +1）
        moveDirection = Random.Range(0, 2) == 0 ? -1 : 1;
        float endTime = Time.time + walkTime;

        // 设置朝向
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.flipX = moveDirection > 0;

        // 持续移动直到时间结束
        while (Time.time < endTime)
        {
            rb.velocity = new Vector2(moveDirection * moveSpeed, rb.velocity.y);
            yield return null;
        }

        // 停止移动
        rb.velocity = new Vector2(0, rb.velocity.y);
        isWalking = false;
        anim.SetBool("IsWalking", false);
    }


    void Attack()
    {
        FacePlayer();
        anim.SetBool("IsWalking", false);
        anim.SetBool("IsJumping", false);
        anim.SetBool("IsAttacking", true);

        // 播放投掷音效
        if (attackSound != null)
            audioSource.PlayOneShot(attackSound);

        // 播放攻击语音
        PlayRandomVoice(attackVoices);

        // 延迟 0.25 秒后生成雷电
        StartCoroutine(DelayedAttack());
    }

    IEnumerator DelayedAttack()
    {
        yield return new WaitForSeconds(0.25f);

        // 获取玩家位置
        Vector2 playerPosition = player.position;

        // 判断攻击方向（Boss 面向玩家的方向）
        Vector2 forward = playerPosition.x > transform.position.x ? Vector2.right : Vector2.left;

        // 生成 3 个雷电
        SpawnThunder1(thunderFrontPrefab, forward);        // 水平向前 Thunder1
        SpawnThunder2(thunderVerticalPrefab, Vector2.up);   // 垂直向上 Thunder2
        SpawnThunder2(thunderVerticalPrefab, Vector2.down); // 垂直向下 Thunder2

        StartCoroutine(ResetAttackAnimation());
    }

    private void SpawnThunder1(GameObject prefab, Vector2 direction)
    {
        if (prefab == null) return;

        GameObject thunder = Instantiate(prefab, attackSpawnPoint.position, Quaternion.identity);
        Thunder1 thunderScript = thunder.GetComponent<Thunder1>();
        if (thunderScript != null)
        {
            thunderScript.Initialize(direction);
        }
    }

    private void SpawnThunder2(GameObject prefab, Vector2 direction)
    {
        if (prefab == null) return;

        GameObject thunder = Instantiate(prefab, attackSpawnPoint.position, Quaternion.identity);
        Thunder2 thunderScript = thunder.GetComponent<Thunder2>();
        if (thunderScript != null)
        {
            thunderScript.Initialize(direction);
        }
    }

    IEnumerator ResetAttackAnimation()
    {
        yield return new WaitForSeconds(0.2f); // 等待攻击动画完成
        anim.SetBool("IsAttacking", false);

        yield return StartCoroutine(ForceIdle(0.2f)); // 额外空闲一点时间
    }



    public void Jump(float jumpHeight = -1f)
    {
        if (isGrounded)
        {
            if (jumpHeight <= 0)
            {
                jumpHeight = jumpForce;
            }

            // 使用当前奔跑方向（moveDirection 必须由奔跑逻辑设置为 -1 或 1）
            float jumpDirection = moveDirection;
            float jumpSpeed = moveSpeed * 1.5f;

            rb.velocity = new Vector2(jumpSpeed * jumpDirection, jumpHeight);

            isJumping = true;
            anim.SetBool("IsWalking", false);
            anim.SetBool("IsAttacking", false);
            anim.SetBool("IsJumping", true);

            PlayRandomVoice(jumpVoices);
            StartCoroutine(ResetJumpAnimation());

            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.flipX = jumpDirection > 0;
        }
    }



    IEnumerator ResetJumpAnimation()
    {
        // 等待直到 Boss 碰到地面
        while (!isGrounded)
        {
            yield return null; // 等待下一帧
        }

        anim.SetBool("IsJumping", false); // 回到 Idle 动画
    }


    void PlayRandomVoice(AudioClip[] voices)
    {
        if (voices == null || voices.Length == 0 || Random.value > voiceProbability)
            return;

        AudioClip clip = voices[Random.Range(0, voices.Length)];
        if (clip != null)
            audioSource.PlayOneShot(clip);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null) return; // 确保碰撞体存在

        // 处理玩家碰撞
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
            if (bullet == null) return; // 如果没有Bullet组件则直接返回

            // 再获取BossHealthSystem组件
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>(); // 注意这里改为从自身获取
            if (bossHealth == null) return; // 如果没有BossHealthSystem则返回

            if (!isHurt)
            {
                // 如果 Boss 没有处于无敌状态，造成伤害并销毁子弹
                bossHealth.TakeDamage(1);
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
                bossHealth.TakeDamage(2);
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
                bossHealth.TakeDamage(4); // cutter 的伤害
                TakeDamage(cutter.transform.position);
                return;
            }

            // 尝试获取 CShockWave 组件
            CShockWave shockWave = collision.GetComponent<CShockWave>();
            if (shockWave != null)
            {
                bossHealth.TakeDamage(6); // shockWave 的伤害
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
                    bossHealth.TakeDamage(3);
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
                bossHealth.TakeDamage(3, ignoreInvincibility: true);
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
        // 处理子弹碰撞
        if (collision.CompareTag("Bullet"))
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
                bossHealth.TakeDamage(1);
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
                bossHealth.TakeDamage(2);
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
                bossHealth.TakeDamage(4); // cutter 的伤害
                TakeDamage(cutter.transform.position);
                return;
            }

            // 尝试获取 CShockWave 组件
            CShockWave shockWave = collision.GetComponent<CShockWave>();
            if (shockWave != null)
            {
                bossHealth.TakeDamage(6); // shockWave 的伤害
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

    void TakeDamage(Vector2 enemyPosition)
    {
        if (isHurt) return;

        isHurt = true;
        invincibilityTimer = invincibilityTime;
        if (healthSystem != null && healthSystem.CurrentHealth > 0)
        {
            StartCoroutine(InvincibilityFlash(invincibilityTime));
        }
        if (damageEffect != null)
        {
            damageEffect.PlayDamageEffects();
        }
        // 播放受伤音效和语音
        if (damageSound != null)
            audioSource.PlayOneShot(damageSound);
        if (hurtVoices.Length > 0 && Random.value <= hurtVoiceProbability)
        {
            PlayRandomVoice(hurtVoices);
        }

        // 重置状态
        isJumping = false;
        isWalking = false;
        isGrounded = true;
        anim.SetBool("IsJumping", false);
        anim.SetBool("IsWalking", false);

        StartCoroutine(ResetInvincibility());
        StartCoroutine(DisableControlForDuration(0.5f));
    }

    IEnumerator ResetInvincibility()
    {
        yield return new WaitForSeconds(invincibilityTime);
        isHurt = false;
        anim.SetBool("IsHurt", false);
    }

    IEnumerator InvincibilityFlash(float duration)
    {
        float timer = 0f;
        int state = 0;

        while (timer < duration)
        {
            switch (state % 3)
            {
                case 0: // 正常显示
                    spriteRenderer.enabled = true;
                    overlayRenderer.enabled = false;
                    break;
                case 1: // 显示受伤叠加
                    spriteRenderer.enabled = false;
                    overlayRenderer.enabled = true;
                    break;
                case 2: // 隐藏角色
                    spriteRenderer.enabled = false;
                    overlayRenderer.enabled = false;
                    break;
            }

            state++;
            timer += flashInterval;
            yield return new WaitForSeconds(flashInterval);
        }

        // 恢复正常
        spriteRenderer.enabled = true;
        overlayRenderer.enabled = false;
    }

    IEnumerator DisableControlForDuration(float duration)
    {
        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(duration);

        // 恢复操作
        isJumping = false;
        isWalking = false;

        // 恢复动画状态
        anim.ResetTrigger("Hurt");
        anim.SetBool("IsHurt", false);


    }

    IEnumerator ForceIdle(float idleTime)
    {
        anim.SetBool("IsWalking", false);
        anim.SetBool("IsAttacking", false);
        anim.SetBool("IsJumping", false);

        yield return new WaitForSeconds(idleTime);
    }

    void FacePlayer()
    {
        if (player != null)
            spriteRenderer.flipX = player.position.x > transform.position.x;
    }


    public void SetDialogueState(bool state)
    {
        isInDialogue = state;
        if (state)
        {
            isInDialogue = true;

        }
        else
        {
            isInDialogue = false;
            // 对话结束后恢复行为
        }
    }

    void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.blue; // 设置颜色为蓝色
            Gizmos.DrawWireCube(groundCheck.position, new Vector2(0.5f, 1.7f)); // 可视化的框体
        }
        if (wallCheck != null)
        {
            Gizmos.color = Color.red; // 设置颜色为蓝色
            Gizmos.DrawWireCube(groundCheck.position, new Vector2(1.5f, 0.5f)); // 可视化的框体
        }
    }

}