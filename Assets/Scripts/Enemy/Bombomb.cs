using System;
using System.Collections;
using UnityEngine;

public class Bombomb : MonoBehaviour
{
    [Header("飞行参数")]
    public float flyHeight = 10f;
    public float flyDuration = 1f;
    public float splitDelay = 0.5f;

    [Header("分裂参数")]
    public GameObject smallBombPrefab;
    public float splitForceX = 9f;
    public float splitForceY = 5f;

    [Header("爆炸参数")]
    public GameObject explosionPrefab;

    private Vector3 spawnPoint;
    private bool isHit = false;
    public bool isFrozen = false;

    public event Action OnDie; // 🔹 新增死亡事件

    private void Start()
    {
        spawnPoint = transform.position;
        Vector3 targetPos = spawnPoint + new Vector3(0f, flyHeight, 0f);
        StartCoroutine(FlyUpAndSplit(targetPos));
    }

    private IEnumerator FlyUpAndSplit(Vector3 targetPos)
    {
        float timer = 0f;
        Vector3 startPos = transform.position;

        while (timer < flyDuration)
        {
            if (isHit) yield break;
            timer += Time.deltaTime;
            float t = timer / flyDuration;
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        transform.position = targetPos;

        float waitTimer = 0f;
        while (waitTimer < splitDelay)
        {
            if (isHit) yield break;
            waitTimer += Time.deltaTime;
            yield return null;
        }

        // 🔹 分裂生成 SmallBombomb
        Split();

        // 🔹 触发死亡事件
        OnDie?.Invoke();
        Destroy(gameObject);
    }

    private void Split()
    {
        if (smallBombPrefab == null) return;

        Vector3[] directions = new Vector3[]
        {
            new Vector3(-splitForceX, splitForceY, 0f),
            new Vector3(-splitForceX / 2f, splitForceY, 0f),
            new Vector3(splitForceX / 2f, splitForceY, 0f),
            new Vector3(splitForceX, splitForceY, 0f)
        };

        foreach (Vector3 force in directions)
        {
            GameObject sb = Instantiate(smallBombPrefab, transform.position, Quaternion.identity);
            Rigidbody2D rb = sb.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.AddForce(force, ForceMode2D.Impulse);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isHit) return;

        if (collision.CompareTag("Bullet") ||
            collision.CompareTag("ChargeBullet") ||
            collision.CompareTag("RollingCutter") ||
            collision.CompareTag("SuperArm") ||
            collision.CompareTag("HyperBomb") ||
            collision.CompareTag("FireStorm") ||
            collision.CompareTag("ThunderBeam") ||
            collision.CompareTag("Player"))
        {
            Explode();
        }
        else if (collision.CompareTag("IceArrow"))
        {
            RIceArrow iceArrow = collision.GetComponent<RIceArrow>();
            if (iceArrow != null)
            {
                if (!isFrozen)
                {
                    iceArrow.Freeze(this);
                    iceArrow.DestroyBullet();
                }
                else
                {
                    iceArrow.DestroyBullet();
                }
            }
        }
    }

    private void Explode()
    {
        isHit = true;

        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        EnemyAutoDrop drop = GetComponent<EnemyAutoDrop>();
        if (drop != null)
            drop.TryDrop();

        // 🔹 触发死亡事件
        OnDie?.Invoke();

        Destroy(gameObject);
    }
}
