using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombombSpawner : MonoBehaviour
{
    [Header("小怪设置")]
    public GameObject bombombPrefab;     // Bombomb 预制体
    public int maxAliveEnemies = 3;      // 场上最多存活数量
    public float roundCooldown = 2f;     // 轮间休息时间
    public float enterProtectionTime = 2f; // 玩家进入后的保护时间

    [Header("检测设置")]
    public string playerTag = "Player"; // 玩家标签

    private Collider2D detectionCollider;
    private bool playerInRange = false;
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private bool spawning = false;
    private Coroutine protectionCoroutine;

    private void Awake()
    {
        detectionCollider = GetComponent<Collider2D>();
        if (detectionCollider == null)
        {
            Debug.LogError("必须挂载 Collider2D 用于检测玩家！");
        }
        detectionCollider.isTrigger = true; // 确保是触发器
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;

            if (protectionCoroutine == null)
                protectionCoroutine = StartCoroutine(ProtectionDelay());
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;

            if (protectionCoroutine != null)
            {
                StopCoroutine(protectionCoroutine);
                protectionCoroutine = null;
            }
        }
    }

    private IEnumerator ProtectionDelay()
    {
        float timer = 0f;
        while (timer < enterProtectionTime && playerInRange)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (playerInRange && !spawning)
        {
            StartCoroutine(SpawnRoutine());
        }

        protectionCoroutine = null;
    }

    private IEnumerator SpawnRoutine()
    {
        spawning = true;

        while (playerInRange)
        {
            // 场上数量 < 最大存活数时生成
            if (spawnedEnemies.Count < maxAliveEnemies)
            {
                GameObject enemyGO = Instantiate(bombombPrefab, transform.position, Quaternion.identity);
                enemyGO.name = "Bombomb_" + enemyGO.GetInstanceID();
                spawnedEnemies.Add(enemyGO);

                // 绑定死亡事件
                Bombomb bb = enemyGO.GetComponent<Bombomb>();
                if (bb != null)
                {
                    bb.OnDie += () => spawnedEnemies.Remove(enemyGO);
                }

                // 每次只生成 1 个，生成间隔为 roundCooldown
                yield return new WaitForSeconds(roundCooldown);
            }
            else
            {
                // 如果已满，等待 cooldown 后再尝试
                float waitTimer = 0f;
                while (waitTimer < roundCooldown && playerInRange)
                {
                    waitTimer += Time.deltaTime;
                    yield return null;
                }
            }
        }

        spawning = false;
    }
}
