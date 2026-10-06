using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

public class SceneTransition : MonoBehaviour
{
    [SerializeField] private ScreenFader screenFader;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private string nextSceneName;

    public bool IsTransitioning { get; private set; }//用来阻止重复转场

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private IEnumerator Start()
    {
        playerInput.DeactivateInput();
        yield return screenFader.FadeTo(0f);//等屏幕淡入完成，再执行后续操作
        playerMovement.SetMovementLocked(false);//解锁玩家移动
        playerInput.ActivateInput();
        IsTransitioning = false;
        Debug.Log(GameProgress.GetState(StoryItemId.MessageCard));
        Debug.Log(GameProgress.GetState(StoryItemId.MemorialPlaque));
        Debug.Log(GameProgress.GetState(StoryItemId.Clock));
    }

    private void Awake()
    {
        Time.timeScale = 1f;//确保游戏时间正常运行
        IsTransitioning = true;
        screenFader.canvasGroup.alpha = 1f;
        playerMovement.SetMovementLocked(true);//锁定玩家移动
    }

    public void GoToNextScene()
    {
        if (IsTransitioning || Time.timeScale == 0f || (DialogueManager.Instance != null && DialogueManager.Instance.IsOpen == true) || string.IsNullOrWhiteSpace(nextSceneName)) return;

        IsTransitioning = true;
        StartCoroutine(Transition());
       
    }

    private IEnumerator Transition()
    {
        playerInput.DeactivateInput();
        playerMovement.SetMovementLocked(true);
        yield return screenFader.FadeTo(1f);
        yield return SceneManager.LoadSceneAsync(nextSceneName);
    }

    private void Update()
    {
        if(Keyboard.current != null)
        {
            if(Keyboard.current.nKey.wasPressedThisFrame)
            {
                GoToNextScene();
            }
            if(Keyboard.current.f1Key.wasPressedThisFrame)
            {
                Debug.Log(GameProgress.TryCollect(StoryItemId.MessageCard));
            }
            if(Keyboard.current.f2Key.wasPressedThisFrame)
            {
                Debug.Log(GameProgress.TryPlace(StoryItemId.MessageCard, StoryItemId.Clock));
            }
            if (Keyboard.current.f3Key.wasPressedThisFrame)
            {
                Debug.Log(GameProgress.TryPlace(StoryItemId.MessageCard, StoryItemId.MessageCard));
            }
        }
    }
}
