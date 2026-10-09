using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [Header("开始游戏转场")]
    [SerializeField] private CanvasGroup menuFade;
    [SerializeField] private CanvasGroup menuContent;
    [SerializeField, Min(0.1f)] private float fadeOutSeconds = 1.2f;
    [SerializeField, Min(0f)] private float blackHoldSeconds = 0.35f;
    private bool isStarting;

    public void StartGame()
    {
        if (isStarting) return;
        isStarting = true;
        Time.timeScale = 1f;
        if (menuContent != null) menuContent.interactable = false;
        StartCoroutine(StartGameSequence());
    }

    private IEnumerator StartGameSequence()
    {
        if (menuFade != null)
        {
            menuFade.alpha = 0f;
            menuFade.gameObject.SetActive(true);
            menuFade.transform.SetAsLastSibling();
            menuFade.blocksRaycasts = true;
            float elapsed = 0f;
            float duration = Mathf.Max(0.1f, fadeOutSeconds);
            while (elapsed < duration)
            {
                menuFade.alpha = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            menuFade.alpha = 1f;
        }
        if (blackHoldSeconds > 0f) yield return new WaitForSecondsRealtime(blackHoldSeconds);

        GameProgress.ResetForNewGame();
        Act01Story.ResetStatics();
        Act02Story.ResetAll();
        Act03Story.ResetAll();
        GameState.ResetAll();
        ActThemeTitle.ResetForNewGame();

        yield return SceneManager.LoadSceneAsync("Act01_Longing");
    }

    public void QuitGame()
    {
        Debug.Log("退出游戏");
        Application.Quit();//退出游戏
    }
}
