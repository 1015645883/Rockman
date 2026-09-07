using UnityEngine;

public class ThunderPiece : MonoBehaviour
{
    public float speed = 8f;          // 移动速度
    public float lifetime = 1.5f;     // 存活时间

    private Vector2 moveDirection;

    void Start()
    {
        // 根据自身 Z 旋转角度计算方向
        float angle = transform.eulerAngles.z;
        float rad = angle * Mathf.Deg2Rad;
        moveDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;

        // 自动销毁
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.Translate(moveDirection * speed * Time.deltaTime, Space.World);
    }
}
