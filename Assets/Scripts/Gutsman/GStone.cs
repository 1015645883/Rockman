using System;
using System.Collections;
using UnityEngine;

public class GStone : MonoBehaviour
{
    [Header("碎裂相关")]
    public GameObject[] GStonePieces;  // 5种碎片预制体
    public float pieceForce = 5f;       // 碎片飞出力度
    public float disableTime = 0.1f;    // 出生后禁用刚体的时长
    public float destroyDelay = 0.05f;  // 碎裂后销毁延迟（仅用于视觉）
    public float lifeTime = 5f;         // 最大寿命（5秒自动碎裂）
    public Action OnDestroyCallback;    // 子弹销毁时的回调

    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer sr;
    private AudioSource audioSource;
    public bool isBroken = false;

    [Header("音效")]
    public AudioClip breakSound;        // ✅ 碎裂音效
    public float breakSoundVolume = 0.5f; // ✅ 音量（可调）

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        if (rb != null)
        {
            rb.simulated = false; // 刚出生时禁用物理
            StartCoroutine(EnableRigidbodyAfterDelay());
        }

        // 🕒 启动寿命计时器
        StartCoroutine(AutoBreakAfterLifeTime());
    }

    private IEnumerator EnableRigidbodyAfterDelay()
    {
        yield return new WaitForSeconds(disableTime);
        if (rb != null)
        {
            rb.simulated = true; // 启用物理下落
        }
    }

    private IEnumerator AutoBreakAfterLifeTime()
    {
        yield return new WaitForSeconds(lifeTime);
        if (!isBroken)
        {
            BreakStone(); // 到时间自动碎裂
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isBroken) return;

        // 碰到敌人/敌人武器/地面/冰面/刺都会碎裂
        if (other.CompareTag("Enemy") ||
            other.CompareTag("EnemyWeapon") ||
            other.CompareTag("Ground") ||
            other.CompareTag("IceFloor") ||
            other.CompareTag("Spike"))
        {
            BreakStone();
        }
    }

    private void BreakStone()
    {
        if (isBroken) return;
        isBroken = true;

        // ✅ 禁用碰撞和物理
        if (col != null) col.enabled = false;
        if (rb != null) rb.simulated = false;

        // ✅ 播放碎裂音效（使用自身 AudioSource）
        float clipLength = 0f;
        if (audioSource != null && breakSound != null)
        {
            audioSource.PlayOneShot(breakSound, breakSoundVolume);
            clipLength = breakSound.length;
        }

        // ✅ 生成碎片
        if (GStonePieces != null && GStonePieces.Length > 0)
        {
            int index1 = UnityEngine.Random.Range(0, GStonePieces.Length);
            int index2;
            do
            {
                index2 = UnityEngine.Random.Range(0, GStonePieces.Length);
            } while (index2 == index1 && GStonePieces.Length > 1);

            Vector3 leftPos = transform.position + Vector3.left * 0.3f;
            Vector3 rightPos = transform.position + Vector3.right * 0.3f;

            GameObject leftPiece = Instantiate(GStonePieces[index1], leftPos, Quaternion.identity);
            GameObject rightPiece = Instantiate(GStonePieces[index2], rightPos, Quaternion.identity);

            if (leftPiece.TryGetComponent<Rigidbody2D>(out var rbLeft))
                rbLeft.AddForce(new Vector2(-1f, 1f) * pieceForce, ForceMode2D.Impulse);

            if (rightPiece.TryGetComponent<Rigidbody2D>(out var rbRight))
                rbRight.AddForce(new Vector2(1f, 1f) * pieceForce, ForceMode2D.Impulse);

            Destroy(leftPiece, 3f);
            Destroy(rightPiece, 3f);
        }

        // ✅ 隐藏自身视觉
        if (sr != null)
            sr.enabled = false;

        // ✅ 通知 PlayerShooting 子弹销毁
        OnDestroyCallback?.Invoke();

        // ✅ 延迟销毁（音效播放完后）
        float finalDelay = Mathf.Max(clipLength, destroyDelay);
        Destroy(gameObject, finalDelay);
    }
}
