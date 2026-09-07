using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    private void Start()
    {
    }

    // ===== 新游戏 =====
    public void NewGame()
    {
        // 清空所有存档
        PlayerPrefs.DeleteAll();

        // 设置默认角色
        PlayerPrefs.SetString("SelectedCharacter", "Rockman");

        PlayerPrefs.Save();

        // 进入选关界面
        SceneManager.LoadScene("LevelSelect");
    }

    // ===== 继续游戏 =====
    public void ContinueGame()
    {
        // 不做任何修改，直接进入
        SceneManager.LoadScene("LevelSelect");
    }

    // ===== 退出游戏 =====
    public void ExitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}