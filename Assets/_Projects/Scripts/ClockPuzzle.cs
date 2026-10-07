using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 第二幕墙钟机关（白盒版）
///
///   ① 按 E → 打开时钟面板 → 三个预设时间（22:30 / 22:47 / 23:00）
///      选 22:47 → 钟响 + 门解锁 + 主角台词
///      选错   → 提示"那不是我们约好的时间。"，可以立刻重试
///   ② 解开之后再按 E → 取下这只小挂钟（白盒：直接拿走）
///      拿走 → 隐藏钟体、GameProgress 记录为"在背包"
///
/// 道具状态统一由 GameProgress（数据层）管理，这里只管表现。
/// 还没做 UI 也能测：Clock Panel 留空时，按 E 后弹出选项面板（鼠标点击 22:30 / 22:47 / 23:00）。
/// </summary>
public class ClockPuzzle : MonoBehaviour
{
    [Header("面板（留空 = 键盘 1/2/3 测试模式）")]
    [SerializeField] private GameObject clockPanel;
    [SerializeField] private TMP_Text feedbackText;

    [Header("首次调钟前的对白")]
    [SerializeField] private DialogueLine[] beforeAdjustLines;

    [Header("正确答案")]
    [SerializeField] private int correctHour = 22;
    [SerializeField] private int correctMinute = 47;

    [Header("滴答声（调到正确时间后停止）")]
    [SerializeField] private AudioSource tickingBgm;

    [Header("钟表外观")]
    [SerializeField] private SpriteRenderer clockVisual;
    [SerializeField] private Sprite initialClockSprite;
    [SerializeField] private Sprite solvedClockSprite;
    [SerializeField] private Vector3 loosenedOffset = new Vector3(0.15f, -0.12f, 0f);
    [SerializeField] private float loosenedAngle = -8f;

