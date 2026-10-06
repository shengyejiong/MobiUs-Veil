using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Galgame 风格的选项面板：鼠标点击选择（支持 2~4 个选项）
///
/// 用法（任意脚本里）：
///   ChoiceUI.Get().Show("……要拿走吗？", new[] { "拿走", "暂时不拿" }, idx =&gt; { ... });
/// 面板打开时会锁定角色移动，并可通过 ChoiceUI.Get().IsOpen 判断（例如追逐战暂停）。
/// </summary>
public class ChoiceUI : MonoBehaviour
{
    public static ChoiceUI Instance;

    [Header("面板")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI questionText;

    [Header("选项（按顺序填，用不到的会自动隐藏）")]
    [SerializeField] private Button[] optionButtons = new Button[4];
    [SerializeField] private TextMeshProUGUI[] optionLabels = new TextMeshProUGUI[4];

    private Action<int> onChosen;
    private PlayerMovement playerMovement;

    /// <summary>面板是否开着</summary>
    public bool IsOpen { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    private void Awake()
    {
        Instance = this;
        if (panel == null) panel = gameObject;
        playerMovement = FindFirstObjectByType<PlayerMovement>();
        panel.SetActive(false);
    }

    /// <summary>给每个按钮挂上点击回调（每次 Show 都重挂，防止预制体/克隆带来的各种怪问题）</summary>
    private void BindButtons()
    {
        for (int i = 0; i < optionButtons.Length; i++)
        {
            Button b = optionButtons[i];
            if (b == null) continue;

            b.onClick.RemoveAllListeners();
            int idx = i;                       // 闭包捕获
            b.onClick.AddListener(() => Choose(idx));

            // ★ 双保险：确保按钮真的能点（模板可能是禁用状态的按钮）
            b.interactable = true;
            if (b.targetGraphic != null) b.targetGraphic.raycastTarget = true;
        }
    }

    /// <summary>兜底获取（场景里没手动挂也能找到）</summary>
    public static ChoiceUI Get()
    {
        if (Instance != null) return Instance;
        Instance = FindFirstObjectByType<ChoiceUI>(FindObjectsInactive.Include);
        return Instance;
    }

    /// <summary>弹出选项。options 最多 4 个；选完后回调 index（0 起）</summary>
    public void Show(string question, string[] options, Action<int> onChosen)
    {
        if (options == null || options.Length == 0) return;

        this.onChosen = onChosen;
        if (questionText != null) questionText.text = question;

        BindButtons();

        for (int i = 0; i < optionButtons.Length; i++)
        {
            bool use = i < options.Length;
            if (optionButtons[i] != null) optionButtons[i].gameObject.SetActive(use);
            if (use && i < optionLabels.Length && optionLabels[i] != null) optionLabels[i].text = options[i];
        }

        if (panel != null) panel.SetActive(true);
        IsOpen = true;

        if (playerMovement == null) playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null) playerMovement.SetMovementLocked(true);

        Debug.Log("[选项] " + question + "  →  " + string.Join("  /  ", options));
    }

    /// <summary>键盘兜底：万一鼠标点不了（UI 被挡等），还能用 1~4 选、Esc 取消，不至于卡死</summary>
    private void Update()
    {
        if (!IsOpen) return;
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        int count = 0;
        for (int i = 0; i < optionButtons.Length; i++) { if (optionButtons[i] != null && optionButtons[i].gameObject.activeSelf) count++; }

        if (kb.digit1Key.wasPressedThisFrame && count >= 1) Choose(0);
        else if (kb.digit2Key.wasPressedThisFrame && count >= 2) Choose(1);
        else if (kb.digit3Key.wasPressedThisFrame && count >= 3) Choose(2);
        else if (kb.digit4Key.wasPressedThisFrame && count >= 4) Choose(3);
        else if (kb.escapeKey.wasPressedThisFrame) Choose(count - 1 < 0 ? 0 : count - 1);
    }

    private void Choose(int index)
    {
        if (!IsOpen) return;
        Action<int> cb = onChosen;
        Debug.Log("[选项] 玩家点了第 " + (index + 1) + " 项");
        Hide();
        if (cb != null) cb(index);
    }

    /// <summary>关闭面板（不回调）</summary>
    public void Hide()
    {
        IsOpen = false;
        onChosen = null;
        if (panel != null) panel.SetActive(false);
        if (playerMovement != null) playerMovement.SetMovementLocked(false);
    }
}
