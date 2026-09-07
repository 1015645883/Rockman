using UnityEngine;
using System.Collections;

public class BombSpecial : MonoBehaviour
{
    [Header("蓄力爆炸特效")]
    public GameObject chargedExplosionPrefab;

    [Header("炸飞力量")]
    public float launchForce = 25f;

    [Header("射击控制")]
    public PlayerShooting playerShooting;

    [Header("动画控制")]
    public Animator animator;

    [Header("落地检测")]
    public PlayerMovement playerMovement; // 用于 isGrounded

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public void ReleaseBombCharge()
    {
        // 生成爆炸风特效
        if (chargedExplosionPrefab != null)
            Instantiate(chargedExplosionPrefab, transform.position, Quaternion.identity);

        // 添加向上冲力
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.AddForce(Vector2.up * launchForce, ForceMode2D.Impulse);

            // 设置动画参数为 BlastJumping
            if (animator != null)
                animator.SetBool("IsBlastJumping", true);

            // 启动落地检测协程
            StartCoroutine(ResetBlastJumping());
        }
    }

    private IEnumerator ResetBlastJumping()
    {
        // 等待玩家落地
        while (playerMovement != null && !playerMovement.isGrounded)
        {
            yield return null;
        }

        // 落地后重置动画参数
        if (animator != null)
            animator.SetBool("IsBlastJumping", false);
    }
}
