using TMPro;
using UnityEngine;

public class DisableTMPUnderline : MonoBehaviour
{
    void Start()
    {
        // 方法1：禁用所有TMP文本的富文本解析（包括<u>标签）
        TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>(true);
        foreach (var tmpText in allTexts)
        {
            tmpText.richText = false; // 这将禁用所有富文本标签
        }

        // 方法2：替换所有<u>标签（如果仍需保留其他富文本）
        /*
        foreach (var tmpText in allTexts)
        {
            tmpText.text = tmpText.text.Replace("<u>", "").Replace("</u>", "");
        }
        */
        // 在初始化脚本中添加（不推荐长期使用）
        Debug.unityLogger.filterLogType = LogType.Error;
    }
}