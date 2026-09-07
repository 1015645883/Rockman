using UnityEngine;

public class CutSpecial : MonoBehaviour
{
    [Header("引用")]
    public PlayerMovement playerMovement;   // 角色的移动脚本引用
    public PlayerShooting playerShooting;
    public Animator animator;
    public AnimatorOverrideController overrideController;
    public RuntimeAnimatorController defaultController;

    [Header("Wall Jump 参数")]
    public Transform leftCheck;
    public Transform rightCheck;
    public float checkRadius = 0.1f;
    public LayerMask groundLayer;
    public LayerMask iceLayer;
    public Transform groundCheck;
    public float slideSpeed = 2f;
    public float wallJumpForceX = 10f;
    public float wallJumpForceY = 12f;
    public KeyCode jumpKey = KeyCode.K;

    [Header("Dust 特效")]
    public GameObject dustPrefab;          // ✅ Dust 预制体
    public float dustSpawnInterval = 0.2f; // ✅ 生成间隔
    private float lastDustTime = 0f;

    private Rigidbody2D rb;
    public bool isWallClinging = false;
    private int lastBulletCount = -1;
    private bool isGrounded = false;
    private bool isWallJumping = false;   // ✅ 蹬墙跳状态
    private float wallJumpEndTime = 0f;   // ✅ 保护结束时间

    // 标记当前贴墙方向
    private bool wallOnLeft = false;
    private bool wallOnRight = false;

    private void Start()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
        if (playerShooting == null)
            playerShooting = GetComponent<PlayerShooting>();

        if (animator == null)
            animator = GetComponent<Animator>();

        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (playerShooting == null || animator == null) return;

        HandleScissorAnimation();
        CheckGround();
        HandleWallJump();
        HandleWallDust(); // ✅ 每帧调用 Dust 处理
    }

    private void HandleScissorAnimation()
    {
        int currentBulletCount = playerShooting.currentBulletCount;

        if (currentBulletCount != lastBulletCount)
        {
            if (currentBulletCount == 2)
            {
                if (overrideController != null)
                    animator.runtimeAnimatorController = overrideController;
            }
            else
            {
                if (defaultController != null)
                    animator.runtimeAnimatorController = defaultController;
            }
            lastBulletCount = currentBulletCount;
        }
    }

    private void HandleWallJump()
    {
        if (rb == null) return;
        if (playerMovement.isClimbing) return;
        if (playerMovement!= null && playerMovement.isHurt)
        {
            if (isWallClinging || isWallJumping)
            {
                isWallClinging = false;
                isWallJumping = false;
                animator.SetBool("WallClimbing", false);
            }
            return;
        }

        if (!isGrounded)
        {
            wallOnLeft = Physics2D.OverlapCircle(leftCheck.position, checkRadius, groundLayer | iceLayer);
            wallOnRight = Physics2D.OverlapCircle(rightCheck.position, checkRadius, groundLayer | iceLayer);

            if ((wallOnLeft && Input.GetAxisRaw("Horizontal") < 0) ||
                (wallOnRight && Input.GetAxisRaw("Horizontal") > 0))
            {
                isWallClinging = true;
                animator.SetBool("WallClimbing", true);

                rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -slideSpeed));

                if (Input.GetKeyDown(jumpKey))
                {
                    float jumpDir = wallOnLeft ? 1 : -1;
                    rb.velocity = new Vector2(jumpDir * wallJumpForceX, wallJumpForceY);

                    isWallClinging = false;
                    animator.SetBool("WallClimbing", false);

                    GetComponent<PlayerMovement>()?.FlipCharacter(jumpDir);

                    isWallJumping = true;
                    wallJumpEndTime = Time.time + 0.2f;
                }
            }
            else
            {
                if (isWallClinging)
                {
                    isWallClinging = false;
                    animator.SetBool("WallClimbing", false);
                }
            }
        }
        else
        {
            if (isWallClinging)
            {
                isWallClinging = false;
                animator.SetBool("WallClimbing", false);
            }
        }

        if (isWallJumping && Time.time > wallJumpEndTime)
        {
            isWallJumping = false;
        }
    }

    private void HandleWallDust()
    {
        if (isWallClinging && dustPrefab != null)
        {
            if (Time.time - lastDustTime >= dustSpawnInterval)
            {
                Vector3 spawnPos = transform.position;

                if (wallOnLeft && leftCheck != null)
                    spawnPos = leftCheck.position;
                else if (wallOnRight && rightCheck != null)
                    spawnPos = rightCheck.position;

                Instantiate(dustPrefab, spawnPos, Quaternion.identity);
                lastDustTime = Time.time;
            }
        }
    }

    private void CheckGround()
    {
        if (groundCheck != null)
        {
            LayerMask combinedMask = groundLayer | iceLayer;
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
    }

    public bool IsWallJumping() => isWallJumping;
    public bool IsWallClinging() => isWallClinging;
}
