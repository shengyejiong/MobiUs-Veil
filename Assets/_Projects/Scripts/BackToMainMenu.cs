using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 暂停菜单里的「Quit」按钮 —— 直接退回主菜单界面。
///
/// ★ 会自动把本物体上的 Button 接到 GoToMainMenu()，
///   所以场景里不需要手写 OnClick 事件（手写容易出问题）。
/// </summary>
public class BackToMainMenu : MonoBehaviour
{
    [Tooltip("要回到的场景名")]
    [SerializeField] private string sceneName = "MainMenu";

    private void Awake()
    {
        // 自动接线：本物体上有 Button 就接上，不用在 Inspector 里拖
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveListener(GoToMainMenu);
            btn.onClick.AddListener(GoToMainMenu);
            btn.interactable = true;
            Debug.Log("[暂停菜单] Quit 按钮已自动接到 GoToMainMenu()");
        }
    }

    /// <summary>退回主菜单（按钮点击 / 也可以手动调用）</summary>
    public void GoToMainMenu()
    {
        Debug.Log("[暂停菜单] 退回主菜单：" + sceneName);

        // ① 恢复鼠标（暂停时可能被锁住）
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // ② 恢复时间 —— 必须放在最后！
        //    暂停菜单会把 timeScale 设成 0，而 timeScale 是全局的、会跨场景保留，
        //    不恢复的话回到主菜单整个界面是冻结的。
        Time.timeScale = 1f;

        // ③ 回主菜单
        SceneManager.LoadScene(sceneName);
    }
}
