using UnityEngine;

public class StartGameManager : MonoBehaviour
{
    private void Awake()
    {


        // 初始化默认角色
        if (!PlayerPrefs.HasKey("SelectedCharacter"))
        {
            PlayerPrefs.SetString("SelectedCharacter", "Rockman");
            PlayerPrefs.Save();
            Debug.Log("默认角色已设置为洛克人");
        }
    }
}
