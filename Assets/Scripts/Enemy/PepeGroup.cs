using UnityEngine;
using System.Collections;

public class PepeGroup : MonoBehaviour
{
    [Header("检测范围")]
    public BoxCollider2D detectionZone;   // 方形检测范围，需要勾选 IsTrigger
    public string playerTag = "Player";   // 玩家Tag

    [Header("敌人生成设置")]
    public GameObject pepePrefab;         // Pepe 敌人预制体
    public float spawnOffsetX = 20f;      // 生成时的X偏移
    public float spawnInterval = 7f;      // 生成间隔（秒）

    [Header("方向设置")]
    public bool pepeMoveLeft = true;      // 生成的Pepe是否向左移动（默认true）

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
            SpawnPepe();

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
            SpawnPepe();
        }
    }

    private void SpawnPepe()
    {
        if (pepePrefab != null && player != null)
        {
            Vector3 spawnPos = new Vector3(player.position.x + spawnOffsetX, player.position.y, 0f);
            GameObject pepe = Instantiate(pepePrefab, spawnPos, Quaternion.identity);

            // 设置方向（需要Pepe脚本里有public bool moveLeft）
            Pepe pepeScript = pepe.GetComponent<Pepe>();
            if (pepeScript != null)
            {
                pepeScript.moveLeft = pepeMoveLeft;
            }

            // 给每个Pepe生成唯一名字
            pepe.name = "Pepe_" + pepe.GetInstanceID();
        }
    }
}
