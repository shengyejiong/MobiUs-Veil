using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionTrigger : MonoBehaviour
{
    [SerializeField] private DialogueLine[] dialogueLines;
    [SerializeField] private GameObject prompt;

    private bool playerInside;

    // 玩家本次运行中是否已经完成过第一次交互
    private bool hasInteracted;

    private void Start()
    {
        if (prompt != null)
        {
            prompt.SetActive(false);
        }
    }

    private void Update()
    {
        DialogueManager dialogueManager = DialogueManager.Instance;

        if (dialogueManager == null)
        {
            return;
        }

        bool canInteract =
            playerInside &&
            !dialogueManager.IsOpen;

        // 只在第一次交互前显示提示
        bool shouldShowPrompt =
            canInteract &&
            !hasInteracted;

        if (prompt != null)
        {
            prompt.SetActive(shouldShowPrompt);
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

            if (prompt != null)
            {
                prompt.SetActive(false);
            }

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

            if (prompt != null)
            {
                prompt.SetActive(false);
            }
        }
    }
}