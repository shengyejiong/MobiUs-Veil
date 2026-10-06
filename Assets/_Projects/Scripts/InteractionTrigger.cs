using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionTrigger : MonoBehaviour
{
    [SerializeField] private DialogueLine[] dialogueLines;

    [Tooltip("第二次及以后再调查时显示的文本（留空 = 一直用上面那套）。配了它以后提示会一直显示，可以反复调查")]
    [SerializeField] private DialogueLine[] secondLines;

    [Tooltip("勾上 = 提示一直显示、可以反复交谈（NPC 闲聊这类）；不勾 = 只在第一次交互前提示（调查物品）")]
    [SerializeField] private bool repeatable;

    private bool playerInside;

    // 玩家本次运行中是否已经完成过第一次交互
    private bool hasInteracted;

    // 提示（Press E to react）由场景里唯一的 InteractionPromptUI 统一管理：
    // 这里只需要"申请显示"或"归还"，不用再手动拖 prompt 引用。
    // 场景里可以有很多个 InteractionTrigger，它们共用同一个提示，先到先得。

    private void Update()
    {
        DialogueManager dialogueManager = DialogueManager.Instance;

        if (dialogueManager == null)
        {
            InteractionPromptUI.Hide(this);
            return;
        }

        bool canInteract =
            playerInside &&
            !dialogueManager.IsOpen &&
            Time.frameCount!= dialogueManager.LastStateChangeFrame &&
            Time.timeScale > 0f;

        // 提示：一次性物体只在第一次交互前显示；
        // 勾了 repeatable（NPC 闲聊）或配了 secondLines（第二次不一样，比如"合照背面"）时一直显示
        bool alwaysAvailable = repeatable || (secondLines != null && secondLines.Length > 0);

        bool shouldShowPrompt =
            canInteract &&
            (alwaysAvailable || !hasInteracted);

        if (shouldShowPrompt)
        {
            InteractionPromptUI.Show(this, transform);
        }
        else
        {
            InteractionPromptUI.Hide(this);
        }

        if (!canInteract)
        {
            return;
        }

        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (dialogueLines == null || dialogueLines.Length == 0)
            {
                Debug.LogWarning($"{name} 没有设置对话内容。");
                return;
            }

            // 第一次用 dialogueLines；之后再调查，如果配了 secondLines 就换成它
            bool useSecond = hasInteracted && secondLines != null && secondLines.Length > 0;
            DialogueLine[] lines = useSecond ? secondLines : dialogueLines;

            hasInteracted = true;

            InteractionPromptUI.Hide(this);

            dialogueManager.StartDialogue(lines);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.transform.root.CompareTag("Player"))
        {
            playerInside = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.transform.root.CompareTag("Player"))
        {
            playerInside = false;

            InteractionPromptUI.Hide(this);
        }
    }
}
