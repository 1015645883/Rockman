using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class FootHelder : MonoBehaviour
{
    [Header("飞行轨迹设置")]
    public float radius = 2f;           // 圆形轨迹半径
    public float speed = 1f;            // 飞行角速度（弧度/秒）
    public Vector2 centerOffset = Vector2.zero; // 圆心相对自身初始位置的偏移

    private Vector2 centerPos;          // 圆心位置
    private float angle = 0f;           // 当前角度
    private SpriteRenderer spriteRenderer;

    private Transform playerOnFoot = null; // 站在FootHelder上的玩家

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        Vector3 pos = transform.position;
        centerPos = new Vector2(pos.x, pos.y) + centerOffset;
    }

    private void Update()
    {
        Vector3 oldPos = transform.position; // 记录旧位置

        // 更新角度
        angle += speed * Time.deltaTime;

        // 计算圆周位置
        float x = centerPos.x + Mathf.Cos(angle) * radius;
        float y = centerPos.y + Mathf.Sin(angle) * radius;

        Vector2 newPos = new Vector2(x, y);

        // 根据水平移动方向翻转Sprite
        if (newPos.x > transform.position.x)
            spriteRenderer.flipX = false;
        else if (newPos.x < transform.position.x)
            spriteRenderer.flipX = true;

        // 移动FootHelder
        transform.position = new Vector3(newPos.x, newPos.y, transform.position.z);

        // 带动玩家随动
        if (playerOnFoot != null)
        {
            Vector3 movement = transform.position - oldPos;
            playerOnFoot.position += movement;
        }
    }

    // 玩家踩上FootHelder（放宽判定）
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                // 只要玩家在FootHelder的上方或者接近上方
                if (contact.point.y > transform.position.y)
                {
                    playerOnFoot = collision.collider.transform;
                    break;
                }
            }
        }
    }


    // 玩家离开FootHelder
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            if (playerOnFoot == collision.collider.transform)
            {
                playerOnFoot = null;
            }
        }
    }
}
