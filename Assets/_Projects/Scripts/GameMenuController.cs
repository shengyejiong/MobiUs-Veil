using UnityEngine;
using UnityEngine.InputSystem;

public class GameMenuController : MonoBehaviour
{
    [SerializeField] private BackpackUI backpackUI;
    [SerializeField] private SceneTransition sceneTransition;

    public GameObject gameMenu;
    public bool isMenuActive = false;//初始时关闭菜单

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameMenu.SetActive(isMenuActive); //确保菜单在开始时关闭
    }

    // Update is called once per frame
    void Update()
    {
        Keyboard keyboard = Keyboard.current;//获取键盘输入引用
        if(keyboard == null) return;

        if(keyboard.escapeKey.wasPressedThisFrame)
        {
            if(backpackUI != null && backpackUI.IsOpen)
            {
                backpackUI.Close(); //如果背包已经打开，按下ESC键则关闭背包
                if (isMenuActive) Time.timeScale = 0f; // menu still open -> keep paused
                return;//防止多触
            }
            if (PauseMenu.Instance != null && PauseMenu.Instance.OnEscape()) return;

            if(isMenuActive)
            {
                ResumeGame(); //如果菜单已经激活，按下ESC键则关闭菜单并恢复游戏
                return;
            }
            isMenuActive = !isMenuActive; //切换菜单的激活状态
            gameMenu.SetActive(isMenuActive);
            Time.timeScale = 0;//暂停游戏
        }

        if (keyboard.bKey.wasPressedThisFrame)
        {
            // cannot open the backpack while being chased (design line 325)
            if (ShadowChase.Instance != null && ShadowChase.Instance.IsChasing)
            {
                ShadowChase.Instance.ShowCantUseBackpack();
                return;
            }

            if (backpackUI == null) return;
            if (backpackUI.IsOpen)
            {
                backpackUI.Close(); //如果背包已经打开，按下B键则关闭背包
                if (isMenuActive) Time.timeScale = 0f; // menu still open -> keep paused
            }
            else if(isMenuActive || Time.timeScale == 0f || (sceneTransition != null && sceneTransition.IsTransitioning) || (DialogueManager.Instance != null && DialogueManager.Instance.IsOpen))
            {
                return; //如果菜单已经激活，按下B键则不做任何操作
            }
            else
            {
                backpackUI.Open(); //如果背包未打开，按下B键则打开背包
            }
        }
    }

    // Items button in the pause menu: open/close the backpack
    // (same panel as the B key, data comes from GameProgress)
    public void ToggleBackpack()
    {
        if (backpackUI == null) return;

        if (backpackUI.IsOpen)
        {
            backpackUI.Close();
            if (isMenuActive) Time.timeScale = 0f;   // menu still open -> keep paused
        }
        else
        {
            // only one panel at a time: must press back first
            if (PauseMenu.Instance != null && PauseMenu.Instance.IsSettingsOpen) return;

            backpackUI.Open();                        // Refresh + pause inside
        }
    }
    public void ResumeGame()
    {
        // returning to the game must also close any open sub panel (backpack / settings)
        if (backpackUI != null && backpackUI.IsOpen) backpackUI.Close();
        if (PauseMenu.Instance != null && PauseMenu.Instance.IsSettingsOpen) PauseMenu.Instance.CloseSubPanels();

        isMenuActive = false;
        gameMenu.SetActive(isMenuActive); //关闭菜单
        Time.timeScale = 1;//恢复游戏
    }
}
