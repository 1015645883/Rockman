using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelSelectManager : MonoBehaviour
{
    [Header("Cutman UI Elements")]
    public Button cutmanButton;
    public Image black1;
    public Button elecmanButton;
    public Image black2;
    public Button gutsmanButton;
    public Image black3;
    public Button icemanButton;
    public Image black4;
    public Button firemanButton;
    public Image black5;
    public Button bombmanButton;
    public Image black6;

    [Header("角色选择按钮")]
    public Button characterSelectButton; // 跳转到角色选择界面的按钮

    [Header("已选角色头像显示")]
    public Image selectedCharacterImage;
    public Sprite rockmanSprite;
    public Sprite bluesSprite;
    public Sprite cutmanSprite;
    public Sprite gutsmanSprite;
    public Sprite icemanSprite;
    public Sprite bombmanSprite;
    public Sprite firemanSprite;
    public Sprite elecmanSprite;

    [Header("新 Boss 按钮")]
    public Button finalBossButton; // 比如通往 Wily Stage 的按钮

    private void Start()
    {
        // 角色选择按钮绑定事件
        if (characterSelectButton != null)
        {
            characterSelectButton.onClick.AddListener(() =>
            {
                SceneManager.LoadScene("CharacterSelect");
            });
        }

        // 检查 Boss 是否被击败并更新 UI
        UpdateBossUI("CutmanDefeated", cutmanButton, black1);
        UpdateBossUI("ElecmanDefeated", elecmanButton, black2);
        UpdateBossUI("GutsmanDefeated", gutsmanButton, black3);
        UpdateBossUI("IcemanDefeated", icemanButton, black4);
        UpdateBossUI("FiremanDefeated", firemanButton, black5);
        UpdateBossUI("BombmanDefeated", bombmanButton, black6);

        // 检查是否所有 Boss 被打败
        CheckFinalBossCondition();
    }

    private void UpdateBossUI(string defeatedKey, Button bossButton, Image blackMask)
    {
        if (PlayerPrefs.GetInt(defeatedKey, 0) == 1)
        {
            if (blackMask != null) blackMask.enabled = true;
            if (bossButton != null) bossButton.interactable = false;
        }
        else
        {
            if (blackMask != null) blackMask.enabled = false;
        }
    }

    private void CheckFinalBossCondition()
    {
        bool allDefeated =
            PlayerPrefs.GetInt("CutmanDefeated", 0) == 1 &&
            PlayerPrefs.GetInt("ElecmanDefeated", 0) == 1 &&
            PlayerPrefs.GetInt("GutsmanDefeated", 0) == 1 &&
            PlayerPrefs.GetInt("IcemanDefeated", 0) == 1 &&
            PlayerPrefs.GetInt("FiremanDefeated", 0) == 1 &&
            PlayerPrefs.GetInt("BombmanDefeated", 0) == 1;

        if (allDefeated)
        {
            // 隐藏角色头像，显示最终 Boss 按钮
            if (selectedCharacterImage != null) selectedCharacterImage.gameObject.SetActive(false);
            if (finalBossButton != null) finalBossButton.gameObject.SetActive(true);
        }
        else
        {
            // 正常显示角色头像
            if (selectedCharacterImage != null) selectedCharacterImage.gameObject.SetActive(true);
            if (finalBossButton != null) finalBossButton.gameObject.SetActive(false);

            UpdateSelectedCharacterUI();
        }
    }

    private void UpdateSelectedCharacterUI()
    {
        string selectedCharacter = PlayerPrefs.GetString("SelectedCharacter", "Rockman");

        if (selectedCharacterImage != null)
        {
            switch (selectedCharacter)
            {
                case "Rockman":
                    selectedCharacterImage.sprite = rockmanSprite;
                    break;
                case "Blues":
                    selectedCharacterImage.sprite = bluesSprite;
                    break;
                case "Cutman":
                    selectedCharacterImage.sprite = cutmanSprite;
                    break;
                case "Gutsman":
                    selectedCharacterImage.sprite = gutsmanSprite;
                    break;
                case "Iceman":
                    selectedCharacterImage.sprite = icemanSprite;
                    break;
                case "Bombman":
                    selectedCharacterImage.sprite = bombmanSprite;
                    break;
                case "Fireman":
                    selectedCharacterImage.sprite = firemanSprite;
                    break;
                case "Elecman":
                    selectedCharacterImage.sprite = elecmanSprite;
                    break;
                default:
                    selectedCharacterImage.sprite = rockmanSprite;
                    break;
            }
        }
    }

    // 加载指定关卡
    public void LoadLevel(string levelName)
    {
        SceneManager.LoadScene(levelName);
    }
}
