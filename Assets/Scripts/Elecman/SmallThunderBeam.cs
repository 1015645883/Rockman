using UnityEngine;

public class SmallThunderBeam : MonoBehaviour
{
    [Header("小Beam 参数")]
    public float speed = 8f;            // 小Beam速度（可比大Beam慢）
    public float maxDistance = 6f;      // 小Beam射程
    public float damageCooldown = 1.4f;        // 冷却时间（秒）
    public float lastDamageTime;
    private Vector3 startPosition;
    private Vector2 moveDirection;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        transform.Translate(moveDirection * speed * Time.deltaTime);

        if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
        {
            Destroy(gameObject);
        }
    }

    void FixedUpdate()
    {
        GetComponent<Rigidbody2D>().MovePosition((Vector2)transform.position + moveDirection * speed * Time.fixedDeltaTime);
    }

    public void SetDirection(Vector2 direction)
    {
        moveDirection = direction.normalized;

        // 只在水平方向时翻转
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && Mathf.Abs(direction.x) > 0.1f)
        {
            sr.flipX = direction.x > 0;
        }
    }
    public void DestroyOnEnemyHit()
    {
        Destroy(gameObject);
    }
}
