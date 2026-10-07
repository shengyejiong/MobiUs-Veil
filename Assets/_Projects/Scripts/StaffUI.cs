﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// 主菜单里的「Staff」按钮 —— 点开显示制作名单。
///
/// ★ 界面是运行时用代码生成的，所以场景里只要把这个脚本挂到按钮上就行，
///   不会破坏原有 UI。
/// ★ 会自动把本物体上的 Button 接到 Open()，不用手写 OnClick 事件。
///
/// 【改名字的地方】选中挂了这个脚本的物体 → Inspector → Staff UI 组件 →
///   策划 / 程序 / 美术 三个输入框，直接改文字即可。
/// </summary>
public class StaffUI : MonoBehaviour
{
    public static StaffUI Instance;

    [Header("字体（留空就用 TMP 默认字体）")]
    [SerializeField] private TMP_FontAsset font;

    [Header("★ 名单（就在这里改名字）")]
    [SerializeField] private string title = "STAFF";

    [Tooltip("策划")]
    [SerializeField] private string planning = "小a，小b";

    [Tooltip("程序")]
    [SerializeField] private string programming = "小c，小d";

    [Tooltip("美术")]
    [SerializeField] private string art = "小e，小f";

    [Header("外观")]
    [SerializeField] private Color panelColor = new Color(0.05f, 0.06f, 0.09f, 1f);
    [SerializeField] private Color titleColor = new Color(0.96f, 0.94f, 0.89f, 1f);
    [SerializeField] private Color textColor = new Color(0.88f, 0.88f, 0.90f, 1f);
    [SerializeField] private Color buttonColor = new Color(0.95f, 0.93f, 0.88f, 1f);
    [SerializeField] private Color buttonTextColor = new Color(0.10f, 0.11f, 0.15f, 1f);

    private GameObject panel;
    private TextMeshProUGUI line1, line2, line3;
    private bool built;

    public bool IsOpen => panel != null && panel.activeSelf;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Instance = null; }

    private void Awake()
    {
        Instance = this;
        BuildOnce();
        if (panel != null) panel.SetActive(false);

        // 自动接线：本物体上有 Button 就接上
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveListener(Open);
            btn.onClick.AddListener(Open);
            btn.interactable = true;
            Debug.Log("[Staff] 按钮已自动接到 Open()");
        }
    }

    private void Update()
    {
        if (!IsOpen) return;
        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) Close();
    }

    // ==================== 生成界面 ====================

    private void BuildOnce()
    {
        if (built) return;
        built = true;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) { Debug.LogError("[Staff] 找不到 Canvas"); return; }

        // ── 全屏面板（挡住后面的按钮，点背景也能关）──
        panel = NewUI("StaffPanel", canvas.transform, out RectTransform prt);
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.offsetMin = Vector2.zero;
        prt.offsetMax = Vector2.zero;
        prt.localScale = Vector3.one;
        Image bg = panel.AddComponent<Image>();
        bg.color = panelColor;
        bg.raycastTarget = true;
        Button bgBtn = panel.AddComponent<Button>();
        bgBtn.transition = Selectable.Transition.None;
        bgBtn.onClick.AddListener(Close);

        // ── 标题 ──
        GameObject t = NewUI("StaffTitle", panel.transform, out RectTransform trt);
        SetRect(trt, new Vector2(1200, 90), new Vector2(0, 260));
        TextMeshProUGUI tmpTitle = t.AddComponent<TextMeshProUGUI>();
        SetupText(tmpTitle, 60, titleColor, TextAlignmentOptions.Center);
        tmpTitle.text = title;

        // ── 三行名单 ──
        line1 = MakeLine("Line_Planning", new Vector2(0, 90), "策划：" + planning);
        line2 = MakeLine("Line_Programming", new Vector2(0, 10), "程序：" + programming);
        line3 = MakeLine("Line_Art", new Vector2(0, -70), "美术：" + art);

        TextMeshProUGUI assetCredit = MakeLine("Line_AssetCredit", new Vector2(0, -185),
            "PSX Decals Free by heyheythere - https://heyheythere.itch.io/psx-decals-free - CC BY 4.0\n"
            + "https://creativecommons.org/licenses/by/4.0/");
        assetCredit.fontSize = 18;

        // ── 返回按钮 ──
        GameObject btnGo = NewUI("StaffCloseButton", panel.transform, out RectTransform brt);
        SetRect(brt, new Vector2(240, 66), new Vector2(0, -300));
        Image bimg = btnGo.AddComponent<Image>();
        bimg.color = buttonColor;
        bimg.raycastTarget = true;
        Button bb = btnGo.AddComponent<Button>();
        bb.targetGraphic = bimg;
        bb.interactable = true;
        bb.onClick.AddListener(Close);

        GameObject btxt = NewUI("Text", btnGo.transform, out RectTransform btrt);
        btrt.anchorMin = Vector2.zero;
        btrt.anchorMax = Vector2.one;
        btrt.offsetMin = Vector2.zero;
        btrt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmpBtn = btxt.AddComponent<TextMeshProUGUI>();
        SetupText(tmpBtn, 24, buttonTextColor, TextAlignmentOptions.Center);
        tmpBtn.text = "返回";

        Debug.Log("[Staff] 界面已生成");
    }

    private TextMeshProUGUI MakeLine(string name, Vector2 pos, string content)
    {
        GameObject go = NewUI(name, panel.transform, out RectTransform rt);
        SetRect(rt, new Vector2(1200, 70), pos);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        SetupText(tmp, 34, textColor, TextAlignmentOptions.Center);
        tmp.text = content;
        return tmp;
    }

    private GameObject NewUI(string name, Transform parent, out RectTransform rt)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        return go;
    }

    private void SetRect(RectTransform rt, Vector2 size, Vector2 pos)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        rt.localScale = Vector3.one;
    }

    private void SetupText(TextMeshProUGUI tmp, float size, Color color, TextAlignmentOptions align)
    {
        if (font != null) tmp.font = font;
        else if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        tmp.text = "";
    }

    // ==================== 开关 ====================

    public void Open()
    {
        BuildOnce();
        // 每次打开都用 Inspector 里最新的名字（方便你随时改）
        if (line1 != null) line1.text = "策划：" + planning;
        if (line2 != null) line2.text = "程序：" + programming;
        if (line3 != null) line3.text = "美术：" + art;

        if (panel != null)
        {
            panel.transform.SetAsLastSibling();   // 强制置顶，彻底盖住主菜单
            panel.SetActive(true);
        }
        Time.timeScale = 1f;
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }
}
