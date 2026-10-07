using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>现实卧室演出；黑屏独白结束后进入暖光循环结尾。</summary>
public sealed class RealityBedroomSequence : MonoBehaviour
{
    [Header("画面引用")]
    [SerializeField] private SpriteRenderer playerVisual;
    [SerializeField] private CanvasGroup screenFade;
    [SerializeField] private CanvasGroup phoneCloseup;
    [SerializeField] private CanvasGroup photoCloseup;
    [SerializeField] private TMP_Text caption;

    [Header("睡眠与醒来立绘")]
    [SerializeField] private Sprite sleepingSprite;
    [SerializeField] private Sprite awakeSprite;
    [SerializeField, Min(0f)] private float sleepingHoldSeconds = 0.6f;
    [SerializeField, Min(0.1f)] private float wakeSeconds = 0.6f;

    [Header("固定演出时间，单位：秒")]
    [SerializeField, Min(0.1f)] private float fadeSeconds = 0.8f;
    [SerializeField, Min(0.1f)] private float phoneSeconds = 2.5f;
    [SerializeField, Min(0.1f)] private float emptyBedSeconds = 2.5f;
    [SerializeField, Min(0.1f)] private float quietSeconds = 2.5f;
    [SerializeField, Min(0.1f)] private float lieDownSeconds = 1.5f;

    [Header("淡黑后的两句独白")]
    [SerializeField] private CanvasGroup[] blackDialogueBoxes;
    [SerializeField, Min(0.01f)] private float blackDialogueFadeSeconds = 0.35f;
    [SerializeField, Min(0f)] private float blackDialogueHoldSeconds = 1.8f;
    [SerializeField, Min(0f)] private float blackDialogueGapSeconds = 0.25f;

    [Header("声音（可选）")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip breathClip;

    [Header("后续场景")]
    [SerializeField] private string nextSceneName = "Ending_Loop";
    [SerializeField] private UnityEvent onSequenceFinished = new UnityEvent();

    public bool HasFinished { get; private set; }
    public string CurrentStage { get; private set; }

    private Vector3 awakePosition;
    private Color awakeColor;

    private void Awake()
    {
        Time.timeScale = 1f;
        awakePosition = playerVisual.transform.localPosition;
        awakeColor = playerVisual.color;
        if (awakeSprite == null) awakeSprite = playerVisual.sprite;
        if (photoCloseup != null) photoCloseup.gameObject.SetActive(false);
    }

    private IEnumerator Start()
    {
        yield return PlaySequence();
    }

    private IEnumerator PlaySequence()
    {
        HasFinished = false;
        caption.text = string.Empty;
        phoneCloseup.alpha = 0f;
        if (blackDialogueBoxes != null)
        {
            foreach (CanvasGroup box in blackDialogueBoxes)
                if (box != null) box.alpha = 0f;
        }
        if (photoCloseup != null)
        {
            photoCloseup.alpha = 0f;
            photoCloseup.gameObject.SetActive(false);
        }

        screenFade.alpha = 1f;
        if (sleepingSprite != null) playerVisual.sprite = sleepingSprite;
        playerVisual.transform.localPosition = awakePosition + Vector3.down * 0.12f;
        playerVisual.color = awakeColor * new Color(0.82f, 0.82f, 0.86f, 1f);
        CurrentStage = "睡眠";
        if (audioSource != null && breathClip != null) audioSource.PlayOneShot(breathClip);
        yield return Fade(screenFade, 0f, fadeSeconds);
        yield return new WaitForSeconds(sleepingHoldSeconds);

        CurrentStage = "从睡眠转醒";
        if (awakeSprite != null) playerVisual.sprite = awakeSprite;
        Vector3 sleepingPosition = playerVisual.transform.localPosition;
        Color sleepingColor = playerVisual.color;
        float wakeElapsed = 0f;
        float wakeDuration = Mathf.Max(0.1f, wakeSeconds);
        while (wakeElapsed < wakeDuration)
        {
            float t = Mathf.SmoothStep(0f, 1f, wakeElapsed / wakeDuration);
            playerVisual.transform.localPosition = Vector3.Lerp(sleepingPosition, awakePosition, t);
            playerVisual.color = Color.Lerp(sleepingColor, awakeColor, t);
            wakeElapsed += Time.deltaTime;
            yield return null;
        }
        playerVisual.transform.localPosition = awakePosition;
        playerVisual.color = awakeColor;
        CurrentStage = "手机 03:17";
        yield return Fade(phoneCloseup, 1f, 0.25f);
        yield return new WaitForSeconds(phoneSeconds);
        yield return Fade(phoneCloseup, 0f, 0.25f);

        CurrentStage = "空床一侧";
        yield return new WaitForSeconds(emptyBedSeconds);

        CurrentStage = "望向空床";
        yield return new WaitForSeconds(quietSeconds);

        CurrentStage = "再次躺下";
        caption.text = "……再待一会儿。";
        yield return new WaitForSeconds(lieDownSeconds);
        if (sleepingSprite != null) playerVisual.sprite = sleepingSprite;
        playerVisual.transform.localPosition = awakePosition + Vector3.down * 0.12f;
        playerVisual.color = awakeColor * new Color(0.82f, 0.82f, 0.86f, 1f);
        yield return Fade(screenFade, 1f, fadeSeconds);

        caption.text = string.Empty;
        yield return PlayBlackDialogues();

        HasFinished = true;
        CurrentStage = "现实片段完成";
        onSequenceFinished.Invoke();
        if (!string.IsNullOrWhiteSpace(nextSceneName))
        {
            if (Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                CurrentStage = "进入循环结尾";
                yield return SceneManager.LoadSceneAsync(nextSceneName);
            }
            else Debug.LogWarning($"现实卧室找不到后续场景：{nextSceneName}，请检查构建场景列表。");
        }
    }

    private IEnumerator PlayBlackDialogues()
    {
        if (blackDialogueBoxes == null) yield break;
        for (int i = 0; i < blackDialogueBoxes.Length; i++)
        {
            CanvasGroup box = blackDialogueBoxes[i];
            if (box == null) continue;
            CurrentStage = $"黑屏独白 {i + 1}";
            box.gameObject.SetActive(true);
            yield return Fade(box, 1f, blackDialogueFadeSeconds);
            yield return new WaitForSeconds(blackDialogueHoldSeconds);
            yield return Fade(box, 0f, blackDialogueFadeSeconds);
            if (i < blackDialogueBoxes.Length - 1)
                yield return new WaitForSeconds(blackDialogueGapSeconds);
        }
    }

    private static IEnumerator Fade(CanvasGroup group, float target, float seconds)
    {
        float start = group.alpha;
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / seconds));
            yield return null;
        }
        group.alpha = target;
    }

    [ContextMenu("测试/重播现实卧室")]
    private void Replay()
    {
        if (!Application.isPlaying) return;
        StopAllCoroutines();
        Time.timeScale = 1f;
        StartCoroutine(PlaySequence());
    }
}
