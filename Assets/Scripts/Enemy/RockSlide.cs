using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class RockSlide : MonoBehaviour
{
    [Header("生成石头")]
    public GameObject rockPrefab;
    public float spawnInterval = 2f;
    public Transform spawnPoint;

    [Header("忽略对象")]
    public GameObject lift;

    [Header("防抖设置")]
    public float stopDelaySeconds = 0.12f;

    [Header("调试")]
    public bool debugLogs = false;

    private Collider2D detectionCollider;
    private Coroutine spawnCoroutine;
    private HashSet<Collider2D> playerColliders = new HashSet<Collider2D>();

    // 替代协程的延迟停止机制（避免在 OnTriggerExit2D 中 StartCoroutine）
    private bool pendingStop = false;
    private float stopTime = -1f;

    private void Start()
    {
        detectionCollider = GetComponent<Collider2D>();
        if (detectionCollider != null && !detectionCollider.isTrigger)
            detectionCollider.isTrigger = true;

        if (spawnPoint == null)
            spawnPoint = transform;
    }

    // 根据你项目识别玩家的方式修改此函数（这里假设玩家带 Player 标签并且父级有 PlayerMovement）
    private bool IsPlayerCollider(Collider2D col)
    {
        if (col == null) return false;
        if (!col.CompareTag("Player")) return false;
        return col.GetComponentInParent<PlayerMovement>() != null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayerCollider(other)) return;

        if (playerColliders.Add(other))
        {
            if (debugLogs) Debug.Log($"RockSlide Enter: {other.name}, count={playerColliders.Count}");
            // 取消任何待停止状态
            pendingStop = false;
            stopTime = -1f;

            if (spawnCoroutine == null)
            {
                spawnCoroutine = StartCoroutine(SpawnRocks());
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayerCollider(other)) return;

        if (playerColliders.Remove(other))
        {
            if (debugLogs) Debug.Log($"RockSlide Exit: {other.name}, count={playerColliders.Count}");

            if (playerColliders.Count == 0)
            {
                // 不再 StartCoroutine，改为设置时间戳，在 Update 里完成延迟停止
                pendingStop = true;
                stopTime = Time.time + Mathf.Max(0f, stopDelaySeconds);
                if (debugLogs) Debug.Log($"RockSlide: pendingStop set, will stop at {stopTime}");
            }
        }
    }

    private void Update()
    {
        if (pendingStop && Time.time >= stopTime)
        {
            // 双重检查：确保期间没有新的 collider 进入
            if (playerColliders.Count == 0)
            {
                if (debugLogs) Debug.Log("RockSlide: stop delay expired -> stopping spawn");
                if (spawnCoroutine != null)
                {
                    StopCoroutine(spawnCoroutine);
                    spawnCoroutine = null;
                }
            }
            pendingStop = false;
            stopTime = -1f;
        }
    }

    private IEnumerator SpawnRocks()
    {
        if (debugLogs) Debug.Log("RockSlide: SpawnRocks started");

        while (playerColliders.Count > 0)
        {
            if (rockPrefab != null)
            {
                GameObject rock = Instantiate(rockPrefab, spawnPoint.position, Quaternion.identity);

                // 忽略和 Lift 的碰撞
                if (lift != null)
                {
                    Collider2D rockCol = rock.GetComponent<Collider2D>();
                    Collider2D liftCol = lift.GetComponent<Collider2D>();
                    if (rockCol != null && liftCol != null)
                    {
                        Physics2D.IgnoreCollision(rockCol, liftCol);
                    }
                }
            }

            yield return new WaitForSeconds(spawnInterval);
        }

        if (debugLogs) Debug.Log("RockSlide: SpawnRocks ended");
        spawnCoroutine = null;
    }

    private void OnDisable()
    {
        // 物体被禁用/销毁时，安全清理
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        spawnCoroutine = null;

        pendingStop = false;
        stopTime = -1f;
        playerColliders.Clear();
    }
}
