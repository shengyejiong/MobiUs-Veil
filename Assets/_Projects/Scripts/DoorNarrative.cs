using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DoorNarrative : MonoBehaviour
{
    [Header("对话内容（Inspector 里填，见第七步的表格）")]
    [SerializeField] private DialogueLine[] overnightLines;      // 过夜 4 句
    [SerializeField] private DialogueLine requestLine;           // 请求扣窗（1 句）
    [SerializeField] private DialogueLine[] alreadyClosedLines;  // 已经扣好时的回应
    [SerializeField] private DialogueLine[] reminderLines;       // 只提醒（1 句）
    [SerializeField] private DialogueLine[] farewellLines;       // 告别 3 句

    [Header("第一幕结束")]
    [SerializeField] private SceneTransition sceneTransition;    // 拖 GameFlow 过来
    [SerializeField] private bool autoTransitionAfterFarewell = true;

    private bool playerInside;
    private bool wasDialogueOpen;   // 用来检测"对话刚刚结束"
    
    private bool farewellPending;   // 新增：门自己刚开了告别对话

    private void Update()
    {
        DialogueManager dm = DialogueManager.Instance;

        if (dm == null)
        {
            InteractionPromptUI.Hide(this);
            return;
        }

        // 还有话说的时候才显示提示；全说完就不再打扰玩家
        bool stillHasSomething = !(Act01Story.DoorTalked && Act01Story.WindowClosed && Act01Story.FarewellDone);

        bool canInteract = playerInside && !dm.IsOpen && stillHasSomething;

        // 显示/归还共享提示（和 InteractionTrigger 里的写法一样）
        if (canInteract) InteractionPromptUI.Show(this, transform);
        else InteractionPromptUI.Hide(this);

        // ⚠️ 防抖三条件：和 InteractionTrigger 里保持一致，否则按 E 开对话的同一帧会被立刻翻到第二句
        bool canPress = canInteract
            && Time.frameCount != dm.LastStateChangeFrame
            && Time.timeScale > 0f;

        if (canPress && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Interact(dm);
        }

        // 对话刚结束的那一帧
        bool isOpen = dm.IsOpen;
        if (wasDialogueOpen && !isOpen) OnDialogueFinished();
        wasDialogueOpen = isOpen;
    }

    private void Interact(DialogueManager dm)
    {
        // ① 第一次：过夜那段（+ 请求扣窗 / 或"已经扣好了"）
        if (!Act01Story.DoorTalked)
        {
            Act01Story.DoorTalked = true;

            List<DialogueLine> lines = new List<DialogueLine>();

            if (overnightLines != null) lines.AddRange(overnightLines);

            if (Act01Story.WindowClosed)
            {
                // 罕见分支：已经扣过窗了（留着不吃亏，策划以后改流程也不用动代码）
                if (alreadyClosedLines != null) lines.AddRange(alreadyClosedLines);
            }
            else
            {
                Act01Story.WindowRequested = true;
                if (requestLine != null) lines.Add(requestLine);
            }

            dm.StartDialogue(lines.ToArray());
            return;
        }

        // ② 说过了但窗还没扣：只提醒一句
        if (!Act01Story.WindowClosed)
        {
            dm.StartDialogue(reminderLines);
            return;
        }


        // ④ 全部说完：什么都不做

        if (!Act01Story.FarewellDone)
        {
             Act01Story.FarewellDone = true;
             farewellPending = true;                   
             dm.StartDialogue(farewellLines);
             return;
        }
    }

    private void OnDialogueFinished()
{
    if (!farewellPending) return;   // 不是门自己开的对话，一律不理会
    farewellPending = false;

    if (autoTransitionAfterFarewell && sceneTransition != null)
    {   
        GameState.Act1WindowClosed = Act01Story.WindowClosed;   // 记录窗户状态
        GameState.CurrentAct = 2;   // 记录当前幕数
        sceneTransition.GoToNextScene();
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