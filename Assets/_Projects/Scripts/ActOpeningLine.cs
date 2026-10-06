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

    [Tooltip("等淡入结束再说话（秒）")]
    [SerializeField] private float delay = 1.2f;

    private IEnumerator Start()
    {
        if (openingLines == null || openingLines.Length == 0)
        {
            yield break;
        }

        yield return new WaitForSeconds(delay);

        DialogueManager dm = DialogueManager.Instance;

        if (dm == null || dm.IsOpen)
        {
            yield break;
        }

        dm.StartDialogue(openingLines);
    }
}
