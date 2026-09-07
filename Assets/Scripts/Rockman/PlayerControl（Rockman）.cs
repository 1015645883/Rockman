using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))] // 确保有AudioSource组件
public class PlayerMovement : MonoBehaviour, IPlayerMovement
{
    public float moveSpeed = 5f;
    public float minJumpForce = 5f;
    public float jumpForce = 7.6f;
    public float maxJumpTime = 0.3f;
    public float climbSpeed = 3f;
    public float dashDistance = 6f; // 每次冲刺固定距离
    public float dashSpeed = 10f;   // 冲刺速度
    private float dashRemainingDistance = 0f;
    private bool isInvincible = false;
    public float invincibleDuration = 1.5f;
    private bool pendingShoot = false; // 是否有待处理的射击
    private bool pendingChargeRelease = false; // 是否有待处理的蓄力释放
    public Vector2 dashDirection;
    private Vector2 originalColliderSize;
    private Vector2 originalColliderOffset;
    public Transform groundCheck;
    public LayerMask groundLayer;
    public LayerMask iceLayer;
    public LayerMask spikeLayer;
    public Transform firePoint;  // 射击点
    public Transform gfirePoint;  // 石头出生点
    public Transform ladderCheck;
    public Transform ladderCheck2;  // 🟢 新增：用于检测梯子顶部
    public LayerMask LadderLayer;
    public GameObject shockwavePrefab;           // 冲击波预制体
    public Transform shockwaveSpawnPoint;        // 冲击波生成位置
    public string currentShapeKey = "R";

    private HealthSystem healthSystem;
    public Rigidbody2D rb;
    public SpriteRenderer mainRenderer;       // 角色本体 sprite renderer
    public SpriteRenderer overlayRenderer;     // 叠加 sprite renderer (DamageEffect2)
    public float flashInterval = 0.1f;      // 每帧闪烁间隔时间
    public Animator animator;
    private AudioSource audioSource;
    public AudioClip soundEffect1;  // 在类中声明音效变量
    public AudioClip soundEffect2;
    public AudioClip soundEffect3;
    public AudioClip soundEffect4;
    public AnimatorOverrideController defaultController;
    public AnimatorOverrideController cShapeOverrideController;
    public AnimatorOverrideController gShapeOverrideController;
    public AnimatorOverrideController iShapeOverrideController;
    public AnimatorOverrideController bShapeOverrideController;
    public AnimatorOverrideController fShapeOverrideController;
    public AnimatorOverrideController eShapeOverrideController;
    public AnimatorOverrideController topClimbOverride;      // 顶端起身
    private PlayerIceDetector iceDetector;
    private PlayerDamageEffect damageEffect;
    public float currentSpeedX = 0f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip[] jumpVoices;
    [SerializeField] private AudioClip[] hurtVoices;
    [SerializeField] public AudioClip[] deadVoices;
    [Range(0, 1)] public float voiceProbability = 0.7f;
    [Range(0, 1)] public float hurtVoiceProbability = 0.5f;

    public bool isWalking = false;
    public bool isJumping = false;
    public bool isGrounded = false;
    public bool isShooting = false;
    public bool isShocking = false;
    public bool canMove = true;
    private float jumpTimeCounter;
    private bool isJumpingHeld;
    public bool isClimbing = false;
    private float lastClimbY;          // 上一次的Y坐标
    private bool climbFlipState = false; // 当前翻转状态
    public float climbFlipDistance = 1.5f; // 触发翻转的最小Y位移
    public bool isDashing;
    public bool isInWater = false;
    public bool isDead;
    bool nearLadder = false;
    bool nearLadderTop = false;
    private bool lastNearLadderTop = false;
    private float shootTimer = 0f;
    private float shootCooldown = 0.5f;

    BoxCollider2D standingCollider;
    BoxCollider2D climbingCollider;
    // **受伤相关变量**
    public bool isHurt { get; private set; }  // 接口要求的属性

    private PlayerShooting playerShooting;
    public CutSpecial cutSpecial;
    public FireSpecial fireSpecial;
    private GamePauseManager pauseManager;
    public string SelectedChar
    {
        get
        {
            return PlayerPrefs.GetString("SelectedCharacter", "");
        }
    }

