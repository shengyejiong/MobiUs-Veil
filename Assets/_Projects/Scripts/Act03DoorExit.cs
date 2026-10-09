using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 第三幕：房门。
///
///   条件没满足（没读资料 / 没写名）→ 打不开："门缝后还是黑的。床边有东西。"
///   追逐开始后【第一次】走入出口 → 回到床下空地，露出舞台痕迹 + "怎么又回来了？"
///   从床下再次逃到出口 → 进入第四幕
///
/// 两次开门 + 一次强制循环是策划案定死的规则（不增加第三轮）。
/// </summary>
public class Act03DoorExit : MonoBehaviour
{
    [Header("转场")]
    [SerializeField] private SceneTransition sceneTransition;

    [Header("门的外观（追逐开始后打开）")]
    [SerializeField] private ShadowChase shadowChase;
    [SerializeField] private SpriteRenderer doorRenderer;
    [SerializeField] private Sprite closedDoorSprite;
    [SerializeField] private Sprite openDoorSprite;
    [SerializeField] private GameObject doorwayDarkness;

    [Header("文本")]
    [SerializeField] private DialogueLine[] lockedLines;   // 条件没满足
    [SerializeField] private DialogueLine[] loopLines;     // 第一次开门（空间循环）

    [Header("舞台痕迹（第一次开门后出现）")]
    [SerializeField] private GameObject[] stageTraces;

