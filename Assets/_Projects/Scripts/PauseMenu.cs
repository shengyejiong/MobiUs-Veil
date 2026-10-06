using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// 暂停菜单总管：主菜单 + 设置面板（以后道具面板也加在这里）
/// 挂在 Canvas 上（和 GameMenuController 同一个物体）
public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance;

    [Header("面板引用")]
    [SerializeField] private GameObject pauseMenu;       // 拖 GameMenuPanel
    [SerializeField] private GameObject settingsPanel;   // 拖 SettingsPanel

    [Header("设置项")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle fullscreenToggle;

    private const string KeyVolume = "opt.volume";
    private const string KeyFullscreen = "opt.fullscreen";

    private void Awake()
    {
        Instance = this;

        // ★ 兜底：Inspector 里没拖引用时，按名字在 Canvas 下面自己找
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
        if (fullscreenToggle == null)
        {
            Transform t = transform.Find("SettingsPanel/FullscreenToggle");
            if (t != null) fullscreenToggle = t.GetComponent<Toggle>();
        }

        if (settingsPanel != null) settingsPanel.SetActive(false);

        // 这一行会告诉我们到底找没找到（测试用，以后可以删）
        Debug.Log($"[暂停菜单] GameMenuPanel={(pauseMenu != null)}  SettingsPanel={(settingsPanel != null)}  " +
                  $"音量滑条={(volumeSlider != null)}  全屏开关={(fullscreenToggle != null)}");
    }

    private void Update()
    {
        // 临时兜底：鼠标点不了时，按 O 可以直接开关设置面板
        if (Keyboard.current != null && Keyboard.current.oKey.wasPressedThisFrame)
        {
            if (settingsPanel != null && settingsPanel.activeSelf) CloseSubPanels();
            else OpenSettings();
        }
    }

    /// ESC 优先级：返回 true = 我处理了，GameMenuController 不用再管
    public bool OnEscape()    {
        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            CloseSubPanels();
            return true;
        }
        return false;
    }

    /// 「设置」按钮
    public void OpenSettings()
    {
        if (settingsPanel == null) return;

        SyncFromSaved();
        settingsPanel.SetActive(true);
        Time.timeScale = 0f;            // 面板打开时暂停（策划案 227 行）
    }

    /// 「返回」按钮：关掉子面板
    public void CloseSubPanels()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // 主菜单还开着 → 保持暂停；否则恢复游戏
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
        if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(fs);
        Screen.fullScreen = fs;
    }

    /// 绑 Slider 的 OnValueChanged（选 Dynamic float）
    public void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(KeyVolume, value);
        PlayerPrefs.Save();
    }

    /// 绑 Toggle 的 OnValueChanged（选 Dynamic bool）
    public void OnFullscreenChanged(bool on)
    {
        Screen.fullScreen = on;
        PlayerPrefs.SetInt(KeyFullscreen, on ? 1 : 0);
        PlayerPrefs.Save();
    }
}