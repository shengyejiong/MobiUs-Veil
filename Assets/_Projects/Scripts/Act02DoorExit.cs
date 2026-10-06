using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 第二幕出口大门。
///   钟还没拨对 → 门打不开，主角说一句台词
///   钟拨对了   → 走近按 E，主角说一句台词 → 渐黑 → 第三幕
/// </summary>
public class Act02DoorExit : MonoBehaviour
{
    [Header("转场")]
    [SerializeField] private SceneTransition sceneTransition;

    [Header("台词")]
    [SerializeField] private DialogueLine[] lockedLines;   // 门还锁着时（"我刚才不是已经出去了吗？"）
    [SerializeField] private DialogueLine[] exitLines;     // 离开前那句（"那之后……我不是已经赶回来了吗？"）

    private bool playerInside;
    private bool wasDialogueOpen;
    private bool exitPending;

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
            Interact(dm);
        }

        bool isOpen = dm.IsOpen;
        if (wasDialogueOpen && !isOpen) OnDialogueFinished();
        wasDialogueOpen = isOpen;
    }

    private void Interact(DialogueManager dm)
    {
        // 还没拨对钟：门打不开
        if (!Act02Story.ClockSolved)
        {
            dm.StartDialogue(lockedLines);
            return;
        }

        // 拨对了：说完这句就进第三幕
        exitPending = true;
        dm.StartDialogue(exitLines);
    }

    private void OnDialogueFinished()
    {
        if (!exitPending) return;
        exitPending = false;

        // 离开第二幕前把本幕结果结算进跨幕契约
        GameState.CurrentAct = 3;

        if (sceneTransition != null)
        {
            sceneTransition.GoToNextScene();
        }
        else
        {
            Debug.LogWarning("[第二幕出口] 没有指定 SceneTransition，无法转场");
        }
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
