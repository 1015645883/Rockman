using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))] // 确保有AudioSource组件
public class CutmanController : MonoBehaviour, IBossController
{
    [Header("Movement Settings")]
    public int health = 28;
    public float moveSpeed = 2f;
    public float walkTime = 2f;
    public float jumpForce = 7f;
    public float bigJumpForce = 12f;
    public GameObject dustPrefab;          //  Dust 预制体
    public float dustSpawnInterval = 0.2f; //  生成间隔
    private float lastDustTime = 0f;
    [Header("Attack Settings")]
    public GameObject cutterPrefab;
    public GameObject bigCutterPrefab;
    public Transform cutterSpawnPoint;
    public bool hasCutter = true;
    public int damage = 2;
    private float damageCooldown = 1.4f;
    private float lastDamageTime;

    [Header("Audio Settings")]
    public AudioClip throwSound;
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

    [Header("Arena Points")]
    public Transform point1; // 左
    public Transform point2; // 中
    public Transform point3; // 右

    private int currentPointIndex = 3;
    private int direction = -1; // 初始 3 → 2 → 1
    // 组件引用
    private Rigidbody2D rb;
    private Transform player;
    public Animator anim;
    private AudioSource audioSource;
    private SpriteRenderer spriteRenderer;
    public SpriteRenderer overlayRenderer;     // 叠加 sprite renderer (DamageEffect2)
    public float flashInterval = 0.1f;      // 每帧闪烁间隔时间
    private BossHealthSystem healthSystem;
    private PlayerDamageEffect damageEffect;

    // 状态变量
    private bool isGrounded;
    private bool isWalking = false;
    private bool isJumping = false;
    public bool disableSuperJump = false;
    public bool isHurt = false;
    public bool IsHurt => isHurt;
    public bool isDead = false;
    private float invincibilityTime = 1.5f;
    private float invincibilityTimer = 0f;
    public bool isInDialogue = true;

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

