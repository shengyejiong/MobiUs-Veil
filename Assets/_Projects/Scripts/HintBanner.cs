using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 操作提示条：等开场对白结束后淡入一小段文字，过几秒自动淡出。
///
/// 用法：挂在 Canvas 下的一个 TMP 文字物体上，填 Hint Text。
/// 时间用的是 unscaled（面板暂停时也能正常淡入淡出）。
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class HintBanner : MonoBehaviour
{
    [TextArea]
    [SerializeField] private string hintText = "WASD 移动 · E 交互/确认 · B 背包 · ESC 菜单";

    [Tooltip("等开场对白结束后，再等几秒才显示")]
    [SerializeField] private float delayAfterDialogue = 0.4f;

    [Tooltip("显示多久（秒）")]
    [SerializeField] private float holdSeconds = 7f;

    [Tooltip("淡入/淡出时长（秒）")]
    [SerializeField] private float fadeSeconds = 1f;

    private TMP_Text label;
    private CanvasGroup group;

    private void Awake()
    {
        label = GetComponent<TMP_Text>();

        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();

        if (label != null) label.text = hintText;
        group.alpha = 0f;
    }

    private IEnumerator Start()
    {
        // 等开场对白播完
        while (DialogueManager.Instance != null && DialogueManager.Instance.IsOpen)
        {
            yield return null;
        }

        if (delayAfterDialogue > 0f)
        {
            yield return new WaitForSecondsRealtime(delayAfterDialogue);
        }

        yield return FadeTo(1f);
        yield return new WaitForSecondsRealtime(holdSeconds);
        yield return FadeTo(0f);

        gameObject.SetActive(false);
    }

    private IEnumerator FadeTo(float target)
    {
        float from = group.alpha;
        float t = 0f;

        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, target, Mathf.Clamp01(t / fadeSeconds));
            yield return null;
        }

        group.alpha = target;
    }
}
