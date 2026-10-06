using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    public void StartGame()
    {
        GameProgress.ResetForNewGame();//重置游戏进度
        SceneManager.LoadScene("Act01_Longing");//加载游戏场景
    }

    public void QuitGame()
    {
        Debug.Log("退出游戏");
        Application.Quit();//退出游戏
    }
}
