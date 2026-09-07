using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Teleport : MonoBehaviour
{
    public Transform target; // 目标位置
    public Vector3 offset = Vector3.zero; // 位置偏移

    private bool playerInRange = false;
    private GameObject playerObj = null;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            playerObj = other.gameObject;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            playerObj = null;
        }
    }

    void Update()
    {
        // 检测玩家在范围内且按下上箭头键
        if (playerInRange && playerObj != null && target != null && Input.GetKeyDown(KeyCode.W))
        {
            // 传送玩家到目标位置
            playerObj.transform.position = target.position + offset;
        }
    }

    // 在 Scene 视图显示连接线
    void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, target.position);
            Gizmos.DrawSphere(target.position + offset, 0.15f);
        }
    }
}