using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 可拾取物（表现层）：按 E → 出文本 → 交给 GameProgress 记录 → 物体消失。
///
/// 道具的"数据"（未拿 / 在背包 / 已放置）统一由 GameProgress 管理；
/// 这个脚本只负责"看得见的行为"：能不能按 E、拾取后要不要消失、已拿过的是否自动隐藏。
///
/// 用法：挂在物品的 InteractionRange 子物体上，Item Id 选对应的 StoryItemId。
/// </summary>
public class PickupItem : MonoBehaviour
{
    [Header("身份（对应 GameProgress 里的三件剧情道具）")]
    [SerializeField] private StoryItemId itemId = StoryItemId.MessageCard;

    [Header("文本")]
    [SerializeField] private DialogueLine[] lookLines;     // 还没拿时看到的内容
    [SerializeField] private DialogueLine[] pickupLines;   // 拿走后的反馈（可选）

    [Header("行为")]
    [Tooltip("白盒：勾上=按E直接拿走；以后做【拿走/暂时不拿】面板时取消勾选")]
    [SerializeField] private bool autoPickup = true;

    [Header("消失表现")]
    [Tooltip("拾取后要隐藏的物体。留空 = 自动往上找带 SpriteRenderer 的那层（推荐留空）")]
    [SerializeField] private GameObject objectToHide;

    private bool playerInside;
    private bool wasDialogueOpen;
    private bool pendingHide;      // 等对话结束再隐藏，别打断玩家阅读

    public StoryItemId ItemId => itemId;

    /// <summary>只要不是"未拿"，就说明这东西已经不在场上了（在背包里或已放到底座）。</summary>
    private static bool IsCollected(StoryItemId id)
    {
        return GameProgress.GetState(id) != StoryItemState.Uncollected;
    }

    private void Awake()
    {
        // ★ 跨幕保留：进场景时如果这件道具已经拿过/放好了，自己消失
        if (IsCollected(itemId))
        {
            Debug.Log($"[拾取] {name}：{itemId} 已经不在场上（{GameProgress.GetState(itemId)}），自动隐藏");
            HideObject();
        }
    }

    private void Update()
    {
#if UNITY_EDITOR
        // ===== 临时调试：按 B 打印三件道具状态（以后这里换成正式背包面板）=====
        if (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)
        {
            Debug.Log("道具状态：" +
                      $"留言卡={GameProgress.GetState(StoryItemId.MessageCard)}，" +
                      $"挂钟={GameProgress.GetState(StoryItemId.Clock)}，" +
                      $"纪念牌={GameProgress.GetState(StoryItemId.MemorialPlaque)}");
        }

#endif
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
        // 已经拿过 / 已放置：只出文本，不再拾取
        if (IsCollected(itemId))
        {
            dm.StartDialogue(lookLines);
            return;
        }

        if (!autoPickup)
        {
            // 以后这里接【拿走】【暂时不拿】面板
            dm.StartDialogue(lookLines);
            return;
        }

        DoPickup(dm);
    }

    private void DoPickup(DialogueManager dm)
    {
        // ★ 数据层只认 GameProgress：它负责"不能重复拿"的判断
        if (!GameProgress.TryCollect(itemId))
        {
            Debug.Log($"[拾取] 拾取失败：{itemId} 已经被拿过了");
            dm.StartDialogue(lookLines);
            return;
        }

        Debug.Log($"[拾取] 已放入背包：{itemId}（状态 = {GameProgress.GetState(itemId)}）");

        pendingHide = true;                       // 等对话结束再隐藏
        dm.StartDialogue(pickupLines);
    }

    private void OnDialogueFinished()
    {
        if (!pendingHide) return;
        pendingHide = false;
        HideObject();                             // 拾取后从地图上消失（连父物体的图形一起）
    }

    /// <summary>
    /// 隐藏"看得见的那一层"：
    /// 优先用 Inspector 里指定的物体；没指定就自动往上找第一个带 SpriteRenderer 的父物体
    /// （脚本挂在子物体 InteractionRange 上、图形在父物体上时，正好命中父物体）。
    /// </summary>
    private void HideObject()
    {
        InteractionPromptUI.Hide(this);   // 先把共享提示收掉，避免残留

        GameObject target = objectToHide != null ? objectToHide : FindVisualRoot();

        if (target != null) target.SetActive(false);
        else gameObject.SetActive(false);
    }

    private GameObject FindVisualRoot()
    {
        Transform current = transform;

        while (current != null)
        {
            if (current.GetComponent<SpriteRenderer>() != null)
            {
                return current.gameObject;
            }

            current = current.parent;
        }

        return gameObject;
    }

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