    void Start()
    {
        healthSystem = GetComponent<HealthSystem>();
        rb = GetComponent<Rigidbody2D>();
        mainRenderer = GetComponent<SpriteRenderer>();
        overlayRenderer.enabled = false;  // 初始隐藏
        damageEffect = GetComponent<PlayerDamageEffect>();
        animator = GetComponent<Animator>();
        BoxCollider2D[] colliders = GetComponents<BoxCollider2D>();
        audioSource = GetComponent<AudioSource>();
        standingCollider = colliders[0];
        climbingCollider = colliders[1];
        climbingCollider.enabled = false;
        originalColliderSize = standingCollider.size;
        originalColliderOffset = standingCollider.offset;
        iceDetector = GetComponent<PlayerIceDetector>();
        damageEffect = GetComponent<PlayerDamageEffect>();
        playerShooting = GetComponent<PlayerShooting>();
        cutSpecial = GetComponent<CutSpecial>();
        fireSpecial = GetComponent<FireSpecial>();
        pauseManager = FindObjectOfType<GamePauseManager>();
        canMove = true;
        // 🔹 确保 groundCheck 不是 null
        if (groundCheck == null)
        {
            groundCheck = transform.Find("GroundCheck");  // **尝试在子对象中找到 GroundCheck**
            if (groundCheck == null)
            {
                Debug.LogError("GroundCheck is missing! Please assign it in the Inspector.");
            }
        }

        if (ladderCheck == null)
        {
            ladderCheck = transform.Find("LadderCheck"); // 在子对象中查找梯子检测点
            if (ladderCheck == null)
            {
                Debug.LogError("LadderCheck is missing! Please assign it in the Inspector.");
            }
        }
        if (ladderCheck2 == null)
        {
            ladderCheck2 = transform.Find("LadderCheck2"); // 在子对象中查找 LadderCheck2
            if (ladderCheck2 == null)
            {
                Debug.LogError("LadderCheck2 is missing! Please assign it in the Inspector.");
            }
        }

        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.drag = 0;
        Time.fixedDeltaTime = 0.01f;
    }
    public bool IsFacingRight()
    {
        return mainRenderer.flipX;  // 角色朝右时返回 true，朝左返回 false
    }

    void Update()
    {
        CheckGround();
        if (!canMove) return;
        if (pauseManager != null && pauseManager.isPaused)
        {
            return; // 暂停时直接禁止所有输入
        }

        float moveInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        CheckLadder();

        bool nearLadder = ladderCheck != null && Physics2D.OverlapBox(ladderCheck.position, new Vector2(0.05f, 1.9f), 0f, LadderLayer);

        // 🟢 攀爬逻辑
        if (nearLadder && Mathf.Abs(verticalInput) > 0.1f)
        {
            StartClimbing(verticalInput);
        }
        else if (isClimbing && Input.GetKeyDown(KeyCode.K))
        {
            StopClimbing();
        }

        if (isClimbing)
        {
            float vertical = Input.GetAxisRaw("Vertical");
            rb.velocity = new Vector2(rb.velocity.x, vertical != 0 ? vertical * 5f : 0);
            if (!nearLadder) StopClimbing();
            FlipCharacter(moveInput);
            HandleShooting();
            return; // 攀爬状态跳过地面移动逻辑
        }

        // 🔹 特殊处理：Gutsman 出拳逻辑
        bool isGutsman = SelectedChar == "Gutsman";
        GutsmanMelee gutsmanMelee = isGutsman ? GetComponent<GutsmanMelee>() : null;
        bool isPunching = gutsmanMelee != null && gutsmanMelee.IsPunching();

        if (isPunching)
        {
            // 出拳期间禁止左右移动
            rb.velocity = new Vector2(0, rb.velocity.y);

            // 记录玩家的移动输入，用于拳击结束后继续移动
            if (Mathf.Abs(moveInput) > 0.1f)
                gutsmanMelee.queuedMoveInput = true;
        }
        else
        {
            // 💨 正常移动逻辑（包括冰面加速/减速）
            float targetSpeed = moveInput * moveSpeed;
            bool isChangingDirection = (currentSpeedX * targetSpeed < 0);

            if (iceDetector != null && iceDetector.IsOnIce)
            {
                float accelRate = Mathf.Abs(moveInput) > 0.1f ? (isChangingDirection ? 3f : 1.5f) : 0.2f;
                currentSpeedX = Mathf.Lerp(currentSpeedX, targetSpeed, accelRate * Time.deltaTime);
            }
            else
            {
                currentSpeedX = targetSpeed;
            }

            if (cutSpecial != null && cutSpecial.IsWallJumping())
            {
                // 正在墙跳保护时间内，禁止正常移动逻辑
                HandleShooting();
                return;
            }
            rb.velocity = new Vector2(currentSpeedX, rb.velocity.y);

            // 移动中按J键立即停止移动并出拳（Gutsman）
            if (isGutsman && gutsmanMelee != null && Input.GetKeyDown(KeyCode.J) && isGrounded)
            {
                // 只在地面时触发拳击
                rb.velocity = new Vector2(0, rb.velocity.y);
            }

        }

        // ⚡ 原有冲刺逻辑
        if (isGrounded && Input.GetKeyDown(KeyCode.L))
        {
            // 只有当角色不是Gutsman、Fireman和Bombman时才可以滑铲
            if (SelectedChar != "Gutsman" && SelectedChar != "Fireman" && SelectedChar != "Bombman" && !isDashing)
            {
                float horizontalInput = Input.GetAxisRaw("Horizontal");
                StartDash(horizontalInput);
            }
        }

        if (isDashing)
        {
            DashUpdate();
            if (Input.GetKeyDown(KeyCode.K))
            {
                EndDash();
                HandleJump();
            }
            FlipCharacter(dashDirection.x);
        }
        else
        {
            HandleWalkingAnimation(moveInput);
            FlipCharacter(moveInput);
            HandleJump();
        }

        HandleShooting();
        if (!isDashing && (pendingShoot || pendingChargeRelease)) HandleShooting();
    }

