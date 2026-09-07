using UnityEngine;
using System.Collections;

public class ElecMachine : MonoBehaviour
{
    public GameObject player;                    // 玩家对象
    public GameObject elecWavePrefab;            // ElecWave 预制体
    public float detectRadius = 5f;              // 检测半径
    public float spawnInterval = 3f;             // 每几秒生成一次
    public float waveLifetime = 1.5f;            // ElecWave 持续时间
    public float waveWidth = 5f;                 // ElecWave 占据的宽度（单位）

    public enum Direction { Left, Right }
    public Direction waveDirection = Direction.Right;

    private float timer = 0f;

    void Start()
    {
        StartCoroutine(FindPlayer());
    }

    IEnumerator FindPlayer()
    {
        while (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj;
                yield break; // 找到了就退出协程
            }
            yield return null; // 等一帧再试
        }
    }

    void Update()
    {
        if (player == null || elecWavePrefab == null) return;

        float distance = Vector2.Distance(transform.position, player.transform.position);
        if (distance <= detectRadius)
        {
            timer += Time.deltaTime;
            if (timer >= spawnInterval)
            {
                SpawnElecWave();
                timer = 0f;
            }
        }
        else
        {
            timer = 0f; // 离开范围后重置
        }
    }

    void SpawnElecWave()
    {
        Vector3 spawnPosition;

        if (waveDirection == Direction.Right)
        {
            spawnPosition = transform.position + new Vector3(waveWidth / 2f, 0f, 0f);
        }
        else // Left
        {
            spawnPosition = transform.position - new Vector3(waveWidth / 2f, 0f, 0f);
        }

        GameObject wave = Instantiate(elecWavePrefab, spawnPosition, Quaternion.identity);
        Destroy(wave, waveLifetime);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}