    [Header("成功反馈")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip unlockClip;
    [SerializeField] private DialogueLine[] successLines;

    [Header("取钟（白盒：直接拿走）")]
    [Tooltip("拿走之后要隐藏的物体（拖 WallClock）。留空 = 隐藏自己的父物体")]
    [SerializeField] private GameObject clockBodyToHide;

    [Tooltip("白盒：勾上 = 解开后按E直接拿走；以后做【拿走/暂时不拿】面板时改成 false")]
    [SerializeField] private bool autoTake = true;

    [SerializeField] private DialogueLine[] takeLines;            // 拿走时的文本
    [SerializeField] private DialogueLine[] afterSolveLookLines;  // 解开但还没拿时按 E 的提示

    [Header("文字")]
    [SerializeField] private string promptText = "把时间拨到她原定报平安的那一刻。";
    [SerializeField] private string wrongText = "那不是我们约好的时间。";

    [SerializeField]
    private string[] hintTexts =
    {
        "看看她为今晚留下的清单。",
        "手机里那条晚安，还没有发出去。",
        "约定是22:47。"
    };

    private bool playerInside;
    private bool wasDialogueOpen;
    private bool beforeAdjustPending;
    private bool successPending;
    private bool takePending;      // 对话结束后隐藏钟体
    private bool choosing;              // 选项面板是否开着
    private int hintIndex;
    private Vector3 initialVisualPosition;
    private Quaternion initialVisualRotation;

    /// <summary>挂钟是否已经不在墙上了（在背包里或已放到底座）。</summary>
    private static bool ClockTaken
        => GameProgress.GetState(StoryItemId.Clock) != StoryItemState.Uncollected;

    private void Awake()
    {
        if (clockVisual == null) return;
        initialVisualPosition = clockVisual.transform.localPosition;
        initialVisualRotation = clockVisual.transform.localRotation;
    }

    private void Start()
    {
        RefreshClockVisual(Act02Story.ClockSolved);
        if (Act02Story.ClockSolved && tickingBgm != null)
            tickingBgm.Stop();
    }

    private void Update()
    {
        DialogueManager dm = DialogueManager.Instance;

        // ---- 选项面板开着的时候：只等鼠标点击 ----
        if (choosing)
        {
            // ⚠️ 玩家直接走开 → 视为取消，随时可以回来重新调
            if (!playerInside)
            {
                Debug.Log("[墙钟] 你走开了 → 取消调时间（回来还能再拨）");
                ClosePanel();
                return;
            }

            return;
        }

        // ---- 面板开着的时候不做交互 ----
        if (clockPanel != null && clockPanel.activeSelf)
        {
            InteractionPromptUI.Hide(this);
            return;
        }

        if (dm == null)
        {
            InteractionPromptUI.Hide(this);
            return;
        }

        // 钟已经拿走了 → 这里没事可做了
        bool hasSomethingToDo = !ClockTaken;
        bool canInteract = playerInside && !dm.IsOpen && hasSomethingToDo && !beforeAdjustPending;

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
        // ① 还没解开：开面板调时间
        if (!Act02Story.ClockSolved)
        {
            if (!Act02Story.ClockIntroPlayed && beforeAdjustLines != null && beforeAdjustLines.Length > 0)
            {
                Act02Story.ClockIntroPlayed = true;
                beforeAdjustPending = true;
                dm.StartDialogue(beforeAdjustLines);
                return;
            }

            OpenPanel();
            return;
        }

        // ② 解开了、钟还在墙上：取钟
        if (ClockTaken)
        {
            dm.StartDialogue(afterSolveLookLines);
            return;
        }

        if (!autoTake)
        {
            // 以后这里接【拿走】【暂时不拿】面板
            dm.StartDialogue(afterSolveLookLines);
            return;
        }

        TakeClock(dm);
    }

    private void TakeClock(DialogueManager dm)
    {
        if (!GameProgress.TryCollect(StoryItemId.Clock))
        {
            Debug.Log("[墙钟] 取钟失败：挂钟已经被拿过了");
            dm.StartDialogue(afterSolveLookLines);
            return;
        }

        Debug.Log($"[墙钟] 已取下 22:47 小挂钟（状态 = {GameProgress.GetState(StoryItemId.Clock)}）");

        takePending = true;                 // 等对话结束再隐藏钟体
        dm.StartDialogue(takeLines);
    }

    // ================= 面板 =================

    public void OpenPanel()
    {
        hintIndex = 0;

        if (clockPanel != null)
        {
            clockPanel.SetActive(true);
            SetFeedback(promptText);
            Time.timeScale = 0f;                 // 面板打开时暂停（策划案要求）
            return;
        }

        // ---- 用画面上的选项面板（鼠标点击）----
        ChoiceUI ui = ChoiceUI.Get();
        if (ui == null)
        {
            Debug.LogWarning("[墙钟] 场景里没有 ChoiceUI 面板 —— 无法弹出选项");
            return;
        }

        choosing = true;
        Time.timeScale = 0f;                     // 拨钟时暂停（策划案要求）
        ui.Show("要把指针拨到几点？",
                new[] { "22:30", "22:47", "23:00" },
                idx =>
                {
                    choosing = false;
                    if (Time.timeScale == 0f) Time.timeScale = 1f;
                    if (idx == 0) ChooseTime(22, 30);
                    else if (idx == 1) ChooseTime(22, 47);
                    else ChooseTime(23, 0);
                });
    }

    public void ClosePanel()
    {
        if (clockPanel != null) clockPanel.SetActive(false);

        ChoiceUI ui = ChoiceUI.Get();
        if (ui != null && ui.IsOpen) ui.Hide();

        choosing = false;

        if (Time.timeScale == 0f) Time.timeScale = 1f;   // 别把游戏永久暂停了
    }

    /// <summary>面板上三个按钮分别绑：(22,30) / (22,47) / (23,00)</summary>
    public void ChooseTime(int hour, int minute)
    {
        if (Act02Story.ClockSolved) return;

        if (hour == correctHour && minute == correctMinute)
        {
            Solve();
            return;
        }

        SetFeedback(wrongText);

        if (choosing)
        {
            Debug.Log($"[墙钟] {hour:00}:{minute:00} → {wrongText}");
        }
    }

    // ⚠️ Unity 的 Button.onClick 只能传一个参数，所以三个按钮绑下面这三个无参方法
    public void Choose2230() => ChooseTime(22, 30);
    public void Choose2247() => ChooseTime(22, 47);
    public void Choose2300() => ChooseTime(23, 0);

    /// <summary>需要提示时接一个「提示」按钮：按顺序给三条</summary>
    public void ShowHint()
    {
        if (hintTexts == null || hintTexts.Length == 0) return;

        int index = Mathf.Clamp(hintIndex, 0, hintTexts.Length - 1);
        SetFeedback(hintTexts[index]);
        hintIndex++;
    }

    // ================= 内部 =================

    private void Solve()
    {
        Act02Story.ClockSolved = true;
        RefreshClockVisual(true);
        if (tickingBgm != null) tickingBgm.Stop();
        ClosePanel();                            // 先恢复 timeScale，否则对话不会推进

        if (audioSource != null && unlockClip != null)
        {
            audioSource.PlayOneShot(unlockClip);
        }

        DialogueManager dm = DialogueManager.Instance;

        if (dm != null && successLines != null && successLines.Length > 0)
        {
            successPending = true;
            dm.StartDialogue(successLines);
        }

        Debug.Log("[墙钟] 已拨到 22:47 → 门解锁");
    }

    private void RefreshClockVisual(bool solved)
    {
        if (clockVisual == null) return;

        Sprite sprite = solved ? solvedClockSprite : initialClockSprite;
        if (sprite != null) clockVisual.sprite = sprite;

        Transform visualTransform = clockVisual.transform;
        visualTransform.localPosition = initialVisualPosition + (solved ? loosenedOffset : Vector3.zero);
        visualTransform.localRotation = initialVisualRotation
            * Quaternion.Euler(0f, 0f, solved ? loosenedAngle : 0f);
    }

    private void SetFeedback(string text)
    {
        if (feedbackText != null) feedbackText.text = text;
    }

    private void OnDialogueFinished()
    {
        if (beforeAdjustPending)
        {
            beforeAdjustPending = false;
            if (playerInside && !Act02Story.ClockSolved && !ClockTaken)
                OpenPanel();
            return;
        }

        if (successPending)
        {
            successPending = false;
            Debug.Log("[墙钟] 成功反馈结束 —— 现在按 E 可以取下这只小挂钟");
            return;
        }

        if (takePending)
        {
            takePending = false;
            HideClockBody();
        }
    }

    /// <summary>拿走之后把墙上的钟体藏起来（以后美术给了"空挂点"，就换成隐藏钟体 + 显示空挂点）</summary>
    private void HideClockBody()
    {
        InteractionPromptUI.Hide(this);

        GameObject target = clockBodyToHide;

        if (target == null && transform.parent != null)
        {
            target = transform.parent.gameObject;
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
