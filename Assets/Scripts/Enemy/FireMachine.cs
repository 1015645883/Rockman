using UnityEngine;
using System.Collections;

public class FireMachine : MonoBehaviour
{
    public GameObject player;
    public FireWave fireWave;
    public float detectRadius = 5f;
    public float spawnInterval = 3f;   // 间隔时间
    public float activeTime = 1.5f;    // FireWave 激活持续时间

    private bool isActive = false;

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
                yield break;
            }
            yield return null;
        }
    }

    void Update()
    {
        if (player == null || fireWave == null) return;

        float distance = Vector2.Distance(transform.position, player.transform.position);

        if (distance <= detectRadius && !isActive)
        {
            StartCoroutine(ActivateCycle());
        }
        else if (distance > detectRadius)
        {
            // 玩家离开范围时立即关闭
            fireWave.SleepWave();
            StopAllCoroutines();
            isActive = false;
        }
    }

    IEnumerator ActivateCycle()
    {
        isActive = true;

        while (true)
        {
            // 激活 FireWave
            fireWave.ActivateWave();
            yield return new WaitForSeconds(activeTime);

            // 进入休眠
            fireWave.SleepWave();
            yield return new WaitForSeconds(spawnInterval - activeTime);
        }
    }
}
