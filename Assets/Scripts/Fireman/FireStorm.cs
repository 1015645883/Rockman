using UnityEngine;
using System;

public class FireStorm: MonoBehaviour
{
    public float speed = 10f;   // 子弹速度
    public float maxDistance = 10f; // 子弹最大射程
    private Vector3 startPosition;
    public Action OnDestroyCallback;  // 子弹销毁时的回调
    private bool isDeflected = false; // 是否已被弹开
    private AudioSource audioSource;  // 音效组件
    public AudioClip Dink; // 弹开音效

    private Vector2 moveDirection; // ✅ 记录子弹移动方向

    public void SetDirection(Vector2 direction)
    {
        moveDirection = direction.normalized;

        // 判断是否需要翻转：往右（x > 0）则翻转，往左（x <= 0）不翻转
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.flipX = direction.x > 0;
        }
    }


    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        // ✅ 按 moveDirection 方向移动，而不是固定向右
        transform.Translate(moveDirection * speed * Time.deltaTime);

        // 超过最大射程，销毁子弹
        if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
        {
            DestroyBullet();
        }
    }
    void FixedUpdate()
    {
        GetComponent<Rigidbody2D>().MovePosition((Vector2)transform.position + moveDirection * speed * Time.fixedDeltaTime);
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
        // 固定120度偏转方向
        moveDirection = Quaternion.Euler(0, 0, 150f) * moveDirection;

        // 完全移除碰撞器而不是禁用
        Destroy(GetComponent<Collider2D>());

        // 保持直线飞行
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = moveDirection * speed;
            rb.gravityScale = 0f;
        }

        // 视觉效果
        GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0.5f);

        Destroy(gameObject, 2f);
    }

    public void DestroyBullet()
    {
        OnDestroyCallback?.Invoke();  // 通知 PlayerShooting 子弹销毁
        Destroy(gameObject);  // 删除子弹
    }
}