    void PlayRandomVoice(AudioClip[] voices)
    {
        if (voices == null || voices.Length == 0 || Random.value > voiceProbability)
            return;

        AudioClip clip = voices[Random.Range(0, voices.Length)];
        if (clip != null)
            audioSource.PlayOneShot(clip);
    }
    // ✅ **检查地面碰撞**
    void CheckGround()
    {
        bool prevGrounded = isGrounded;

        if (groundCheck != null)
        {
            LayerMask combinedMask = groundLayer | iceLayer | spikeLayer;
            isGrounded = Physics2D.OverlapBox(
                groundCheck.position,
                new Vector2(0.9f, 1.7f),
                0f,
                combinedMask
            );
        }
        else
        {
            isGrounded = false;
        }

        // ✅ 只在“下落接触地面”时触发
        if (!prevGrounded && isGrounded && rb.velocity.y <= 0f)
        {
            OnLand();
        }
    }

    void OnLand()
    {
        // 播放落地音效
        if (soundEffect1 != null) // 你可以换成专门的 landingSound
        {
            audioSource.PlayOneShot(soundEffect4);
        }
    }

    // ✅ **检查梯子**
    void CheckLadder()
    {
        // 🟢 检测梯子与顶端逻辑
        if (ladderCheck != null)
        {
            nearLadder = Physics2D.OverlapBox(
                ladderCheck.position,
                new Vector2(0.05f, 1.4f),
                0f,
                LadderLayer
            );
        }
        else nearLadder = false;

        if (ladderCheck2 != null)
        {
            bool hasLadderAbove = Physics2D.OverlapBox(
                ladderCheck2.position,
                new Vector2(0.05f, 0.5f),
                0f,
                LadderLayer
            );
            nearLadderTop = !hasLadderAbove;
        }
        else nearLadderTop = false;

        // 🟠 攀爬中
        if (isClimbing)
        {
            // 🔸特殊攻击控制器检测，保持不打断
            if ((cutSpecial != null && animator.runtimeAnimatorController == cutSpecial.overrideController) ||
                (fireSpecial != null && animator.runtimeAnimatorController == fireSpecial.extinguishOverride))
                return;

            // 🔹1. 在梯子顶端并且正在攻击：保持当前形态控制器（不切换到 topClimb）
            if (nearLadderTop && isShooting)
            {
                string shape = playerShooting.currentShapeKey;
                AnimatorOverrideController target = null;

                switch (shape)
                {
                    case "C": target = cShapeOverrideController; break;
                    case "G": target = gShapeOverrideController; break;
                    case "I": target = iShapeOverrideController; break;
                    case "B": target = bShapeOverrideController; break;
                    case "F": target = fShapeOverrideController; break;
                    case "E": target = eShapeOverrideController; break;
                    default: target = defaultController; break;
                }

                if (animator.runtimeAnimatorController != target && target != null)
                    animator.runtimeAnimatorController = target;

                lastNearLadderTop = nearLadderTop;
                return; // 攻击中保持形态控制器
            }

            // 🔹2. 攻击结束时（刚从 isShooting=true 变 false）且仍在梯子顶端 → 切回 topClimb
            if (nearLadderTop && !isShooting && animator.runtimeAnimatorController != topClimbOverride)
            {
                animator.runtimeAnimatorController = topClimbOverride;
                lastNearLadderTop = nearLadderTop;
                return;
            }

            // 🔹3. 普通攀爬控制 
            if (nearLadderTop && !lastNearLadderTop)
            {
                if (topClimbOverride != null)
                    animator.runtimeAnimatorController = topClimbOverride;
            }
            else if (!nearLadderTop && lastNearLadderTop)
            {
                // 如果存在 playerShooting，说明是玩家；否则是 Boss
                if (playerShooting != null)
                {
                    string shape = playerShooting.currentShapeKey;
                    if (shape == "C" && cShapeOverrideController != null)
                        animator.runtimeAnimatorController = cShapeOverrideController;
                    else if (shape == "G" && gShapeOverrideController != null)
                        animator.runtimeAnimatorController = gShapeOverrideController;
                    else if (shape == "I" && iShapeOverrideController != null)
                        animator.runtimeAnimatorController = iShapeOverrideController;
                    else if (shape == "B" && bShapeOverrideController != null)
                        animator.runtimeAnimatorController = bShapeOverrideController;
                    else if (shape == "F" && fShapeOverrideController != null)
                        animator.runtimeAnimatorController = fShapeOverrideController;
                    else if (shape == "E" && eShapeOverrideController != null)
                        animator.runtimeAnimatorController = eShapeOverrideController;
                    else
                        animator.runtimeAnimatorController = defaultController;
                }
                else
                {
                    // Gutsman 或其他非玩家对象走这里
                    if (defaultController != null)
                        animator.runtimeAnimatorController = defaultController;
                }
            }


            lastNearLadderTop = nearLadderTop;
        }
        else
        {
            // 离开攀爬状态的逻辑保持不变
            if ((cutSpecial != null && animator.runtimeAnimatorController == cutSpecial.overrideController) ||
                (fireSpecial != null && animator.runtimeAnimatorController == fireSpecial.extinguishOverride))
                return;

            if (animator.runtimeAnimatorController != defaultController && playerShooting != null)
            {
                string shape = playerShooting.currentShapeKey;
                if (shape == "C" && cShapeOverrideController != null)
                    animator.runtimeAnimatorController = cShapeOverrideController;
                else if (shape == "G" && gShapeOverrideController != null)
                    animator.runtimeAnimatorController = gShapeOverrideController;
                else if (shape == "I" && iShapeOverrideController != null)
                    animator.runtimeAnimatorController = iShapeOverrideController;
                else if (shape == "B" && bShapeOverrideController != null)
                    animator.runtimeAnimatorController = bShapeOverrideController;
                else if (shape == "F" && fShapeOverrideController != null)
                    animator.runtimeAnimatorController = fShapeOverrideController;
                else if (shape == "E" && eShapeOverrideController != null)
                    animator.runtimeAnimatorController = eShapeOverrideController;
                else
                    animator.runtimeAnimatorController = defaultController;
            }

            lastNearLadderTop = false;
        }
    }



