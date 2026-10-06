﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>一个分类 = 一个文件夹 = 一屏（放不下自动分页）</summary>
[Serializable]
public class GalleryCategory
{
    [Tooltip("Resources/Gallery/ 下的文件夹名")]
    public string folder;

    [Tooltip("屏幕上显示的标题")]
    public string title;

    [Tooltip("每屏显示几张（普通 6 张，道具与环境 8 张）")]
    public int perPage = 6;

    [Tooltip("每屏几列（普通 3 列，道具与环境 4 列）")]
    public int columns = 3;
}

/// <summary>
/// 主菜单里的「Animation Enjoy」—— 概念美术 / 动画欣赏。
///
/// ★ 整个界面（面板、标题、页码、8 个格子、3 个按钮）都是**运行时用代码生成**的，
///   所以场景里只需要挂一个空物体 + 这个脚本就行，绝不会破坏原有 UI。
///
/// · 每个文件夹一屏：上面三张下面三张（道具与环境 4 列 × 2 行 = 8 张）
/// · 放不下自动分页：← → 或点按钮翻页，翻到头自动切下一个分类
/// · ESC / 返回按钮关闭；主菜单里按 G 键也能打开
/// </summary>
public class GalleryUI : MonoBehaviour
{
    public static GalleryUI Instance;

    [Header("字体（拖项目里的中文字体，例如 CJK SDF）")]
    [SerializeField] private TMP_FontAsset font;

    [Header("分类（顺序就是显示顺序）")]
    [SerializeField] private GalleryCategory[] categories;

    [Header("外观")]
    [SerializeField] private Color panelColor = new Color(0.05f, 0.06f, 0.09f, 1f);
    [SerializeField] private Color titleColor = new Color(0.96f, 0.94f, 0.89f, 1f);
    [SerializeField] private Color pageColor = new Color(0.78f, 0.80f, 0.84f, 1f);
    [SerializeField] private Color buttonColor = new Color(0.95f, 0.93f, 0.88f, 1f);
    [SerializeField] private Color buttonTextColor = new Color(0.10f, 0.11f, 0.15f, 1f);
    [SerializeField] private Vector2 gridSize = new Vector2(1560f, 600f);
    [SerializeField] private float cellGap = 16f;

    private GameObject panel;
    private Image[] slots;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI pageText;
    private Button prevButton;
    private Button nextButton;
    private Button closeButton;

    private Sprite[][] sprites;
    private int catIndex;
    private int page;
    private bool built;

