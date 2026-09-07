using UnityEngine;
using System;

public class ThunderBeam : MonoBehaviour
{
    [Header("ThunderBeam 参数")]
    public float speed = 10f;                   // 速度
    public float maxDistance = 10f;             // 最大射程
    public GameObject subThunderBeamPrefab;     // 小Beam预制体
    public GameObject[] lightningEffectPrefabs; // ⚡ 六种不同的雷电特效预制体

    private Vector3 startPosition;
    private Vector2 moveDirection;
    private bool isDeflected = false;

    public Action OnDestroyCallback;  // 销毁回调

    private AudioSource audioSource;
    public AudioClip Dink;

    void Start()
    {
        startPosition = transform.position;

        // ✅ 出生时生成上下两个小Beam
        if (subThunderBeamPrefab != null)
        {
            // 上
            GameObject upBeam = Instantiate(subThunderBeamPrefab, transform.position, Quaternion.identity);
            SmallThunderBeam sub1 = upBeam.GetComponent<SmallThunderBeam>();
            if (sub1 != null) sub1.SetDirection(Vector2.up);

            // 下
            GameObject downBeam = Instantiate(subThunderBeamPrefab, transform.position, Quaternion.identity);
            SmallThunderBeam sub2 = downBeam.GetComponent<SmallThunderBeam>();
            if (sub2 != null) sub2.SetDirection(Vector2.down);
        }
    }

    void Update()
    {
        transform.Translate(moveDirection * speed * Time.deltaTime);

        // 超过最大射程 -> 自然销毁
        if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
        {
            DestroyBullet();
        }
    }

    void FixedUpdate()
    {
        GetComponent<Rigidbody2D>().MovePosition((Vector2)transform.position + moveDirection * speed * Time.fixedDeltaTime);
    }

    public void SetDirection(Vector2 direction)
    {
        moveDirection = direction.normalized;

        // 根据方向翻转精灵
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.flipX = direction.x > 0;
        }
    }

    public void Deflect()
    {
        if (isDeflected) return;
        isDeflected = true;

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = Dink;
            audioSource.playOnAwake = false;
        }
        audioSource.Play();

        moveDirection = Quaternion.Euler(0, 0, 150f) * moveDirection;

        Destroy(GetComponent<Collider2D>());

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = moveDirection * speed;
            rb.gravityScale = 0f;
        }

        GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0.5f);

        Destroy(gameObject, 2f);
    }

    /// <summary>
    /// 自然销毁（超出射程等）
    /// </summary>
    public void DestroyBullet()
    {
        OnDestroyCallback?.Invoke();
        Destroy(gameObject);
    }

    /// <summary>
    /// 击中敌人销毁（会额外生成 6 个不同的雷电特效）
    /// </summary>
    public void DestroyOnEnemyHit()
    {
        // 调用自然销毁回调
        OnDestroyCallback?.Invoke();

        if (lightningEffectPrefabs != null && lightningEffectPrefabs.Length >= 6)
        {
            // 每个特效对应 60 度区间
            for (int i = 0; i < 6; i++)
            {
                float minAngle = i * 60f;
                float maxAngle = (i + 1) * 60f;

                // 在区间内随机选取角度
                float randomAngle = UnityEngine.Random.Range(minAngle, maxAngle);

                Quaternion rot = Quaternion.Euler(0, 0, randomAngle);

                // 实例化特效
                Instantiate(lightningEffectPrefabs[i], transform.position, rot);
            }
        }
        else
        {
            Debug.LogWarning("lightningEffectPrefabs 未正确配置，需要 6 个预制体");
        }

        Destroy(gameObject);
    }

}