    void StartClimbing(float verticalInput)
    {
        if (cutSpecial != null)
        {
            if (cutSpecial.isWallClinging) return;
        }
            if (!isClimbing) // 只有开始攀爬时播放音效
        {
            audioSource.PlayOneShot(soundEffect3);
            lastClimbY = transform.position.y; // 记录进入攀爬的起始Y坐标
            climbFlipState = false; // 初始翻转状态
        }

        isClimbing = true;

        // 锁定X轴速度（防止左右移动）
        rb.velocity = new Vector2(0f, verticalInput * climbSpeed);

        rb.gravityScale = 0; // 取消重力影响
        standingCollider.enabled = false;
        climbingCollider.enabled = true;
        animator.SetBool("IsClimbing", true);
        animator.SetBool("IsJumping", false);
        animator.SetBool("IsWalking", false);

        HandleClimbFlip();
    }

    void StopClimbing()
    {
        ForceStopClimbing();
        isClimbing = false;
        rb.gravityScale = 2.7f; // 恢复重力
        animator.SetBool("IsClimbing", false);
        climbingCollider.enabled = false;
        standingCollider.enabled = true;
    }

    // 攀爬时翻转Sprite并同步firePoint
    private void HandleClimbFlip()
    {
        if (!isClimbing) return;

        float deltaY = transform.position.y - lastClimbY;

        // 如果Y位移超过阈值，则切换翻转状态
        if (Mathf.Abs(deltaY) >= climbFlipDistance)
        {
            climbFlipState = !climbFlipState;
            mainRenderer.flipX = climbFlipState;

            // 🔹 同步翻转 firePoint
            if (climbFlipState)
            {
                firePoint.localPosition = new Vector3(Mathf.Abs(firePoint.localPosition.x), firePoint.localPosition.y, 0);
                gfirePoint.localPosition = new Vector3(Mathf.Abs(gfirePoint.localPosition.x), gfirePoint.localPosition.y, 0);
            }
            else
            {
                firePoint.localPosition = new Vector3(-Mathf.Abs(firePoint.localPosition.x), firePoint.localPosition.y, 0);
                gfirePoint.localPosition = new Vector3(-Mathf.Abs(gfirePoint.localPosition.x), gfirePoint.localPosition.y, 0);
            }

            lastClimbY = transform.position.y;
        }
    }


    // ✅ **处理角色翻转**
    public void FlipCharacter(float moveInput)
    {
        CutSpecial cutSpecial = GetComponent<CutSpecial>();
        bool isWallClinging = cutSpecial != null && cutSpecial.IsWallClinging();

        if (isWallClinging && cutSpecial != null)
        {
            // 🔹 根据贴墙方向设置 firePoint
            bool wallOnLeft = Physics2D.OverlapCircle(cutSpecial.leftCheck.position, cutSpecial.checkRadius, cutSpecial.groundLayer | cutSpecial.iceLayer);
            bool wallOnRight = Physics2D.OverlapCircle(cutSpecial.rightCheck.position, cutSpecial.checkRadius, cutSpecial.groundLayer | cutSpecial.iceLayer);

            if (wallOnLeft)
            {
                firePoint.localPosition = new Vector3(Mathf.Abs(firePoint.localPosition.x), firePoint.localPosition.y, firePoint.localPosition.z);
            }
            else if (wallOnRight)
            {
                firePoint.localPosition = new Vector3(-Mathf.Abs(firePoint.localPosition.x), firePoint.localPosition.y, firePoint.localPosition.z);
            }

            return; // 滑墙时不做普通翻转
        }
        // ✅ 退出滑墙，修正 firePoint X
        float targetX = mainRenderer.flipX ? Mathf.Abs(firePoint.localPosition.x) : -Mathf.Abs(firePoint.localPosition.x);
        firePoint.localPosition = new Vector3(targetX, firePoint.localPosition.y, firePoint.localPosition.z);

        // 普通移动翻转逻辑
        if (moveInput > 0)
        {
            mainRenderer.flipX = true;
            firePoint.localPosition = new Vector3(Mathf.Abs(firePoint.localPosition.x), firePoint.localPosition.y, firePoint.localPosition.z);
            gfirePoint.localPosition = new Vector3(Mathf.Abs(gfirePoint.localPosition.x), gfirePoint.localPosition.y, gfirePoint.localPosition.z);
        }
        else if (moveInput < 0)
        {
            mainRenderer.flipX = false;
            firePoint.localPosition = new Vector3(-Mathf.Abs(firePoint.localPosition.x), firePoint.localPosition.y, firePoint.localPosition.z);
            gfirePoint.localPosition = new Vector3(-Mathf.Abs(gfirePoint.localPosition.x), gfirePoint.localPosition.y, gfirePoint.localPosition.z);
        }
    }



