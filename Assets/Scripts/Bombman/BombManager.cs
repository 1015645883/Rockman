using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BombManager : MonoBehaviour
{
    // BombState 用于保存炸弹初始状态和运行时对象
    private class BombState
    {
        public Vector3 position;
        public float countdownTime;
        public bool isActivated;
        public Sprite initialSprite;
        public CountBomb bombReference;    // 当前场景对象
        public GameObject runtimePrefab;   // 保存运行时对象作为“运行时 prefab”
    }

    private List<BombState> bombStates = new List<BombState>();

    private void Start()
    {
        StartCoroutine(CollectBombsWithDelay(1f)); // 延迟1秒收集炸弹
    }

    private IEnumerator CollectBombsWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        CountBomb[] bombsInScene = FindObjectsOfType<CountBomb>();
        foreach (var bomb in bombsInScene)
        {
            if (bomb != null)
            {
                BombState state = new BombState
                {
                    position = bomb.transform.position,
                    countdownTime = bomb.countdownTime,
                    isActivated = bomb.isActivated,
                    initialSprite = bomb.spriteRenderer.sprite,
                    bombReference = bomb,
                    runtimePrefab = bomb.gameObject  // 保存运行时对象
                };
                bombStates.Add(state);
            }
        }

        Debug.Log("BombManager：收集炸弹数量 = " + bombStates.Count);
    }

    // 玩家复活时调用
    public void ResetBombs()
    {
        foreach (var state in bombStates)
        {
            if (state.bombReference == null)
            {
                // 已销毁炸弹，从运行时对象克隆
                GameObject newBombObj = Instantiate(state.runtimePrefab, state.position, Quaternion.identity);
                CountBomb newBomb = newBombObj.GetComponent<CountBomb>();
                if (newBomb != null)
                {
                    // 重置炸弹状态
                    newBomb.countdownTime = state.countdownTime;
                    newBomb.isActivated = false;
                    newBomb.spriteRenderer.sprite = state.initialSprite;
                    newBomb.isFrozen = false;

                    state.bombReference = newBomb;
                }
            }
            else
            {
                // 炸弹存在但已经激活
                if (state.bombReference.isActivated)
                {
                    state.bombReference.ResetBomb();
                }
                // 未激活的炸弹保持原样
            }
        }
    }
}
