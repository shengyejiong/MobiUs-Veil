using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>独立的暖光循环结尾；复用第一幕布景，不重新开放第一幕任务。</summary>
public sealed class LoopEndingSequence : MonoBehaviour
{
    [SerializeField] private CanvasGroup screenFade;
    [SerializeField] private CanvasGroup titleGroup;
    [SerializeField] private TMP_Text caption;
    [Header("静态演员（仅用于播片）")]
    [SerializeField] private SpriteRenderer femaleVisual;
    [SerializeField] private Sprite femaleSideSprite;
    [SerializeField] private Transform playerVisual;
    [SerializeField, Min(0.1f)] private float fadeSeconds = 0.8f;
    [SerializeField, Min(0.1f)] private float welcomeSeconds = 2f;
    [SerializeField, Min(0.1f)] private float answerSeconds = 1.8f;
    [SerializeField, Min(0.1f)] private float finalHoldSeconds = 2.5f;
    [SerializeField] private UnityEvent onSequenceFinished = new UnityEvent();

    public bool HasFinished { get; private set; }
    private bool endInputReady;
    private Sprite originalFemaleSprite;
    private bool originalFemaleFlipX;

    private void Awake()
    {
        if (femaleVisual == null) return;
        originalFemaleSprite = femaleVisual.sprite;
        originalFemaleFlipX = femaleVisual.flipX;
    }

    private void Update()
    {
        if (!HasFinished || !endInputReady) return;
        bool returnRequested =
            (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            || (Keyboard.current != null && Keyboard.current.anyKey.wasReleasedThisFrame);
        if (!returnRequested) return;
        endInputReady = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    private IEnumerator Start()
    {
        yield return PlaySequence();
    }

    private IEnumerator PlaySequence()
    {
        Time.timeScale = 1f;
        HasFinished = false;
        endInputReady = false;
        titleGroup.alpha = 0f;
        caption.text = string.Empty;
        screenFade.alpha = 1f;
        RestoreFemalePose();
        yield return Fade(screenFade, 0f);

        if (femaleVisual != null && femaleSideSprite != null && playerVisual != null)
        {
            femaleVisual.sprite = femaleSideSprite;
            femaleVisual.flipX = playerVisual.position.x > femaleVisual.transform.position.x;
        }
        caption.text = "林晚：醒啦？";
        yield return new WaitForSeconds(welcomeSeconds);
        caption.text = string.Empty;
        yield return new WaitForSeconds(0.6f);
        caption.text = "主角：嗯。";
        yield return new WaitForSeconds(answerSeconds);
        caption.text = string.Empty;
        yield return new WaitForSeconds(finalHoldSeconds);

        yield return Fade(screenFade, 1f);
        RestoreFemalePose();
        yield return Fade(titleGroup, 1f);
        HasFinished = true;
        onSequenceFinished.Invoke();
        yield return new WaitUntil(() =>
            (Keyboard.current == null || !Keyboard.current.anyKey.isPressed)
            && (Mouse.current == null || !Mouse.current.leftButton.isPressed));
        yield return null;
        endInputReady = true;
    }

    private void RestoreFemalePose()
    {
        if (femaleVisual == null) return;
        femaleVisual.sprite = originalFemaleSprite;
        femaleVisual.flipX = originalFemaleFlipX;
    }

    private IEnumerator Fade(CanvasGroup group, float target)
    {
        float start = group.alpha;
        float elapsed = 0f;
        while (elapsed < fadeSeconds)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / fadeSeconds));
            yield return null;
        }
        group.alpha = target;
    }

    [ContextMenu("测试/重播循环结尾")]
    private void Replay()
    {
        if (!Application.isPlaying) return;
        StopAllCoroutines();
        StartCoroutine(PlaySequence());
    }
}
