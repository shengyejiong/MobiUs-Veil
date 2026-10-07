using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>每局首次进入本幕时显示主题字，不阻塞对白或玩家控制。</summary>
public sealed class ActThemeTitle : MonoBehaviour
{
    [SerializeField] private TMP_Text themeText;
    [SerializeField] private CanvasGroup titleGroup;
    [SerializeField] private ScreenFader entryFader;
    [SerializeField] private string themeCharacter = "慕";
    [SerializeField, Min(0.01f)] private float fadeSeconds = 0.45f;
    [SerializeField, Min(0f)] private float holdSeconds = 3f;

    private static readonly HashSet<string> shownScenes = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetForNewGame()
    {
        shownScenes.Clear();
    }

    private IEnumerator Start()
    {
        if (titleGroup == null || themeText == null) yield break;
        titleGroup.alpha = 0f;
        themeText.text = themeCharacter;
        themeText.outlineWidth = 0.12f;
        themeText.outlineColor = Color.black;
        string sceneKey = gameObject.scene.name;
        if (shownScenes.Contains(sceneKey)) yield break;

        // 入场淡黑消失后才显示；开场对白可以同时继续。
        while (entryFader != null && entryFader.canvasGroup != null
            && entryFader.canvasGroup.alpha > 0.01f)
            yield return null;
        if (!shownScenes.Add(sceneKey)) yield break;

        yield return FadeTitle(1f);
        yield return new WaitForSeconds(holdSeconds);
        yield return FadeTitle(0f);
    }

    private IEnumerator FadeTitle(float targetAlpha)
    {
        float initialAlpha = titleGroup.alpha;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, fadeSeconds);
        while (elapsed < duration)
        {
            titleGroup.alpha = Mathf.Lerp(initialAlpha, targetAlpha,
                Mathf.SmoothStep(0f, 1f, elapsed / duration));
            elapsed += Time.deltaTime;
            yield return null;
        }
        titleGroup.alpha = targetAlpha;
    }
}
