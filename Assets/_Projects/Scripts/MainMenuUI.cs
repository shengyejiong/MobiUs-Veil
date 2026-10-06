using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    public void StartGame()
    {
        Time.timeScale = 1f;

        GameProgress.ResetForNewGame();
        Act01Story.ResetStatics();
        Act02Story.ResetAll();
        Act03Story.ResetAll();
        GameState.ResetAll();

        SceneManager.LoadScene("Act01_Longing");
    }

    public void QuitGame()
    {
        Debug.Log("退出游戏");
        Application.Quit();//退出游戏
    }
}
