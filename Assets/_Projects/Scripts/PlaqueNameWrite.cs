using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 第三幕：床边的纪念牌 —— 本幕的核心动作。
///
/// 固定顺序（策划案 323 行）：
///   读资料 → 写名 → 基础信息 → 是否拿走 → 关闭界面 →（建立追逐检查点）
///
/// 白盒说明：写名/拿取用画面上的选项面板（ChoiceUI，鼠标点击），
///           玩家走开则视为"暂时不写 / 暂时不拿"，随时可以回来重新调查。
/// </summary>
public class PlaqueNameWrite : MonoBehaviour
{
    [Header("文本")]
    [SerializeField] private DialogueLine[] needFactsLines;    // 还没读资料：把你引向资料
    [SerializeField] private DialogueLine[] askNameLines;      // "名字，我一直都记得。"
    [SerializeField] private DialogueLine[] afterWriteLines;   // "可我一直没肯把它写在这里。" + 基础信息
    [SerializeField] private DialogueLine[] askTakeLines;      // 拿取询问前的一句（可留空）

    [Header("表现")]
    [Tooltip("写名后要换色的纪念牌（一般就是父物体的 SpriteRenderer）")]
    [SerializeField] private SpriteRenderer plaqueRenderer;
    [SerializeField] private Color blankColor = new Color(0.42f, 0.33f, 0.24f, 1f);
    [SerializeField] private Color writtenColor = new Color(0.72f, 0.58f, 0.32f, 1f);
    [Tooltip("拿走之后要隐藏的物体。留空 = 自动往上找带 SpriteRenderer 的那层")]
    [SerializeField] private GameObject objectToHide;

    [Header("拿取")]
    [SerializeField] private StoryItemId itemId = StoryItemId.MemorialPlaque;

    private bool playerInside;
    private bool wasDialogueOpen;
    private bool pendingFactsThenAsk;  // 先在纪念牌这里读了资料 → 接着问要不要写名
    private bool pendingWrite;         // 对话结束 → 进入"写名"选择
    private bool pendingTakeAsk;       // 对话结束 → 进入"是否拿走"选择
    private int choiceMode;            // 0=无 1=写名 2=是否拿走

    /// <summary>纪念牌是不是已经不在场上了（拿走了）</summary>
    private static bool Taken =>
        Act03Story.PlaqueTaken || GameProgress.GetState(StoryItemId.MemorialPlaque) != StoryItemState.Uncollected;

    private void Start()
    {
        if (plaqueRenderer != null) plaqueRenderer.color = Act03Story.NameWritten ? writtenColor : blankColor;
    }