    public bool IsOpen => panel != null && panel.activeSelf;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Instance = null; }

    private void Awake()
    {
        Instance = this;
        ApplyDefaultCategories();       // 场景里没填分类表就用内置的
        BuildOnce();
        if (panel != null) panel.SetActive(false);
        AutoWireSelfButton();           // 本物体上若有 Button，自动接上 Open()
    }

    /// <summary>
    /// 兜底：如果这个物体本身就是个按钮（比如复制 QuitButton 做的），
    /// 就自动把它的点击接到 Open() 上 —— 不依赖场景里手写的 OnClick 事件，
    /// 免得事件接线出一点问题按钮就点不动。
    /// </summary>
    private void AutoWireSelfButton()
    {
        Button self = GetComponent<Button>();
        if (self == null) return;

        self.onClick.RemoveListener(Open);   // 防重复
        self.onClick.AddListener(Open);
        self.interactable = true;
        Debug.Log("[画廊] 已自动把本物体上的 Button 接到 Open()");
    }

    /// <summary>
    /// 场景里 categories 留空时，用这份内置分类表 ——
    /// 这样只需要把本组件挂到一个物体上就能用，不用在 Inspector 里一项项填。
    /// </summary>
    private void ApplyDefaultCategories()
    {
        if (categories != null && categories.Length > 0) return;

        string[] folders =
        {
            "01_SideWalk", "02_FrontPortrait", "03_FrontWalk", "04_HeroinePortrait",
            "05_Bag", "06_BackWalk", "07_CharacterShow",
            "09_Ghost", "10_Tiles", "11_Props"
        };
        string[] titles =
        {
            "侧面走路", "正面立绘", "正面走的动画", "爱人与立绘",
            "背书包", "背后走路", "角色展示",
            "ghost", "tiles", "道具与环境"
        };

        categories = new GalleryCategory[folders.Length];
        for (int i = 0; i < folders.Length; i++)
        {
            categories[i] = new GalleryCategory();
            categories[i].folder = folders[i];
            categories[i].title = titles[i];
            bool big = folders[i] == "11_Props";
            categories[i].perPage = big ? 8 : 6;
            categories[i].columns = big ? 4 : 3;
        }
        Debug.Log("[画廊] 使用内置分类表，共 " + categories.Length + " 类");
    }

    public static GalleryUI Get()
    {
        if (Instance != null) return Instance;
        Instance = FindFirstObjectByType<GalleryUI>(FindObjectsInactive.Include);
        return Instance;
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (!IsOpen)
        {
            // 主菜单里按 G 也能打开（万一按钮没接上）
            if (kb.gKey.wasPressedThisFrame) Open();
            return;
        }

        if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) PrevPage();
        else if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) NextPage();
        else if (kb.escapeKey.wasPressedThisFrame) Close();
    }

    // ==================== 生成界面 ====================

    private void BuildOnce()
    {
        if (built) return;
        built = true;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) { Debug.LogError("[画廊] 找不到 Canvas"); return; }

        // ── 面板 ──
        panel = NewUI("GalleryPanel", canvas.transform, out RectTransform prt);
        Stretch(prt);
        Image pimg = panel.AddComponent<Image>();
        pimg.color = panelColor;
        pimg.raycastTarget = true;

        // ── 标题 ──
        GameObject titleGo = NewUI("GalleryTitle", panel.transform, out RectTransform trt);
        SetRect(trt, new Vector2(1300, 70), new Vector2(0, 430));
        titleText = titleGo.AddComponent<TextMeshProUGUI>();
        SetupText(titleText, 46, titleColor, TextAlignmentOptions.Center);

        // ── 页码 ──
        GameObject pageGo = NewUI("GalleryPage", panel.transform, out RectTransform pgrt);
        SetRect(pgrt, new Vector2(1300, 46), new Vector2(0, 378));
        pageText = pageGo.AddComponent<TextMeshProUGUI>();
        SetupText(pageText, 26, pageColor, TextAlignmentOptions.Center);

        // ── 8 个图片格子 ──
        slots = new Image[8];
        for (int i = 0; i < slots.Length; i++)
        {
            GameObject slotGo = NewUI("Slot" + i, panel.transform, out RectTransform srt);
            SetRect(srt, new Vector2(400, 250), Vector2.zero);
            Image img = slotGo.AddComponent<Image>();
            img.color = Color.white;
            img.preserveAspect = true;
            img.raycastTarget = false;
            slots[i] = img;
        }

        // ── 三个按钮 ──
        prevButton = MakeButton("prevButton", "上一页", new Vector2(-560, -450));
        nextButton = MakeButton("nextButton", "下一页", new Vector2(560, -450));
        closeButton = MakeButton("closeButton", "返回", new Vector2(0, -450));

        prevButton.onClick.AddListener(PrevPage);
        nextButton.onClick.AddListener(NextPage);
        closeButton.onClick.AddListener(Close);

        EnsureLoaded();
    }

    private GameObject NewUI(string name, Transform parent, out RectTransform rt)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;                       // UI 层
        rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        return go;
    }

    private void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
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
        // 字体没拖就用 TMP 默认字体兜底（中文靠系统的 fallback 字体链渲染）
        if (font != null) tmp.font = font;
        else if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        tmp.text = "";
    }

    private Button MakeButton(string name, string label, Vector2 pos)
    {
        GameObject go = NewUI(name, panel.transform, out RectTransform rt);
        SetRect(rt, new Vector2(240, 66), pos);

        Image img = go.AddComponent<Image>();
        img.color = buttonColor;
        img.raycastTarget = true;

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.interactable = true;

        GameObject textGo = NewUI("Text", go.transform, out RectTransform trt);
        Stretch(trt);
        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        SetupText(tmp, 24, buttonTextColor, TextAlignmentOptions.Center);
        tmp.text = label;

        return btn;
    }

    // ==================== 开关 / 翻页 ====================

    public void Open()
    {
        BuildOnce();
        if (panel == null) return;

        catIndex = 0;
        page = 0;
        while (catIndex < categories.Length && (sprites[catIndex] == null || sprites[catIndex].Length == 0)) catIndex++;
        if (catIndex >= categories.Length) catIndex = 0;

        panel.transform.SetAsLastSibling();   // 强制置顶，彻底盖住主菜单
        panel.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        Debug.Log("[画廊] 关闭");
    }

    public void NextPage()
    {
        if (categories == null || categories.Length == 0) return;
        page++;
        if (page >= PageCount(catIndex)) { catIndex = NextNonEmpty(catIndex); page = 0; }
        Refresh();
    }

    public void PrevPage()
    {
        if (categories == null || categories.Length == 0) return;
        page--;
        if (page < 0) { catIndex = PrevNonEmpty(catIndex); page = Mathf.Max(0, PageCount(catIndex) - 1); }
        Refresh();
    }

    private int NextNonEmpty(int from)
    {
        for (int i = 1; i <= categories.Length; i++)
        {
            int idx = (from + i) % categories.Length;
            if (sprites[idx] != null && sprites[idx].Length > 0) return idx;
        }
        return from;
    }

    private int PrevNonEmpty(int from)
    {
        for (int i = 1; i <= categories.Length; i++)
        {
            int idx = (from - i + categories.Length * 2) % categories.Length;
            if (sprites[idx] != null && sprites[idx].Length > 0) return idx;
        }
        return from;
    }

    private int PageCount(int idx)
    {
        if (categories == null || idx < 0 || idx >= categories.Length) return 1;
        int per = Mathf.Max(1, categories[idx].perPage);
        int total = (sprites != null && sprites[idx] != null) ? sprites[idx].Length : 0;
        return Mathf.Max(1, Mathf.CeilToInt((float)total / per));
    }

    // ==================== 加载 ====================

    private void EnsureLoaded()
    {
        if (sprites != null) return;
        if (categories == null) categories = new GalleryCategory[0];

        sprites = new Sprite[categories.Length][];
        for (int i = 0; i < categories.Length; i++)
        {
            string path = "Gallery/" + categories[i].folder;
            List<Sprite> list = new List<Sprite>();

            // 优先按 _list.txt 清单逐个加载。
            // Resources.LoadAll 会连子文件夹里的图片一起抓，数量不可控
            //（之前就出现过「ghost 里冒出 104 张」），用清单就不会。
            TextAsset manifest = Resources.Load<TextAsset>(path + "/_list");
            if (manifest != null && !string.IsNullOrEmpty(manifest.text))
            {
                string[] names = manifest.text.Split('\n');
                foreach (string raw in names)
                {
                    // 只去掉行尾的 \r 和开头的 BOM：
                    // 文件名里可能带空格（例如「床头柜破损 .png」），不能整体 Trim
                    string n = raw.TrimEnd('\r').TrimStart('\uFEFF');
                    if (n.Length == 0) continue;
                    Sprite sp = Resources.Load<Sprite>(path + "/" + n);
                    if (sp != null) list.Add(sp);
                    else Debug.LogWarning("[画廊] 读不到: " + path + "/" + n);
                }
            }
            else
            {
                Sprite[] found = Resources.LoadAll<Sprite>(path);
                if (found != null) list.AddRange(found);
                list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            }

            sprites[i] = list.ToArray();
            if (list.Count == 0) Debug.LogWarning("[画廊] 这个分类没有图片: " + path);
        }
        Debug.Log("[画廊] 加载完成，共 " + categories.Length + " 个分类");
    }

    // ==================== 刷新 ====================

    private void Refresh()
    {
        if (categories == null || categories.Length == 0 || slots == null) return;

        GalleryCategory cat = categories[catIndex];
        Sprite[] all = sprites[catIndex];
        int per = Mathf.Max(1, cat.perPage);
        int cols = Mathf.Max(1, cat.columns);
        int totalPages = PageCount(catIndex);

        if (titleText != null) titleText.text = cat.title;
        if (pageText != null) pageText.text = (page + 1) + " / " + totalPages + "     (" + all.Length + " 张)";

        int rows = Mathf.CeilToInt((float)per / cols);
        float cellW = (gridSize.x - cellGap * (cols - 1)) / cols;
        float cellH = (gridSize.y - cellGap * (rows - 1)) / rows;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            int index = page * per + i;
            bool use = i < per && index < all.Length;

            slots[i].gameObject.SetActive(use);
            if (!use) continue;

            slots[i].sprite = all[index];
            slots[i].color = Color.white;

            int r = i / cols;
            int c = i % cols;
            RectTransform rt = slots[i].rectTransform;
            rt.sizeDelta = new Vector2(cellW, cellH);
            rt.anchoredPosition = new Vector2(
                (c - (cols - 1) * 0.5f) * (cellW + cellGap),
                -((r - (rows - 1) * 0.5f) * (cellH + cellGap)));
        }
    }
}
