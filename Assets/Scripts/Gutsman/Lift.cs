using UnityEngine;

public class LiftController : MonoBehaviour
{
    [Header("移动设置")]
    public Vector2 direction = Vector2.right; // 运动方向
    public float moveDistance = 5f;           // 往返距离
    public float moveSpeed = 2f;              // 移动速度

    [Header("电梯状态控制")]
    public Animator liftAnimator; // 控制失效/生效动画
    public BoxCollider2D platformCollider; // 电梯的物理碰撞器

    private bool isInDisableZone = false;

    private Rigidbody2D rb;
    private Vector3 startPos;
    private Vector3 lastPosition;
    private bool isActivated = false;
    private float activateTime;

    private Transform playerOnLift = null;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        startPos = transform.position;
        lastPosition = startPos;
    }

    void FixedUpdate()
    {
        if (isActivated)
        {
            float elapsed = Time.time - activateTime;
            float offset = Mathf.PingPong(elapsed * moveSpeed, moveDistance);
            Vector3 targetPos = startPos + (Vector3)(direction.normalized * offset);

            Vector3 movement = targetPos - transform.position;
            rb.MovePosition(targetPos);

            if (playerOnLift != null)
            {
                playerOnLift.position += movement;
            }
        }

        lastPosition = transform.position;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y < -0.5f)
                {
                    playerOnLift = collision.collider.transform;

                    if (!isActivated)
                    {
                        isActivated = true;
                        activateTime = Time.time;
                    }

                    break;
                }
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            if (playerOnLift == collision.collider.transform)
            {
                playerOnLift = null;
            }
        }
    }

    public void DisableLift()
    {
        if (!isInDisableZone)
        {
            isInDisableZone = true;

            if (liftAnimator != null)
                liftAnimator.SetTrigger("Disable");

            if (platformCollider != null)
                platformCollider.enabled = false;
        }
    }

    public void EnableLift()
    {
        if (isInDisableZone)
        {
            isInDisableZone = false;

            if (liftAnimator != null)
                liftAnimator.SetTrigger("Enable");

            if (platformCollider != null)
                platformCollider.enabled = true;
        }
    }
    public void ResetLift()
    {
        // 回到初始位置
        transform.position = startPos;
        rb.velocity = Vector2.zero;

        // 重置状态
        isActivated = false;
        activateTime = 0f;
        playerOnLift = null;

        // 恢复碰撞体 & 动画
        if (liftAnimator != null)
        {
            liftAnimator.ResetTrigger("Disable");
            liftAnimator.ResetTrigger("Enable");
            liftAnimator.Play("abled", 0, 0f);
        }

        if (platformCollider != null)
            platformCollider.enabled = true;

        isInDisableZone = false;
    }
}