    private void Update()
    {
        // ---- 选择中：只等面板回调 ----
        // ⚠️ 玩家如果直接走开，就当作"暂时不写 / 暂时不拿"，随时可以回来重新调查
        if (choiceMode != 0)
        {
            if (!playerInside)
            {
                Debug.Log(choiceMode == 1
                    ? "[纪念牌] 你走开了 → 视为暂时不写（回来还能写）"
                    : "[纪念牌] 你走开了 → 视为暂时不拿（回来还能拿）");
                ChoiceUI ui = ChoiceUI.Get();
                if (ui != null && ui.IsOpen) ui.Hide();
                choiceMode = 0;
                return;
            }

            return;
        }

        DialogueManager dm = DialogueManager.Instance;

        if (dm == null)
        {
            InteractionPromptUI.Hide(this);
            return;
        }

        bool hasSomethingToDo = !Taken;
        bool canInteract = playerInside && !dm.IsOpen && hasSomethingToDo;

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

    // ================= 交互 =================

    private void Interact(DialogueManager dm)
    {
        // ① 还没读资料 → 先在纪念牌这里把资料读完（策划案 297 行），再接着问写名
        if (!Act03Story.FactsRead)
        {
            Act03Story.FactsRead = true;
            pendingFactsThenAsk = true;
            Debug.Log("[纪念牌] 先读资料（在纪念牌这里读）");
            dm.StartDialogue(needFactsLines);
            return;
        }

        // ② 读过资料、还没写名 → 问要不要写
        if (!Act03Story.NameWritten)
        {
            pendingWrite = true;
            dm.StartDialogue(askNameLines);
            return;
        }

        // ③ 写过名、还没拿走 → 基础信息 + 询问是否拿走
        if (askTakeLines == null || askTakeLines.Length == 0)
        {
            ShowTakeChoice();
            return;
        }

        pendingTakeAsk = true;
        dm.StartDialogue(askTakeLines);
    }

    private void OnDialogueFinished()
    {
        DialogueManager dm = DialogueManager.Instance;

        // 资料读完 → 接着问写名
        if (pendingFactsThenAsk)
        {
            pendingFactsThenAsk = false;
            pendingWrite = true;
            if (dm != null) dm.StartDialogue(askNameLines);
            return;
        }

        if (pendingWrite)
        {
            pendingWrite = false;
            ShowWriteChoice();
            return;
        }

        if (pendingTakeAsk)
        {
            pendingTakeAsk = false;
            ShowTakeChoice();
        }
    }

    /// <summary>弹出"写名"选项（鼠标点击）</summary>
    private void ShowWriteChoice()
    {
        choiceMode = 1;
        ChoiceUI ui = ChoiceUI.Get();
        if (ui == null)
        {
            Debug.LogWarning("[纪念牌] 场景里没有 ChoiceUI 面板 —— 无法弹出选项");
            choiceMode = 0;
            return;
        }
        ui.Show("要在这里写下她的名字吗？",
                new[] { "写下「林晚」", "暂时不写" },
                idx => { if (idx == 0) ConfirmChoice(); else CancelChoice(); });
    }

    /// <summary>弹出"是否拿走"选项（鼠标点击）</summary>
    private void ShowTakeChoice()
    {
        choiceMode = 2;
        ChoiceUI ui = ChoiceUI.Get();
        if (ui == null)
        {
            Debug.LogWarning("[纪念牌] 场景里没有 ChoiceUI 面板 —— 无法弹出选项");
            choiceMode = 0;
            return;
        }
        ui.Show("要把这块纪念牌带走吗？",
                new[] { "拿走", "暂时不拿" },
                idx => { if (idx == 0) ConfirmChoice(); else CancelChoice(); });
    }

    private void ConfirmChoice()
    {
        DialogueManager dm = DialogueManager.Instance;

        if (choiceMode == 1)
        {
            choiceMode = 0;

            Act03Story.NameWritten = true;
            if (plaqueRenderer != null) plaqueRenderer.color = writtenColor;

            Debug.Log("[纪念牌] 已写下：林晚");

            if (dm != null) dm.StartDialogue(afterWriteLines);

            // 写完这一句之后，接着弹"是否拿走"
            pendingTakeAsk = true;
            return;
        }

        if (choiceMode == 2)
        {
            choiceMode = 0;
            Act03Story.PlaqueAsked = true;     // ★ 询问答完了 → 追逐检查点建立、黑影可以出现

            if (GameProgress.TryCollect(itemId))
            {
                Act03Story.PlaqueTaken = true;
                Debug.Log($"[纪念牌] 已拿走（状态 = {GameProgress.GetState(itemId)}）");
                HidePlaque();
            }
            else
            {
                Debug.Log("[纪念牌] 拿取失败：已经被拿过了");
            }
        }
    }

    private void CancelChoice()
    {
        if (choiceMode == 1)
        {
            Debug.Log("[纪念牌] 暂时不写 —— 之后回来还能写");
        }
        else
        {
            // 选「暂时不拿」也算询问结束 → 同样建立追逐检查点（策划案 322 行：两种选择都继续）
            Debug.Log("[纪念牌] 暂时不拿 —— 牌子留在床边");
            Act03Story.PlaqueAsked = true;
        }

        choiceMode = 0;
        pendingWrite = false;
        pendingTakeAsk = false;
        pendingFactsThenAsk = false;
    }

    private void HidePlaque()
    {
        InteractionPromptUI.Hide(this);

        GameObject target = objectToHide;
        if (target == null)
        {
            Transform current = transform;
            while (current != null)
            {
                if (current.GetComponent<SpriteRenderer>() != null) { target = current.gameObject; break; }
                current = current.parent;
            }
        }

        if (target != null) target.SetActive(false);
        else gameObject.SetActive(false);
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