        StartCoroutine(DelayedStart(3f));
    }



    private IEnumerator FindPlayerCoroutine()
    {
        while (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.Log("成功找到Player对象");
                yield break;
            }
            Debug.Log("未找到Player，等待下一帧重试");
            yield return null;
        }
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
        hasCutter = true;
        isHurt = false;

        // 开始行为选择循环
        StartCoroutine(BehaviorLoop());
    }

    private void OnEnable()
    {
        StartCoroutine(FindPlayerCoroutine());
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
            {
                isHurt = false;  // 无敌时间结束，恢复正常
            }
        }
        CheckGround();
        if (isGrounded)
        {
            isJumping = false;
            anim.SetBool("IsJumping", false); // 确保落地后动画回到 Idle
        }
        else
        {
            isJumping = true;
            anim.SetBool("IsJumping", true); // 让动画保持跳跃状态
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
        UpdateAnimationLayers();
    }
    void UpdateAnimationLayers()
    {
        if (anim == null) return; // 确保 Animator 存在

        // 获取动画层索引（确保你的 Animator 里有这两个层）
        int baseLayerIndex = anim.GetLayerIndex("Base Layer");
        int noCutterLayerIndex = anim.GetLayerIndex("NoCutter Layer");

        if (hasCutter)
        {
            // 启用 BaseLayer，禁用 NoCutter
            if (baseLayerIndex != -1) anim.SetLayerWeight(baseLayerIndex, 1f);
            if (noCutterLayerIndex != -1) anim.SetLayerWeight(noCutterLayerIndex, 0f);
        }
        else
        {
            // 启用 NoCutter，禁用 BaseLayer
            if (baseLayerIndex != -1) anim.SetLayerWeight(baseLayerIndex, 0f);
            if (noCutterLayerIndex != -1) anim.SetLayerWeight(noCutterLayerIndex, 1f);
        }
    }
    void CheckGround()
    {

        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapBox(groundCheck.position, new Vector2(0.9f, 1.7f), 0f, groundLayer);
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

    void UpdateAnimationStates()
    {
        // 更新行走状态
        bool shouldWalk = Mathf.Abs(rb.velocity.x) > 0.1f && !isJumping;
        if (shouldWalk != isWalking)
        {
            isWalking = shouldWalk;
            anim.SetBool("IsWalking", isWalking);
        }
    }

    public IEnumerator BehaviorLoop()
    {
        if (isInDialogue || isDead) yield break;

        while (health > 0)
        {
            if (isDead) yield break;

            Transform targetPoint = GetNextPoint();

            // ⭐ 移动阶段
            yield return StartCoroutine(MoveToPoint(targetPoint));

            // ⭐ 到达边缘才攻击
            if (currentPointIndex == 1 || currentPointIndex == 3)
            {
                yield return StartCoroutine(AttackFromWall());
            }

            //yield return new WaitForSeconds(0.1f);
        }
    }

    public IEnumerator ChooseAction()
    {
        yield return new WaitForSeconds(0.5f);
        yield break;
    }

    Transform GetNextPoint()
    {
        currentPointIndex += direction;

        if (currentPointIndex <= 1)
        {
            currentPointIndex = 1;
            direction = 1;
        }
        else if (currentPointIndex >= 3)
        {
            currentPointIndex = 3;
            direction = -1;
        }

        switch (currentPointIndex)
        {
            case 1: return point1;
            case 2: return point2;
            case 3: return point3;
        }

        return point2;
    }

    IEnumerator MoveToPoint(Transform target)
    {
        bool fromCenterToEdge = (currentPointIndex == 1 || currentPointIndex == 3) && Mathf.Abs(transform.position.x - point2.position.x) < 0.2f;

        // ⭐ 如果是 2 → 边缘，有概率触发 Skill2
        if (!disableSuperJump && fromCenterToEdge && Random.value < 0.5f)
        {
            yield return StartCoroutine(SuperJumpSkill2(target));
            yield break;
        }

        int mode = Random.Range(0, 3);

        switch (mode)
        {
            case 0:
                yield return StartCoroutine(WalkTo(target));
                break;

            case 1:
                yield return StartCoroutine(DoubleJumpTo(target));
                break;

            case 2:
                yield return StartCoroutine(BigJumpTo(target));
                break;
        }
    }

    IEnumerator WalkTo(Transform target)
    {
        anim.SetBool("IsWalking", true);

        while (Mathf.Abs(rb.position.x - target.position.x) > 0.05f)
        {
            float dir = Mathf.Sign(target.position.x - rb.position.x);

            rb.velocity = new Vector2(dir * moveSpeed, rb.velocity.y);

            spriteRenderer.flipX = dir > 0;

            yield return new WaitForFixedUpdate(); 
        }

        rb.velocity = new Vector2(0, rb.velocity.y);

        // ⭐ 强制对齐（防止微抖）
        rb.position = new Vector2(target.position.x, rb.position.y);

        anim.SetBool("IsWalking", false);
    }

    IEnumerator ArcJump(Vector2 targetPos, float height)
    {
        Vector2 start = transform.position;

        float g = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);

        // 初速度（上升）
        float vy = Mathf.Sqrt(2 * g * height);

        float timeUp = vy / g;

        // 下落时间
        float timeDown = Mathf.Sqrt(2 * (start.y - targetPos.y + height) / g);

        float totalTime = timeUp + timeDown;

        float vx = (targetPos.x - start.x) / totalTime;

        spriteRenderer.flipX = vx > 0;

        rb.velocity = new Vector2(vx, vy);
        anim.SetBool("IsJumping", true);

        float timer = 0f;

        while (timer < totalTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        rb.position = targetPos;
        rb.velocity = Vector2.zero;

        anim.SetBool("IsJumping", false);
    }

    IEnumerator DoubleJumpTo(Transform target)
    {
        Vector2 start = transform.position;
        Vector2 end = target.position;

        float totalDistance = end.x - start.x;

        // ⭐ 每一跳负责一半水平距离
        float halfDistance = totalDistance * 0.5f;

        // ⭐ 第一跳目标点（只改X，不强制Y插值）
        Vector2 firstTarget = new Vector2(start.x + halfDistance, start.y);

        // ⭐ 第二跳目标点（最终点）
        Vector2 secondTarget = end;

        // ⭐ 小跳高度（关键：要“短、快”）
        float smallJumpHeight = 3.0f;
        PlayRandomVoice(jumpVoices);
        // ⭐ 第一跳（完整小抛物线）
        yield return StartCoroutine(ArcJump(firstTarget, smallJumpHeight));

        // ⭐ 落地后立刻第二跳（形成“连续感”）
        PlayRandomVoice(jumpVoices);
        // ⭐ 第二跳（完整小抛物线）
        yield return StartCoroutine(ArcJump(secondTarget, smallJumpHeight));
    }

    IEnumerator BigJumpTo(Transform target)
    {
        PlayRandomVoice(jumpVoices);
        // ⭐ 一个大半圆（高度更高）
        yield return StartCoroutine(ArcJump(target.position, 6f));
    }

    IEnumerator AttackFromWall()
    {
        // ⭐ 如果没有刀，直接跳过（防止异常）
        if (!hasCutter) yield break;

        int mode = Random.Range(0, 2); // 0=模式1，1=模式2

        if (mode == 0)
        {
            // =========================
            // ⭐ Skill1 模式1：三角丢两次
            // =========================

            hasCutter = false;
            // ⭐ 找对面墙点
            Transform targetWall = (currentPointIndex == 1) ? point3 : point1;
            
            // ⭐ 第一次
            yield return StartCoroutine(ThrowBigCutter_Triangle(targetWall.position));
            yield return new WaitForSeconds(0.1f);
            // ⭐ 第二次
            yield return StartCoroutine(ThrowBigCutter_Triangle(targetWall.position));
            yield return new WaitForSeconds(0.1f);
            hasCutter = true;
            anim.Play("Idle", 0, 0f);
        }
        else
        {
            // =========================
            // ⭐ Skill1 模式2：跟随刀
            // =========================

            hasCutter = false;
            disableSuperJump = true;
            // ⭐ 播动画 + 反向
            anim.Play("Skill1", 0, 0f); // Base Layer
            anim.Play("Skill1", 1, 0f); // Layer 1
            yield return new WaitForSeconds(1f);
            PlayRandomVoice(attackVoices);
            spriteRenderer.flipX = !spriteRenderer.flipX;

            // ⭐ 生成刀
            GameObject cutter = Instantiate(bigCutterPrefab, transform.position, Quaternion.identity);

            RollingCutter_Big big = cutter.GetComponent<RollingCutter_Big>();

            if (big != null)
            {
                big.Initialize_Mode2(transform);
            }

            // ⭐ 等动画播完
            yield return new WaitForSeconds(1f);
            anim.Play("Idle", 0, 0f);
            anim.Play("NCIdle", 1, 0f);
            // ⭐ 直接进入移动（注意：不会触发超级跳）
            yield return StartCoroutine(MoveToPoint(point2));
            currentPointIndex = 2;
        }
    }

    IEnumerator ThrowBigCutter_Triangle(Vector2 wallPos)
    {
        anim.Play("Skill1", 0, 0f); // Base Layer
        anim.Play("Skill1", 1, 0f); // Layer 1
        yield return new WaitForSeconds(1f);
        hasCutter = false;
        PlayRandomVoice(attackVoices);
        // ⭐ 生成刀
        GameObject cutter = Instantiate(bigCutterPrefab, transform.position, Quaternion.identity);
        RollingCutter_Big big = cutter.GetComponent<RollingCutter_Big>();

        if (big != null)
        {
            big.Initialize_Mode1(transform, wallPos);
        }
        yield return new WaitForSeconds(1f);


        // ⭐ 等刀飞完
        yield return new WaitForSeconds(2f);
        hasCutter = true;
    }

    IEnumerator SuperJumpSkill2(Transform target)
    {
        Vector2 start = transform.position;

        //  根据左右点修正墙边偏移
        float offsetX = 0f;

        if (target == point1)
            offsetX = -0.5f;
        else if (target == point3)
            offsetX = 0.5f;

        //  目标：墙边上方（带偏移）
        Vector2 topTarget = new Vector2(
            target.position.x + offsetX,
            target.position.y + 8f
        );


        float g = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);

        float height = 8f; // ⭐ 高度建议=目标高度（更精准）

        // ⭐ 只算上升速度
        float vy = Mathf.Sqrt(2 * g * height);

        // ⭐ 只用上升时间（关键！）
        float timeUp = vy / g;

        // ⭐ 水平速度：刚好在上升结束时到达目标X
        float vx = (topTarget.x - start.x) / timeUp;

        spriteRenderer.flipX = vx > 0;

        rb.velocity = new Vector2(vx, vy);
        PlayRandomVoice(jumpVoices);
        anim.SetBool("IsJumping", true);

        float timer = 0f;

        while (timer < timeUp)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        // ⭐ 此时正好到达顶点（不会再下落一段）
        transform.position = topTarget;
        rb.velocity = Vector2.zero;

        anim.SetBool("IsJumping", false);

        // ⭐ 进入 Skill2
        yield return StartCoroutine(Skill2Fall(target));
    }

    IEnumerator Skill2Fall(Transform wallTarget)
    {
        anim.Play("Skill2", 0, 0f);

        float originalGravity = rb.gravityScale;

        // ⭐ 慢慢下落
        rb.gravityScale = originalGravity * 0.1f;

        float timer = 0f;
        float duration = 3f;

        // ⭐ 重置计时（避免第一次不触发）
        lastDustTime = Time.time;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            // ⭐ 持续生成 Dust（核心修复）
            if (Time.time - lastDustTime >= dustSpawnInterval)
            {
                float offsetX = 0f;

                //  判断当前在哪一侧墙
                if (currentPointIndex == 1)
                {
                    // 左墙 → 往左偏
                    offsetX = -0.49f;
                }
                else if (currentPointIndex == 3)
                {
                    // 右墙 → 往右偏
                    offsetX = 0.49f;
                }

                Vector3 spawnPos = transform.position + new Vector3(offsetX, 0f, 0f);

                Instantiate(dustPrefab, spawnPos, Quaternion.identity);

                lastDustTime = Time.time;
            }

            yield return null;
        }

        anim.Play("Jump", 0, 0f);

        // ⭐ 恢复重力
        rb.gravityScale = originalGravity;

        // ⭐ 后续行为
        int next = Random.Range(0, 2);

        if (next == 0)
        {
            yield return StartCoroutine(BigJumpTo(point2));
            currentPointIndex = 2;
        }
        else
        {
            while (!isGrounded)
                yield return null;

            yield return StartCoroutine(MoveToPoint(point2));
            currentPointIndex = 2;
        }
    }

    // ⭐ Skill2动画事件调用（丢滚地剪刀）
    public void ThrowGroundCutter()
    {
        if (cutterPrefab == null) return;

        // ⭐ 判断当前在左墙还是右墙
        float dir = 0f;

        if (currentPointIndex == 1)
        {
            // 左墙 → 往右丢
            dir = 1f;
        }
        else if (currentPointIndex == 3)
        {
            // 右墙 → 往左丢
            dir = -1f;
        }
        else
        {
            // 防呆（理论不会发生）
            dir = transform.localScale.x > 0 ? 1f : -1f;
        }

        // ⭐ 生成位置（建议用一个spawn点）
        PlayRandomVoice(attackVoices);
        Vector3 spawnPos = transform.position;

        if (cutterSpawnPoint != null)
            spawnPos = cutterSpawnPoint.position;

        GameObject cutter = Instantiate(cutterPrefab, spawnPos, Quaternion.identity);

        // ⭐ 初始化滚地剪刀
        RollingCutter_Ground groundCutter = cutter.GetComponent<RollingCutter_Ground>();

        if (groundCutter != null)
        {
            groundCutter.Initialize(dir, transform);
        }
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
                bossHealth.TakeDamage(1); // shockWave 的伤害
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
                    bossHealth.TakeDamage(4);
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
                bossHealth.TakeDamage(4, ignoreInvincibility: true);
                TakeDamage(hitPos);
            }
            else if (gstone != null)
            {
                // GStone 造成较低伤害，受无敌影响
                if (!isHurt)
                {
                    bossHealth.TakeDamage(2);
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
                bossHealth.TakeDamage(1); // shockWave 的伤害
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
                // 如果 Boss 没有处于无敌状态，造成伤害并销毁子弹
                bossHealth.TakeDamage(2);
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
                bossHealth.TakeDamage(3);
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
                    bossHealth.TakeDamage(3);
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
                bossHealth.TakeDamage(1);
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

        if (damageSound != null)
            audioSource.PlayOneShot(damageSound);

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

        // 短暂空闲状态
        if (health > 0 && !isHurt) // 确保Boss还活着且不处于无敌状态
        {
            StartCoroutine(ChooseAction());
        }
    }

    IEnumerator ForceIdle(float idleTime)
    {
        anim.SetBool("IsWalking", false);
        anim.SetBool("IsThrowing", false);
        anim.SetBool("IsJumping", false);

        yield return new WaitForSeconds(idleTime);
    }


    public void ReturnCutter()
    {
        hasCutter = true;
        disableSuperJump = false;
        UpdateAnimationLayers(); // 新增：更新动画层
    }

    public void DisableCutter() => hasCutter = false;

    public void SetDialogueState(bool state)
    {
        isInDialogue = state;
        if (!isInDialogue) StartCoroutine(DelayedStart(1.5f));
    }

    void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(groundCheck.position, new Vector2(0.9f, 1.7f));
        }
    }
}