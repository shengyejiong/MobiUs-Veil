using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>独立的现实卧室演出；不修改背包进度，也不加载其他场景。</summary>
public sealed class RealityBedroomSequence : MonoBehaviour
{
    [Header("画面引用")]
    [SerializeField] private SpriteRenderer playerVisual;
    [SerializeField] private CanvasGroup screenFade;
    [SerializeField] private CanvasGroup phoneCloseup;
    [SerializeField] private CanvasGroup photoCloseup;
    [SerializeField] private TMP_Text caption;
    [SerializeField] private Image photoArtwork;
    [SerializeField] private GameObject photoPlaceholder;

    [Header("合照（美术交付后拖入这里）")]
    [SerializeField] private Sprite photoSprite;

    [Header("固定演出时间，单位：秒")]
    [SerializeField, Min(0.1f)] private float fadeSeconds = 0.8f;
    [SerializeField, Min(0.1f)] private float phoneSeconds = 2.5f;
    [SerializeField, Min(0.1f)] private float emptyBedSeconds = 2.5f;
    [SerializeField, Min(0.1f)] private float photoSeconds = 3.5f;
    [SerializeField, Min(0.1f)] private float quietSeconds = 2.5f;
    [SerializeField, Min(0.1f)] private float lieDownSeconds = 1.5f;

    [Header("声音（可选）")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip breathClip;

    [Header("后续连接口，本场景暂不绑定")]
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
        photoCloseup.alpha = 0f;
        photoArtwork.gameObject.SetActive(photoSprite != null);
        photoPlaceholder.SetActive(photoSprite == null);
        if (photoSprite != null)
        {
            photoArtwork.sprite = photoSprite;
            photoArtwork.preserveAspect = true;
        }

        screenFade.alpha = 1f;
        playerVisual.transform.localPosition = awakePosition + Vector3.down * 0.12f;
        playerVisual.color = awakeColor * new Color(0.82f, 0.82f, 0.86f, 1f);
        CurrentStage = "现实醒来";
        if (audioSource != null && breathClip != null) audioSource.PlayOneShot(breathClip);
        yield return Fade(screenFade, 0f, fadeSeconds);

        playerVisual.transform.localPosition = awakePosition;
        playerVisual.color = awakeColor;
        CurrentStage = "手机 03:17";
        yield return Fade(phoneCloseup, 1f, 0.25f);
        yield return new WaitForSeconds(phoneSeconds);
        yield return Fade(phoneCloseup, 0f, 0.25f);

        CurrentStage = "空床一侧";
        yield return new WaitForSeconds(emptyBedSeconds);

        CurrentStage = "看旧合照";
        yield return Fade(photoCloseup, 1f, 0.35f);
        yield return new WaitForSeconds(photoSeconds);
        yield return Fade(photoCloseup, 0f, 0.35f);

        CurrentStage = "放回照片，望向空床";
        yield return new WaitForSeconds(quietSeconds);

        CurrentStage = "再次躺下";
        caption.text = "……再待一会儿。";
        yield return new WaitForSeconds(lieDownSeconds);
        playerVisual.transform.localPosition = awakePosition + Vector3.down * 0.12f;
        playerVisual.color = awakeColor * new Color(0.82f, 0.82f, 0.86f, 1f);
        yield return Fade(screenFade, 1f, fadeSeconds);

        HasFinished = true;
        CurrentStage = "现实片段完成";
        onSequenceFinished.Invoke();
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
