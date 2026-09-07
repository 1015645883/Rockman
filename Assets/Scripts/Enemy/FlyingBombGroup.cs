using UnityEngine;
using System.Collections;

public class FlyingBombGroup : MonoBehaviour
{
    [Header("检测范围")]
    public BoxCollider2D detectionZone;   // 方形检测范围，需要勾选 IsTrigger
    public string playerTag = "Player";   // 玩家Tag

    [Header("敌人生成设置")]
    public GameObject flyingBombPrefab;   // FlyingBomb 敌人预制体
    public float spawnOffsetX = 20f;      // 生成时的X偏移
    public float spawnInterval = 7f;      // 生成间隔（秒）

    [Header("方向设置")]
    public bool bombMoveLeft = true;      // 生成的炸弹是否向左移动（默认true）

    private Transform player;             // 玩家引用
    private Coroutine spawnRoutine;       // 控制协程

    void Reset()
    {
        // 自动加上BoxCollider2D并设置为Trigger
        detectionZone = GetComponent<BoxCollider2D>();
        if (detectionZone == null)
        {
            detectionZone = gameObject.AddComponent<BoxCollider2D>();
        }
        detectionZone.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(playerTag))
        {
            player = collision.transform;

            // 玩家刚进入时立即生成一次
            SpawnFlyingBomb();

            // 开始周期性生成
            if (spawnRoutine == null)
                spawnRoutine = StartCoroutine(SpawnLoop());
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag(playerTag))
        {
            player = null;

            // 停止生成
            if (spawnRoutine != null)
            {
                StopCoroutine(spawnRoutine);
                spawnRoutine = null;
            }
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (player != null)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnFlyingBomb();
        }
    }

    private void SpawnFlyingBomb()
    {
        if (flyingBombPrefab != null && player != null)
        {
            Vector3 spawnPos = new Vector3(player.position.x + spawnOffsetX, player.position.y, 0f);
            GameObject bomb = Instantiate(flyingBombPrefab, spawnPos, Quaternion.identity);

            FlyingBomb bombScript = bomb.GetComponent<FlyingBomb>();
            if (bombScript != null)
            {
                bombScript.moveLeft = bombMoveLeft; // ✅ 正确传递方向
            }

            bomb.name = "FlyingBomb_" + bomb.GetInstanceID();
        }
    }

}
