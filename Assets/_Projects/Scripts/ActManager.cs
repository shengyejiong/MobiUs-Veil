using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class ActManager : MonoBehaviour
{
    [SerializeField] private Light2D roomlight;
    [SerializeField] private Color duskColor;
    public Rigidbody2D playerRigidbody;
    public Transform duskSpawnPoint;
    public GameMenuController gameMenuController;
    public ScreenFader screenFader;
    public PlayerInput playerInput;
    bool isTransition = false;

    private enum sceneState
    {
        Longing,
        Dusk
    }

    [SerializeField] private sceneState currentSceneState = sceneState.Longing;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        

    }

    // Update is called once per frame
    void Update()
    {
        Keyboard keyboard = Keyboard.current;//获取键盘输入引用
        if (keyboard == null || gameMenuController.isMenuActive) return;
        if(keyboard.nKey.wasPressedThisFrame)
        {
            EnterDusk();//按下N键切换到黄昏场景
        }
    }

    public void EnterDusk()
    {
        if(currentSceneState != sceneState.Dusk && !isTransition)
        {
            isTransition = true;
            playerRigidbody.linearVelocity = Vector2.zero; //重置玩家的速度
            StartCoroutine(TransitionToDusk()); //开始场景切换协程

        }
    }

    public void ApplyDuskState()
    {
        currentSceneState = sceneState.Dusk;
        Debug.Log("切换到黄昏场景");
        roomlight.intensity = 0.7f; //降低灯光强度
        roomlight.color = duskColor; //改变灯光颜色为黄昏色
        playerRigidbody.position = duskSpawnPoint.position; //将玩家传送到黄昏场景的出生点
    }

    public IEnumerator TransitionToDusk()
    {
        playerInput.DeactivateInput(); //禁用玩家输入
        yield return screenFader.FadeTo(1f); //屏幕渐变到黑色
        ApplyDuskState(); //应用黄昏场景状态
        yield return screenFader.FadeTo(0f); //屏幕渐变回正常
        playerInput.ActivateInput(); //重新启用玩家输入
        isTransition = false;
    }
}
