using System.Collections;
using UnityEngine;

/// <summary>
/// 进场独白：场景淡入结束后自动播一段对白（一次性）。
/// 第二幕的"……林晚？"、第三/四幕的开场都能用它。
/// 用法：在场景里新建一个空物体（例如 OpeningLine），挂上这个脚本，填 Opening Lines。
/// </summary>
public class ActOpeningLine : MonoBehaviour
{
    [SerializeField] private DialogueLine[] openingLines;

    [Tooltip("进场到开场对白的最短等待时间；对白一定在淡入完成后开始")]
    [SerializeField] private float delay = 1.2f;

    private bool hasPlayed;

    private IEnumerator Start()
    {
        // 有转场控制器时，由它统一负责淡入、开场对白和移动解锁。
        SceneTransition transition = FindFirstObjectByType<SceneTransition>();
        if (transition != null && transition.isActiveAndEnabled) yield break;

        yield return PlayOpening();
    }

    public IEnumerator PlayOpening(float elapsedSinceEntry = 0f)
    {
        if (hasPlayed) yield break;
        hasPlayed = true;
        if (openingLines == null || openingLines.Length == 0) yield break;

        float remainingDelay = Mathf.Max(0f, delay - elapsedSinceEntry);
        if (remainingDelay > 0f) yield return new WaitForSeconds(remainingDelay);

        DialogueManager dm = DialogueManager.Instance;
        if (dm == null) yield break;

        // 不覆盖另一段已经打开的对白。
        while (dm != null && dm.IsOpen) yield return null;
        if (dm == null) yield break;

        dm.StartDialogue(openingLines);
        while (dm != null && dm.IsOpen) yield return null;
    }
}
