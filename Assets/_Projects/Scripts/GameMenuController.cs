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
            if (backpackUI == null) return;
            if (backpackUI.IsOpen)
            {
                backpackUI.Close(); //如果背包已经打开，按下B键则关闭背包
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

    public void ResumeGame()
    {
        isMenuActive = false;
        gameMenu.SetActive(isMenuActive); //关闭菜单
        Time.timeScale = 1;//恢复游戏
    }
}
