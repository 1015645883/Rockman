using UnityEngine;

public class MeetingPoint : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 通知关卡管理器
            FindObjectOfType<SpecialLevelManager>().OnCharacterReachedMeetingPoint();

            // 如果是火焰人，触发全局光源禁用
            if (other.name.Contains("PlayableFireman")|| other.name.Contains("PlayableBombman"))
            {
                // 找到所有 LightController 并设置它们的 permanentDisable
                LightController[] lights = FindObjectsOfType<LightController>();
                foreach (var light in lights)
                {
                    light.allowLight = false;
                }
            }
        }
    }
}
