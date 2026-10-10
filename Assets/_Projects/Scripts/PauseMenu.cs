using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 暂停菜单总管：主菜单 + 设置面板（以后道具面板也加在这里）
/// 挂在 Canvas 上（和 GameMenuController 同一个物体）
/// </summary>
public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance;

    [Header("面板引用")]
    [SerializeField] private GameObject pauseMenu;       // GameMenuPanel
    [SerializeField] private GameObject settingsPanel;   // SettingsPanel

    [Header("设置项")]
    [SerializeField] private Slider volumeSlider;
    [Tooltip("「全屏」按钮的底图（用来高亮当前模式）")]
    [SerializeField] private Image fullscreenButtonImage;
    [Tooltip("「窗口化」按钮的底图")]
    [SerializeField] private Image windowedButtonImage;

    [Header("互斥面板")]
    [Tooltip("打开设置面板时会先把背包关掉（两个面板不允许同时开）")]
    [SerializeField] private BackpackUI backpackUI;

    private const string KeyVolume = "opt.volume";
    private const string KeyFullscreen = "opt.fullscreen";

    // 当前模式高亮色 / 另一个按钮的底色
    private static readonly Color ModeActive = new Color(0.85f, 0.72f, 0.45f, 1f);
    private static readonly Color ModeIdle = new Color(0.22f, 0.23f, 0.28f, 1f);

    private void Awake()
    {
        Instance = this;

        // ★ 兜底 1：Inspector 里没拖引用时，按名字自己找
        //   （Transform.Find 连"未激活"的子物体也能找到，所以 SettingsPanel 关着也没关系）
        if (pauseMenu == null)
        {
            Transform t = transform.Find("GameMenuPanel");
            if (t != null) pauseMenu = t.gameObject;
        }
        if (settingsPanel == null)
        {
            Transform t = transform.Find("SettingsPanel");
            if (t != null) settingsPanel = t.gameObject;
        }
        if (volumeSlider == null)
        {
            Transform t = transform.Find("SettingsPanel/VolumeSlider");
            if (t != null) volumeSlider = t.GetComponent<Slider>();
        }
        if (fullscreenButtonImage == null)
        {
            Transform t = transform.Find("SettingsPanel/FullscreenButton");
            if (t != null) fullscreenButtonImage = t.GetComponent<Image>();
        }
        if (windowedButtonImage == null)
        {
            Transform t = transform.Find("SettingsPanel/WindowedButton");
            if (t != null) windowedButtonImage = t.GetComponent<Image>();
        }

        // 兜底：背包界面（用来做"两个面板互斥"）
        if (backpackUI == null) backpackUI = FindFirstObjectByType<BackpackUI>();

        if (settingsPanel != null) settingsPanel.SetActive(false);

        // ★ 兜底 2：旧版场景里按钮是"禁用 + 没接事件"的占位状态，按名字自动补上
        //   （场景数据更新好以后，这两段兜底都可以删掉）
        foreach (Button b in GetComponentsInChildren<Button>(true))
        {
            switch (b.name)
            {
                case "Settings":
                    if (b.onClick.GetPersistentEventCount() == 0) b.onClick.AddListener(OpenSettings);
                    if (!b.interactable) b.interactable = true;
                    break;

                case "BackButton":
                    if (b.onClick.GetPersistentEventCount() == 0) b.onClick.AddListener(CloseSubPanels);
                    if (!b.interactable) b.interactable = true;
                    break;

                case "FullscreenButton":
                    if (b.onClick.GetPersistentEventCount() == 0) b.onClick.AddListener(SetFullscreenOn);
                    if (!b.interactable) b.interactable = true;
                    break;

                case "WindowedButton":
                    if (b.onClick.GetPersistentEventCount() == 0) b.onClick.AddListener(SetFullscreenOff);
                    if (!b.interactable) b.interactable = true;
                    break;
            }
        }
    }

    /// <summary>ESC 优先级：返回 true = 我处理了，GameMenuController 不用再管</summary>
    public bool OnEscape()
    {
        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            CloseSubPanels();
            return true;
        }
        return false;
    }

    /// <summary>设置面板现在开着吗（给 GameMenuController 判断用）</summary>
    public bool IsSettingsOpen => settingsPanel != null && settingsPanel.activeSelf;

    /// <summary>「设置」按钮</summary>
    public void OpenSettings()
    {
        if (settingsPanel == null) return;

        // 设置已经开着 → 不做任何事
        if (settingsPanel.activeSelf) return;

        // ★ 同一时间只允许开一个面板：背包还开着就必须先按返回（B / ESC / 道具按钮）关掉它
        if (backpackUI != null && backpackUI.IsOpen)
        {
            Debug.Log("[暂停菜单] 背包还开着 —— 先按 B 或 ESC 返回，才能打开设置");
            return;
        }

        SyncFromSaved();
        settingsPanel.SetActive(true);
        settingsPanel.transform.SetAsLastSibling();
        Time.timeScale = 0f;            // 面板打开时暂停（策划案 227 行）
    }

    /// <summary>「返回」按钮：关掉子面板</summary>
    public void CloseSubPanels()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);

        bool menuOpen = pauseMenu != null && pauseMenu.activeSelf;
        Time.timeScale = menuOpen ? 0f : 1f;
    }

    // ===== 设置项 =====

    private void SyncFromSaved()
    {
        float v = PlayerPrefs.GetFloat(KeyVolume, 1f);
        if (volumeSlider != null) volumeSlider.SetValueWithoutNotify(v);
        AudioListener.volume = v;

        bool fs = PlayerPrefs.GetInt(KeyFullscreen, Screen.fullScreen ? 1 : 0) == 1;
        ApplyFullscreen(fs);
    }

    /// <summary>绑 Slider 的 OnValueChanged（Dynamic float）</summary>
    public void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(KeyVolume, value);
        PlayerPrefs.Save();
    }

    /// <summary>「全屏」按钮</summary>
    public void SetFullscreenOn() => ApplyFullscreen(true);

    /// <summary>「窗口化」按钮</summary>
    public void SetFullscreenOff() => ApplyFullscreen(false);

    private void ApplyFullscreen(bool on)
    {
        Screen.fullScreenMode = on ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        Screen.fullScreen = on;

        PlayerPrefs.SetInt(KeyFullscreen, on ? 1 : 0);
        PlayerPrefs.Save();

        HighlightMode(on);
        Debug.Log($"[设置] 显示模式 → {(on ? "全屏" : "窗口化")}");
    }

    /// <summary>把当前模式的那个按钮点亮</summary>
    private void HighlightMode(bool fullscreenOn)
    {
        if (fullscreenButtonImage != null) fullscreenButtonImage.color = fullscreenOn ? ModeActive : ModeIdle;
        if (windowedButtonImage != null) windowedButtonImage.color = fullscreenOn ? ModeIdle : ModeActive;
    }
}
