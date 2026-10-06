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

    /// <summary>「设置」按钮</summary>
    public void OpenSettings()
    {
        if (settingsPanel == null) return;

        SyncFromSaved();
        settingsPanel.SetActive(true);
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