    // ✅ **处理跳跃**
    void HandleJump()
    {
        // ✅ 地面跳跃 or 水中跳跃
        if (Input.GetKeyDown(KeyCode.K) && (isGrounded || isInWater))
        {
            if (SelectedChar == "Gutsman")
            {
                GutsmanMelee gutsman = GetComponent<GutsmanMelee>();
                if (gutsman != null) gutsman.CancelPunch();
            }
            StartJump();
        }

        // ✅ 长按继续上升（依然保留）
        if (Input.GetKey(KeyCode.K) && isJumpingHeld)
        {
            ContinueJump();
        }

        if (Input.GetKeyUp(KeyCode.K))
        {
            EndJumpEarly();
        }
    }

    void HandleShooting()
    {
        FireSpecial fireSpecial = GetComponent<FireSpecial>();
        bool canShoot = playerShooting != null && playerShooting.CanShoot;

        if (SelectedChar == "Gutsman")
        {
            if (Input.GetKeyDown(KeyCode.J))
            {
                GutsmanMelee melee = GetComponent<GutsmanMelee>();
                if (melee != null) melee.TryMeleeAttack();
            }
            return; // 直接退出，后面都是射击角色逻辑
        }
        if (playerShooting == null || isDashing || isHurt)
        {
            if (Input.GetKeyDown(KeyCode.J)) pendingShoot = true;
            if (Input.GetKeyUp(KeyCode.J)) pendingChargeRelease = true;
            return;
        }

        bool isUpPressed = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);
        // 🔹 先判断是否是火焰人，并且没在蓄力
        bool isCharging = playerShooting != null && playerShooting.isCharging; // 你需要在 PlayerShooting 里有 IsCharging 这样的标识
        if (isUpPressed && SelectedChar == "Fireman" && fireSpecial != null && !isCharging)
        {
            if (Input.GetKeyDown(KeyCode.J) && canShoot)
            {
                fireSpecial.TryFireSpecial();
                shootTimer = Time.time;
            }
            return; // 只有没蓄力时才会拦截到这里
        }

        // 普通射击 / 蓄力开始
        if ((Input.GetKeyDown(KeyCode.J) || pendingShoot) && canShoot)
        {
            pendingShoot = false;

          if (playerShooting.currentShapeKey == "R")
            {
                playerShooting.StartCharge();
            }
            else
            {
                bool bulletFired = playerShooting.TryShoot();
                if (bulletFired)
                {

                    shootTimer = Time.time;
                }
            }
        }


