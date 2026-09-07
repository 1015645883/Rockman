using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class YokuTileData
{
    public Vector3Int position;
    public float appearDelay = 0f;
    public float visibleDuration = 1f;
}

public class YokuBlockController : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap tilemap;              // 主砖块 Tilemap
    public Tilemap backgroundTilemap;    // 背景砖块 Tilemap（用于阴影）

    [Header("Tiles")]
    public TileBase blinkTile;           // 主砖块 Tile
    public TileBase backgroundTile;      // 背景砖块 Tile（阴影）
    public GameObject appearEffectPrefab;


    [Header("Schedule Settings")]
    public List<YokuTileData> scheduleList;

    [Header("Detection Settings")]
    public float detectionRadius = 5f;
    public Transform detectionCenter;

    [Header("Sound")]
    public AudioSource audioSource;
    public AudioClip appearSound;

    private bool playerInRange = false;
    private Coroutine controlRoutine;

    private Transform Player
    {
        get
        {
            SpecialLevelManager slm = FindObjectOfType<SpecialLevelManager>();
            if (slm != null && slm.currentPlayer != null)
                return slm.currentPlayer.transform; // ✅ 特殊关卡中的当前角色

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            return playerObj != null ? playerObj.transform : null; // ✅ 普通关卡玩家
        }
    }

    // 在开始时隐藏所有砖块
    void Start()
    {
        ClearAllTiles(); // 初始化时所有砖块都不显示
    }

    void Update()
    {
        Transform player = Player; // ✅ 改用统一属性
        if (player == null || detectionCenter == null)
            return;

        float dist = Vector2.Distance(player.position, detectionCenter.position);

        if (dist <= detectionRadius)
        {
            if (!playerInRange)
            {
                playerInRange = true;
                controlRoutine = StartCoroutine(RunBlinkSchedule());
            }
        }
        else
        {
            if (playerInRange)
            {
                playerInRange = false;
                if (controlRoutine != null)
                    StopCoroutine(controlRoutine);
                ClearAllTiles();
            }
        }
    }


    IEnumerator RunBlinkSchedule()
    {
        while (playerInRange)
        {
            List<Coroutine> activeTiles = new List<Coroutine>();

            foreach (var tileData in scheduleList)
            {
                Coroutine tileRoutine = StartCoroutine(HandleTileBlink(tileData));
                activeTiles.Add(tileRoutine);
            }

            float maxTime = 0f;
            foreach (var tile in scheduleList)
            {
                float endTime = tile.appearDelay + tile.visibleDuration;
                if (endTime > maxTime)
                    maxTime = endTime;
            }

            yield return new WaitForSeconds(maxTime);
        }
    }

    IEnumerator HandleTileBlink(YokuTileData data)
    {
        yield return new WaitForSeconds(data.appearDelay);

        // 播放音效
        if (appearSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(appearSound);
        }

        if (appearEffectPrefab != null)
        {
            Vector3 worldPos = tilemap.CellToWorld(data.position) + tilemap.tileAnchor;
            Instantiate(appearEffectPrefab, worldPos, Quaternion.identity);
        }

        // 设置主砖块
        tilemap.SetTile(data.position, blinkTile);

        // 设置背景砖块：位置下移 1 格
        if (backgroundTilemap != null && backgroundTile != null)
        {
            Vector3Int bgPos = new Vector3Int(data.position.x, data.position.y - 1, data.position.z);
            backgroundTilemap.SetTile(bgPos, backgroundTile);
        }

        yield return new WaitForSeconds(data.visibleDuration);

        // 清除主砖块
        tilemap.SetTile(data.position, null);

        // 清除背景砖块
        if (backgroundTilemap != null)
        {
            Vector3Int bgPos = new Vector3Int(data.position.x, data.position.y - 1, data.position.z);
            backgroundTilemap.SetTile(bgPos, null);
        }
    }

    void ClearAllTiles()
    {
        foreach (var data in scheduleList)
        {
            tilemap.SetTile(data.position, null);

            if (backgroundTilemap != null)
            {
                Vector3Int bgPos = new Vector3Int(data.position.x, data.position.y - 1, data.position.z);
                backgroundTilemap.SetTile(bgPos, null);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

}