    [Header("音效")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip doorClip;

    [Header("第一次到门口：血手印演出")]
    [SerializeField] private SpriteRenderer[] bloodHandprints;
    [SerializeField] private AudioSource impactAudioSource;
    [SerializeField] private AudioClip impactClip;
    [SerializeField, Min(0.1f)] private float impactSequenceDuration = 0.5f;

    private bool playerInside;
    private bool wasDialogueOpen;
    private bool entryConsumed;
    private bool doorWasOpen;
    private Collider2D exitArea;
    private Collider2D playerCollider;
    private bool isPlayingDoorSequence;
    private bool hasShownBloodHandprints;

    private bool DoorOpen => shadowChase != null && shadowChase.IsChasing;
    private bool AtDoorway => playerCollider != null && exitArea != null
        && exitArea.OverlapPoint(playerCollider.bounds.center);

    /// <summary>可以开门了吗：资料已读 + 名字已写</summary>
    private static bool Ready =>
    Act03Story.FactsRead &&
    Act03Story.NameWritten &&
    GameProgress.GetState(StoryItemId.MemorialPlaque) != StoryItemState.Uncollected;

    private void Awake()
    {
        exitArea = GetComponent<Collider2D>();
        // 兜底：没拖引用就自己找场景里的 SceneTransition
        if (sceneTransition == null) sceneTransition = FindFirstObjectByType<SceneTransition>();
        if (shadowChase == null) shadowChase = FindFirstObjectByType<ShadowChase>();
        if (doorwayDarkness != null && doorRenderer != null)
        {
            Transform darkness = doorwayDarkness.transform;
            darkness.SetParent(doorRenderer.transform, false);
            darkness.localPosition = Vector3.zero;
            darkness.localRotation = Quaternion.identity;
            darkness.localScale = Vector3.one;
        }
    }

    private void Start()
    {
        SetDoorVisual(false);
        ClearBloodHandprints();
    }

    private void SetDoorVisual(bool open)
    {
        doorWasOpen = open;
        Sprite sprite = open ? openDoorSprite : closedDoorSprite;
        if (doorRenderer != null && sprite != null) doorRenderer.sprite = sprite;
        if (doorwayDarkness != null) doorwayDarkness.SetActive(open);
    }

    private void Update()
    {
        bool doorOpen = DoorOpen;
        if (doorOpen != doorWasOpen) SetDoorVisual(doorOpen);
        if (isPlayingDoorSequence)
        {
            InteractionPromptUI.Hide(this);
            return;
        }
        if (hasShownBloodHandprints && !Act03Story.DoorLooped)
        {
            ClearBloodHandprints();
            hasShownBloodHandprints = false;
        }
        bool atDoorway = AtDoorway;
        if (!atDoorway) entryConsumed = false;

        DialogueManager dm = DialogueManager.Instance;

        if (dm == null)
        {
            InteractionPromptUI.Hide(this);
            return;
        }

        bool canInteract = playerInside && !dm.IsOpen;

        if (canInteract && !doorOpen) InteractionPromptUI.Show(this, transform);
        else InteractionPromptUI.Hide(this);

        bool canPress = canInteract
            && Time.frameCount != dm.LastStateChangeFrame
            && Time.timeScale > 0f
            && (sceneTransition == null || !sceneTransition.IsTransitioning)
            && (shadowChase == null || !shadowChase.IsCaught);

        ChoiceUI choices = ChoiceUI.Get();
        if (choices != null && choices.IsOpen) canPress = false;

        // 门打开后走入出口就触发；离开触发区才允许下一次进入。
        if (canPress && doorOpen && Ready && atDoorway && !entryConsumed)
        {
            entryConsumed = true;
            Interact(dm);
        }
        else if (canPress && !doorOpen && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
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
        if (!Ready || !DoorOpen)
        {
            Debug.Log($"[第三幕] 门还开不了（资料已读={Act03Story.FactsRead} 已写名={Act03Story.NameWritten}）");
            dm.StartDialogue(lockedLines);
            return;
        }

        // ② 第一次开门 → 空间循环
        if (!Act03Story.DoorLooped)
        {
            StartCoroutine(PlayFirstDoorSequence(dm));
            return;
        }

        // ③ 第二次开门 → 第四幕
        GameState.CurrentAct = 4;
        Debug.Log("[第三幕] 第二次开门 → 进入第四幕");

        if (sceneTransition != null) sceneTransition.GoToNextScene();
        else Debug.LogWarning("[第三幕] 没有绑定 SceneTransition，无法转场");
    }

    private void OnDialogueFinished() { }

    private IEnumerator PlayFirstDoorSequence(DialogueManager dm)
    {
        isPlayingDoorSequence = true;
        if (shadowChase != null) shadowChase.SetDoorSequencePaused(true);
        InteractionPromptUI.Hide(this);
        ClearBloodHandprints();

        try
        {
            const int count = 5;
            float duration = Mathf.Max(0.1f, impactSequenceDuration);
            float elapsed = 0f;
            int nextPrint = 0;

            // 五次拍击均匀分布在演出时间内；0.5 秒时，每隔 0.1 秒拍下一次。
            while (elapsed < duration)
            {
                while (nextPrint < count && elapsed >= nextPrint * duration / count)
                {
                    ShowBloodHandprint(nextPrint);
                    nextPrint++;
                }
                yield return null;
                elapsed += Time.deltaTime;
            }
            // 低帧率跨过多个拍点时，也保证五个手印和五次声音全部触发。
            while (nextPrint < count) ShowBloodHandprint(nextPrint++);

            if (dm == null) yield break;
            hasShownBloodHandprints = true;
            Act03Story.DoorLooped = true;

            if (audioSource != null && doorClip != null) audioSource.PlayOneShot(doorClip);
            if (stageTraces != null)
            {
                foreach (GameObject o in stageTraces)
                    if (o != null) o.SetActive(true);
            }

            dm.StartDialogue(loopLines);
            if (shadowChase != null) shadowChase.RestartAfterDoorLoop();
            playerInside = false;
            playerCollider = null;
            entryConsumed = false;
            Debug.Log("[第三幕] 五次血手印演出结束 → 回到床下继续第二轮追逐");
        }
        finally
        {
            isPlayingDoorSequence = false;
            if (shadowChase != null) shadowChase.SetDoorSequencePaused(false);
        }
    }

    private void ShowBloodHandprint(int index)
    {
        if (bloodHandprints != null && index < bloodHandprints.Length && bloodHandprints[index] != null)
            bloodHandprints[index].enabled = true;
        if (impactAudioSource != null && impactClip != null)
            impactAudioSource.PlayOneShot(impactClip);
    }

    private void ClearBloodHandprints()
    {
        if (bloodHandprints == null) return;
        foreach (SpriteRenderer handprint in bloodHandprints)
            if (handprint != null) handprint.enabled = false;
    }

    private void OnDisable()
    {
        if (!isPlayingDoorSequence) return;
        StopAllCoroutines();
        isPlayingDoorSequence = false;
        if (shadowChase != null) shadowChase.SetDoorSequencePaused(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.transform.root.CompareTag("Player"))
        {
            playerInside = true;
            playerCollider = other;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.transform.root.CompareTag("Player"))
        {
            playerInside = false;
            playerCollider = null;
            entryConsumed = false;
            InteractionPromptUI.Hide(this);
        }
    }
}
