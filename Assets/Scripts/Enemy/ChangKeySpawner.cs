using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangKeySpawner : MonoBehaviour
{
    [Header("小怪设置")]
    public GameObject changKeyPrefab;   // 小怪预制体
    public int maxAliveEnemies = 3;     // 场上最多存活数量
    public float roundCooldown = 2f;    // 轮间休息时间
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
            // 如果场上数量 < 最大存活数，才生成
            if (spawnedEnemies.Count < maxAliveEnemies)
            {
                int needToSpawn = maxAliveEnemies - spawnedEnemies.Count;
                for (int i = 0; i < needToSpawn; i++)
                {
                    GameObject enemyGO = Instantiate(changKeyPrefab, transform.position, Quaternion.identity);
                    enemyGO.name = "ChangKey_" + enemyGO.GetInstanceID();
                    spawnedEnemies.Add(enemyGO);

                    // 绑定死亡事件
                    ChangKey ck = enemyGO.GetComponent<ChangKey>();
                    if (ck != null)
                    {
                        ck.OnDie += () => spawnedEnemies.Remove(enemyGO);
                    }

                    yield return new WaitForSeconds(0.5f); // 小怪之间的生成间隔
                }
            }

            // 等待 cooldown 再尝试下一次生成
            float roundTimer = 0f;
            while (roundTimer < roundCooldown && playerInRange)
            {
                roundTimer += Time.deltaTime;
                yield return null;
            }
        }

        spawning = false;
    }
}
