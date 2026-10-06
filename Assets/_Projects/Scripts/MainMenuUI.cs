using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("RoomPrototype");//加载游戏场景
    }

    public void QuitGame()
    {
        Debug.Log("退出游戏");
        Application.Quit();//退出游戏
    }
}
