using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 第三幕：房门。
///
///   条件没满足（没读资料 / 没写名）→ 打不开："门缝后还是黑的。床边有东西。"
///   写名后【第一次】开门 → 空间循环：露出舞台痕迹 + "怎么又回来了？"
///   写名后【第二次】开门 → 静音，进入第四幕
///
/// 两次开门 + 一次强制循环是策划案定死的规则（不增加第三轮）。
/// </summary>
public class Act03DoorExit : MonoBehaviour
{
    [Header("转场")]
    [SerializeField] private SceneTransition sceneTransition;

    [Header("文本")]
    [SerializeField] private DialogueLine[] lockedLines;   // 条件没满足
    [SerializeField] private DialogueLine[] loopLines;     // 第一次开门（空间循环）

    [Header("舞台痕迹（第一次开门后出现）")]
    [SerializeField] private GameObject[] stageTraces;

    [Header("音效（可选，留空也能跑）")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip doorClip;

    private bool playerInside;
    private bool wasDialogueOpen;

    /// <summary>可以开门了吗：资料已读 + 名字已写</summary>
    private static bool Ready => Act03Story.FactsRead && Act03Story.NameWritten;

    private void Awake()
    {
        // 兜底：没拖引用就自己找场景里的 SceneTransition
        if (sceneTransition == null) sceneTransition = FindFirstObjectByType<SceneTransition>();
    }

    private void Update()
    {
        DialogueManager dm = DialogueManager.Instance;

        if (dm == null)
        {
            InteractionPromptUI.Hide(this);
            return;
        }

        bool canInteract = playerInside && !dm.IsOpen;

        if (canInteract) InteractionPromptUI.Show(this, transform);
        else InteractionPromptUI.Hide(this);

        bool canPress = canInteract
            && Time.frameCount != dm.LastStateChangeFrame
            && Time.timeScale > 0f;

        if (canPress && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Interact(dm);
        }

        bool isOpen = dm.IsOpen;
        if (wasDialogueOpen && !isOpen) OnDialogueFinished();
        wasDialogueOpen = isOpen;
    }

    private void Interact(DialogueManager dm)
    {
        // ① 条件没满足：门打不开，把你引回床边
        if (!Ready)
        {
            Debug.Log($"[第三幕] 门还开不了（资料已读={Act03Story.FactsRead} 已写名={Act03Story.NameWritten}）");
            dm.StartDialogue(lockedLines);
            return;
        }

        // ② 第一次开门 → 空间循环
        if (!Act03Story.DoorLooped)
        {
            Act03Story.DoorLooped = true;

            if (audioSource != null && doorClip != null) audioSource.PlayOneShot(doorClip);

            if (stageTraces != null)
            {
                foreach (GameObject o in stageTraces)
                {
                    if (o != null) o.SetActive(true);
                }
            }

            Debug.Log("[第三幕] 第一次开门 → 空间循环（舞台痕迹出现）");
            dm.StartDialogue(loopLines);
            return;
        }

        // ③ 第二次开门 → 第四幕
        GameState.CurrentAct = 4;
        Debug.Log("[第三幕] 第二次开门 → 进入第四幕");

        if (sceneTransition != null) sceneTransition.GoToNextScene();
        else Debug.LogWarning("[第三幕] 没有绑定 SceneTransition，无法转场");
    }

    private void OnDialogueFinished() { }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.transform.root.CompareTag("Player")) playerInside = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.transform.root.CompareTag("Player"))
        {
            playerInside = false;
            InteractionPromptUI.Hide(this);
        }
    }
}
