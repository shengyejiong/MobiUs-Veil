using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 第二幕墙钟机关（白盒版）
///
///   按 E → 打开时钟面板 → 三个预设时间（22:30 / 22:47 / 23:00）
///   选 22:47 → 钟响 + 门解锁 + 主角台词
///   选错   → 提示"那不是我们约好的时间。"，可以立刻重试
///
/// 还没做 UI 也能测：把 Clock Panel 留空，按 E 后用键盘 1 / 2 / 3 选择（Q 取消）。
/// </summary>
public class ClockPuzzle : MonoBehaviour
{
    [Header("面板（留空 = 键盘 1/2/3 测试模式）")]
    [SerializeField] private GameObject clockPanel;
    [SerializeField] private TMP_Text feedbackText;

    [Header("正确答案")]
    [SerializeField] private int correctHour = 22;
    [SerializeField] private int correctMinute = 47;

    [Header("成功反馈")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip unlockClip;
    [SerializeField] private DialogueLine[] successLines;

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
    private bool successPending;
    private bool keyboardChoosing;
    private int hintIndex;

    private void Update()
    {
        DialogueManager dm = DialogueManager.Instance;

        // ---- 键盘测试模式（没做 UI 时用）----
        if (keyboardChoosing)
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.digit1Key.wasPressedThisFrame) ChooseTime(22, 30);
            else if (Keyboard.current.digit2Key.wasPressedThisFrame) ChooseTime(22, 47);
            else if (Keyboard.current.digit3Key.wasPressedThisFrame) ChooseTime(23, 0);
            else if (Keyboard.current.qKey.wasPressedThisFrame) ClosePanel();

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

        // 解开之后不再需要交互（以后要"取钟"就用 PickupItem，不走这里）
        bool canInteract = playerInside && !dm.IsOpen && !Act02Story.ClockSolved;

        if (canInteract) InteractionPromptUI.Show(this, transform);
        else InteractionPromptUI.Hide(this);

        bool canPress = canInteract
            && Time.frameCount != dm.LastStateChangeFrame
            && Time.timeScale > 0f;

        if (canPress && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            OpenPanel();
        }

        bool isOpen = dm.IsOpen;
        if (wasDialogueOpen && !isOpen) OnDialogueFinished();
        wasDialogueOpen = isOpen;
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
        }
        else
        {
            keyboardChoosing = true;
            Debug.Log("[墙钟] 未设置面板 → 键盘测试模式：1 = 22:30，2 = 22:47，3 = 23:00，Q = 取消");
        }
    }

    public void ClosePanel()
    {
        if (clockPanel != null) clockPanel.SetActive(false);
        keyboardChoosing = false;

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

        if (keyboardChoosing)
        {
            Debug.Log($"[墙钟] {hour:00}:{minute:00} → {wrongText}");
        }
    }

    // ⚠️ Unity 的 Button.onClick 在 Inspector 里【只能传一个参数】，
    //    所以做 UI 时三个按钮分别绑下面这三个无参方法（它们不显示在 Inspector 字段里，只作为按钮入口）
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

    private void SetFeedback(string text)
    {
        if (feedbackText != null) feedbackText.text = text;
    }

    private void OnDialogueFinished()
    {
        if (!successPending) return;
        successPending = false;

        // 策划案的下一步（【拿走】【暂时不拿】取钟询问）以后接在这里
        Debug.Log("[墙钟] 成功反馈结束 —— 下一步可以接「是否取下这只小挂钟？」");
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