        // 松开J — 蓄力释放
        if (Input.GetKeyUp(KeyCode.J) || pendingChargeRelease)
        {
            pendingChargeRelease = false;
            if (playerShooting.currentShapeKey == "R")
            {
                playerShooting.ReleaseCharge();

                shootTimer = Time.time;
            }
            StartCoroutine(StopShootingAfterDelay(0.3f));
        }
    }



    public bool IsShooting()
    {
        return Input.GetKeyDown(KeyCode.J) || Input.GetKeyUp(KeyCode.J);
    }




    // ✅ **处理跑步动画**
    void HandleWalkingAnimation(float moveInput)
    {
        bool isMoving = Mathf.Abs(moveInput) > 0.1f;
        if (isGrounded && !isShooting)
        {
            if (isMoving)
            {
                if (!isWalking)
                {
                    if (SelectedChar != "Iceman") // ⚡ 只有非 Iceman 才有起步动画
                    {
                        animator.SetTrigger("StartMoving");
                        animator.speed = 2.0f;
                        StartCoroutine(SkipStartAnimation(0.1f));
                    }
                    else
                    {
                        // Iceman：直接进入走路动画
                        animator.SetBool("IsWalking", true);
                    }

                    isWalking = true;
                }
                else
                {
                    animator.SetBool("IsWalking", true);
                }
            }
            else
            {
                StartCoroutine(DelayedStopWalking(0.15f));
            }
        }
    }



    void FixedUpdate()
    {
        bool isMoving = Mathf.Abs(rb.velocity.x) > 0.1f;

        // ✅ **更新跳跃状态**
        if (isGrounded)
        {
            isJumping = false;
            animator.SetBool("IsJumping", false);
        }
        else
        {
            isJumping = true;
            animator.SetBool("IsJumping", true);
            animator.SetBool("IsWalking", false);
        }
        if (SelectedChar != "Gutsman")
        {
            // ✅ **确保 ShootingLayer 立即关闭**
            if (!isMoving || isJumping || isDashing)
            {
                animator.SetLayerWeight(1, 0);
            }
        }

        // ✅ **BaseLayer 控制站立 & 跳跃射击**
        if (isShooting && !isDead)
        {
            shootTimer = Time.time;
            if (isJumping && !isClimbing)
            {
                animator.SetLayerWeight(1, 0);
                /*
                animator.Play("Jumping_Shoot", 0, 0);
                */
            }
            else if (isClimbing)
            {
                animator.SetLayerWeight(1, 0);

                // ✅ Bombman 爬梯特殊处理
                if (SelectedChar == "Bombman" || SelectedChar == "Gutsman")
                {
                    animator.Play("Climb", 0, 0);
                }
                else
                {
                    animator.Play("Climbing_Shoot", 0, 0);
                }
            }
            else if (!isMoving)
            {
                animator.SetLayerWeight(1, 0);
                animator.Play("Idle_Shoot", 0, 0);
            }
        }

        // ✅ **ShootingLayer 只在跑步射击时激活**
        if (SelectedChar != "Gutsman" && isMoving && isGrounded && (Time.time - shootTimer < shootCooldown) && !isHurt && !isDashing && !isDead)
        {
            animator.SetLayerWeight(1, 1);
        }
        else
        {
            animator.SetLayerWeight(1, 0);
        }

        if (isDashing)
        {
            DashUpdate(); // 冲刺位移逻辑放这里，确保稳定
        }
    }

    private IEnumerator StopShootingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isShooting = false;
        animator.SetBool("IsShooting", false);
    }

    private IEnumerator SkipStartAnimation(float delay)
    {
        yield return new WaitForSeconds(delay);
        animator.SetBool("IsWalking", true);
        animator.speed = 1.0f;
    }

    void StartJump()
    {
        rb.velocity = new Vector2(rb.velocity.x, minJumpForce);
        rb.velocity += new Vector2(0, 5f);
        jumpTimeCounter = 0f;
        isJumpingHeld = true;
        isGrounded = false;
        isJumping = true;
        animator.SetBool("IsJumping", true);
        PlayRandomVoice(jumpVoices);
    }

    public void SetShootingState(bool isShooting)
    {
        this.isShooting = isShooting;
        if (animator != null)
        {
            animator.SetBool("IsShooting", isShooting);
        }
    }

    void ContinueJump()
    {
        if (jumpTimeCounter < maxJumpTime)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            jumpTimeCounter += Time.deltaTime;
        }
    }

    void EndJumpEarly()
    {
        if (isJumpingHeld && rb.velocity.y > 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);
        }
        isJumpingHeld = false;
    }




    private IEnumerator DelayedStopWalking(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!isJumping && Mathf.Abs(rb.velocity.x) < 0.1f)
        {
            ResetToIdle();
        }
    }


    private void ResetToIdle()
    {
        if (!isJumping)
        {
            if (SelectedChar != "Iceman")
            {
                animator.ResetTrigger("StartMoving");
                animator.SetBool("IsWalking", false);
                isWalking = false;
            }
            else
            {
                animator.SetBool("IsWalking", false);
                isWalking = false;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy") || other.CompareTag("EnemyWeapon") || other.CompareTag("Gutsman"))
        {
            TakeDamage(other.transform.position);
        }
        if (other.CompareTag("Water"))
        {
            isInWater = true;
        }
    }
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Spike"))
        {
            // 这个脚本挂在玩家身上，自己就是 HealthSystem 所在对象
            GetComponent<HealthSystem>()?.TakeDamage(28);
        }
    }


    void OnTriggerStay2D(Collider2D other)
    {
        
        if (other.CompareTag("Enemy") || other.CompareTag("EnemyWeapon"))
        {
            TakeDamage(other.transform.position);
        }
    }
    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Spike"))
        {
            GetComponent<HealthSystem>()?.TakeDamage(28);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Water"))
        {
            isInWater = false;
        }
    }

    // 受伤处理
    void TakeDamage(Vector2 enemyPosition)
    {
        if (isHurt || isInvincible) return;
        if (SelectedChar == "Gutsman")
        {

            // 播放音效/语音
            audioSource.PlayOneShot(soundEffect2);
            if (hurtVoices.Length > 0 && Random.value <= hurtVoiceProbability)
            {
                PlayRandomVoice(hurtVoices);
            }

            // 播放特效
            if (damageEffect != null)
            {
                damageEffect.PlayDamageEffects();
            }

            // 开始无敌
            if (!isInvincible)
            {
                isInvincible = true;
                StartCoroutine(InvincibilityTimer(invincibleDuration));
                if (healthSystem != null && healthSystem.CurrentHealth > 0)
                {
                    StartCoroutine(InvincibilityFlash(invincibleDuration));
                }
            }

            // 不播放受伤动画，不改变 rb.velocity，不阻断控制
            return;
        }
        // C 形态冲刺中无敌
        if (currentShapeKey == "C" && isDashing)
        {
            return;
        }

        if (isClimbing)
        {
            ForceStopClimbing();
        }

        // 清除蓄力
        if (playerShooting != null)
        {
            playerShooting.CancelCharge();
        }

        isHurt = true;
        isInvincible = true; // 开始无敌
        StartCoroutine(InvincibilityTimer(invincibleDuration)); // 启动计时器
        if (healthSystem != null && healthSystem.CurrentHealth > 0)
        {
            StartCoroutine(InvincibilityFlash(invincibleDuration));
        }

        audioSource.PlayOneShot(soundEffect2);
        if (hurtVoices.Length > 0 && Random.value <= hurtVoiceProbability)
        {
            PlayRandomVoice(hurtVoices);
        }

        animator.SetLayerWeight(animator.GetLayerIndex("ShootingLayer"), 0);
        animator.CrossFade("Hurt", 0f, 0);
        animator.SetBool("IsHurt", true);
        animator.SetTrigger("Hurt");
        if (damageEffect != null)
        {
            damageEffect.PlayDamageEffects();
        }
        Vector2 pushDirection = (transform.position - new Vector3(enemyPosition.x, transform.position.y, transform.position.z)).normalized;
        rb.velocity = new Vector2(pushDirection.x * 3f, -10f);

        bool canShoot = playerShooting != null && playerShooting.CanShoot;

        canMove = false;
        canShoot = false;
        isGrounded = false;
        isJumping = false;
        isClimbing = false;
        isWalking = false;
        isDashing = false;

        animator.SetBool("IsJumping", false);
        //animator.SetBool("IsWalking", false);
        animator.SetBool("Dash", false);

        StartCoroutine(DisableControlForDuration(0.5f));

    }


    private IEnumerator InvincibilityTimer(float duration)
    {
        yield return new WaitForSeconds(duration);
        isInvincible = false;
    }
    IEnumerator InvincibilityFlash(float duration)
    {
        float timer = 0f;
        int state = 0;

        while (timer < duration)
        {
            if (healthSystem.CurrentHealth <= 0)
            {
                // 血量为0，立即取消闪烁，恢复正常状态，退出协程
                mainRenderer.enabled = true;
                overlayRenderer.enabled = false;
                yield break;
            }

            switch (state % 3)
            {
                case 0: // 正常显示
                    mainRenderer.enabled = true;
                    overlayRenderer.enabled = false;
                    break;
                case 1: // 显示受伤叠加
                    mainRenderer.enabled = false;
                    overlayRenderer.enabled = true;
                    break;
                case 2: // 隐藏角色
                    mainRenderer.enabled = false;
                    overlayRenderer.enabled = false;
                    break;
            }

            state++;
            timer += flashInterval;
            yield return new WaitForSeconds(flashInterval);
        }

        // 恢复正常
        mainRenderer.enabled = true;
        overlayRenderer.enabled = false;
    }


    // 新增强制停止攀爬的方法
    private void ForceStopClimbing()
    {
        isClimbing = false;
        rb.gravityScale = 1; // 恢复重力
        animator.SetBool("IsClimbing", false);
        climbingCollider.enabled = false;
        standingCollider.enabled = true;

        // 重置速度，避免保持攀爬时的惯性
        rb.velocity = Vector2.zero;
    }

    // 恢复控制的协程
    IEnumerator DisableControlForDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        // 恢复控制时，更新地面检测
        CheckGround();  // 重新检查角色是否着地

        // 如果在空中，保持跳跃状态
        if (isGrounded)
        {
            isJumping = false;  // 如果已着地，重置跳跃状态
            animator.SetBool("IsJumping", false);
        }
        // 恢复操作
        isJumping = false;
        isClimbing = false;
        isWalking = false;
        isHurt = false;
        // 恢复动画状态
        animator.SetBool("IsHurt", false);
        // 恢复速度（确保受伤时施加的力已经消失）
        rb.velocity = Vector2.zero;
        canMove = true;
    }

    void StartDash(float horizontalInput)
    {
        isDashing = true;
        isShooting = false;
        dashRemainingDistance = dashDistance;

        // 冲刺方向判定
        if (horizontalInput != 0)
        {
            dashDirection = new Vector2(horizontalInput, 0).normalized;
        }
        else
        {
            float facing = mainRenderer.flipX ? 1f : -1f;
            dashDirection = new Vector2(facing, 0);
        }

        animator.SetBool("Dash", true);
        // ↓↓↓ 翻转 shockwaveSpawnPoint 的位置（根据冲刺方向）↓↓↓
        if (shockwaveSpawnPoint != null)
        {
            Vector3 pos = shockwaveSpawnPoint.localPosition;
            pos.x = dashDirection.x > 0 ? Mathf.Abs(pos.x) : -Mathf.Abs(pos.x);
            shockwaveSpawnPoint.localPosition = pos;
        }
        // ↑↑↑ 翻转生成点结束 ↑↑↑
        // ↓↓↓ C形态下尝试消耗4格能量并生成冲击波 ↓↓↓
        if (currentShapeKey == "C")
        {
            if (CEnergyManager.Instance.TryUseEnergy(4))
            {
                if (shockwavePrefab != null && shockwaveSpawnPoint != null)
                {
                    GameObject wave = Instantiate(shockwavePrefab, shockwaveSpawnPoint.position, Quaternion.identity);

                    // 根据冲刺方向设置冲击波朝向
                    float dir = dashDirection.x >= 0 ? -1f : 1f;
                    wave.transform.localScale = new Vector3(dir * Mathf.Abs(wave.transform.localScale.x), wave.transform.localScale.y, 1f);
                }
            }
            else
            {
                Debug.Log("C能量不足，未生成冲击波。");
            }
        }
        // ↑↑↑ C形态冲刺逻辑结束 ↑↑↑

        // 缩小Collider高度
        standingCollider.size = new Vector2(originalColliderSize.x, originalColliderSize.y - 0.08f);
        standingCollider.offset = new Vector2(originalColliderOffset.x, originalColliderOffset.y - 0.04f);
    }


    void DashUpdate()
    {
        // 计算本帧冲刺移动量
        float moveDistance = dashSpeed * Time.deltaTime;
        if (moveDistance > dashRemainingDistance)
        {
            moveDistance = dashRemainingDistance;
        }

        // 移动角色
        Vector2 newPosition = rb.position + dashDirection * moveDistance;
        rb.MovePosition(newPosition);
        dashRemainingDistance -= moveDistance;


        if (dashRemainingDistance <= 0f)
        {
            EndDash();
        }
    }

    void EndDash()
    {
        isDashing = false;
        animator.SetBool("Dash", false);

        // 恢复Collider尺寸和偏移
        standingCollider.size = originalColliderSize;
        standingCollider.offset = originalColliderOffset;


    }
    public void SwitchShape(string shapeKey)
    {
        currentShapeKey = shapeKey;
        
    }

    public void Shock()
    {
        if (SelectedChar == "Gutsman") return;
        if (playerShooting.currentShapeKey == "G") return;
        if (!isGrounded || isShocking) return;

        isShocking = true;

        // 强制打断爬墙/跳跃状态
        if (isClimbing) ForceStopClimbing();
        isJumping = false;
        isDashing = false;
        isWalking = false;
        isShooting = false;
        animator.SetBool("IsJumping", false);
        animator.SetBool("IsWalking", false);
        animator.SetBool("IsShooting", false);
        animator.SetBool("Dash", false);
        animator.ResetTrigger("StartMoving");

        // 清除蓄力 - 使用自身的 playerShooting 引用
        if (playerShooting != null)
        {
            playerShooting.CancelCharge();
        }

        // 播放震击动画
        animator.CrossFade("Shock", 0f, 0);
        animator.SetTrigger("Shock");
        animator.SetBool("IsShocking", true);
        // 禁止所有控制
        canMove = false;
        isClimbing = false;

        // 启动震击硬直协程（无预输入）
        StartCoroutine(ShockCoroutine(1.0f));
    }

    private IEnumerator ShockCoroutine(float duration)
    {
        float backOffset = 0.5f; // 后退距离
        Vector3 backDirection = transform.localScale.x > 0 ? Vector3.left : Vector3.right;
        Vector3 startPos = transform.localPosition;
        Vector3 targetPos = startPos + backDirection * backOffset;

        float elapsed = 0f;
        float moveDuration = 0.8f; // 后退的时间
        float shakeMagnitude = 0.15f; // 抖动幅度
        float shakeSpeed = 40f; // 抖动速度

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // 计算后退平滑插值
            float t = Mathf.Clamp01(elapsed / moveDuration);
            Vector3 movePos = Vector3.Lerp(startPos, targetPos, t);

            // 计算抖动偏移
            float offsetY = Mathf.Sin(elapsed * shakeSpeed) * shakeMagnitude;

            // 合成最终位置
            transform.localPosition = movePos + new Vector3(0, offsetY, 0);

            yield return null;
        }

        // 结束时保持后退位置，不抖动了
        transform.localPosition = targetPos;
        // 震击结束后可视恢复状态
        animator.ResetTrigger("Shock");
        animator.SetBool("IsShocking", false);
        animator.Play("Idle");
        // 恢复控制
        isShocking = false;
        canMove = true;

    }

    void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(groundCheck.position, new Vector2(0.9f, 1.7f));
        }

        if (ladderCheck != null)
        {
            Gizmos.color = nearLadder ? Color.green : Color.blue;
            Vector2 boxSize = new Vector2(0.05f, 1.4f);
            Gizmos.DrawWireCube(ladderCheck.position, boxSize);
        }

        if (ladderCheck2 != null)
        {
            // 🟢 顶端检测框可视化
            Gizmos.color = nearLadderTop ? Color.yellow : Color.cyan;
            Vector2 boxSize = new Vector2(0.05f, 0.5f); // 这里用小一点的范围
            Gizmos.DrawWireCube(ladderCheck2.position, boxSize);
        }
    }


    public void OnStartWalkEnd()
    {
        if (SelectedChar == "Iceman")
            return;
        if (!isJumping && Mathf.Abs(rb.velocity.x) > 0.1f)
        {
            animator.SetBool("IsWalking", true);
        }
        else
        {
            ResetToIdle();
        }
    }



}