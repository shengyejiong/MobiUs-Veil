using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionTrigger : MonoBehaviour
{
    [SerializeField] private DialogueLine[] dialogueLines;

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
            !dialogueManager.IsOpen;

        // 只在第一次交互前显示提示
        bool shouldShowPrompt =
            canInteract &&
            !hasInteracted;

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

            // 第一次或之后再次按 E，都从第一句开始
            hasInteracted = true;

            InteractionPromptUI.Hide(this);

            dialogueManager.StartDialogue(dialogueLines);
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
