using UnityEngine;
using System.Collections;

public class CutterMachine : MonoBehaviour
{
    public GameObject player;
    public GameObject cutterPrefab;
    public float detectRadius = 5f;
    public float throwInterval = 1.5f;
    public Transform cutterSpawnPoint;

    private float throwTimer;

    void Start()
    {
        StartCoroutine(DelayedFindPlayer());
    }

    IEnumerator DelayedFindPlayer()
    {
        // 延迟0.5秒再查找，确保玩家对象已经生成
        yield return new WaitForSeconds(0.5f);

        player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
            Debug.LogWarning("CutterMachine: 未找到标签为 Player 的对象！");
    }


    void Update()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.transform.position);
        if (distance <= detectRadius)
        {
            throwTimer += Time.deltaTime;
            if (throwTimer >= throwInterval)
            {
                ThrowCutter();
                throwTimer = 0f;
            }
        }
    }

    void ThrowCutter()
    {
        GameObject cutter = Instantiate(cutterPrefab, cutterSpawnPoint.position, Quaternion.identity);
        Vector2 direction = (player.transform.position - transform.position).normalized;
        cutter.GetComponent<CutterProjectile>().Launch(direction);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}
