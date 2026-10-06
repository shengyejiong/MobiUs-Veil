using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 第三幕：床边的两份资料（医院通知 + 主角事后记录）。
/// 读完标记 Act03Story.FactsRead = true —— 写名流程要先读它。
/// 结构与 InteractionTrigger 一致，只是多了一个"已读"标记。
/// </summary>
public class Act03Evidence : MonoBehaviour
{
    [Header("文本")]
    [SerializeField] private DialogueLine[] lines;        // 第一次读（医院通知 + 记录 + 主角沉默）
    [SerializeField] private DialogueLine[] againLines;   // 之后再读（可选）

    private bool playerInside;
    private bool wasDialogueOpen;
    private bool readOnce;

    private void Update()
    {
        DialogueManager dm = DialogueManager.Instance;

        if (dm == null)
        {
            InteractionPromptUI.Hide(this);
            return;
        }

        bool canInteract = playerInside && !dm.IsOpen;

        if (canInteract) InteractionPromptUI.Show(this, transform);
        else InteractionPromptUI.Hide(this);

        bool canPress = canInteract
            && Time.frameCount != dm.LastStateChangeFrame
            && Time.timeScale > 0f;

        if (canPress && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            DialogueLine[] use = (readOnce && againLines != null && againLines.Length > 0) ? againLines : lines;

            if (use == null || use.Length == 0)
            {
                Debug.LogWarning($"{name} 没有设置资料文本。");
            }
            else
            {
                Act03Story.FactsRead = true;      // ★ 标记"死亡事实已读"
                readOnce = true;

                Debug.Log("[第三幕] 死亡资料已读");

                InteractionPromptUI.Hide(this);
                dm.StartDialogue(use);
            }
        }

        bool isOpen = dm.IsOpen;
        if (wasDialogueOpen && !isOpen) { /* 读完了，什么都不用做 */ }
        wasDialogueOpen = isOpen;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.transform.root.CompareTag("Player")) playerInside = true;
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
