using UnityEngine;
using UnityEngine.InputSystem;

public class ItemPedestal : MonoBehaviour
{

    [SerializeField] private StoryItemId requiredItemId;
    [SerializeField] private DialogueLine[] missingItemLines;
    [SerializeField] private DialogueLine[] placedLines;
    [SerializeField] private DialogueLine[] alreadyPlacedLines;
    [SerializeField] private Act04Flow act04Flow;

    [Header("已放置物品的显示")]
    [SerializeField] private SpriteRenderer placedItemRenderer;

    private void Start()
    {
        RefreshPlacedItem();
    }

    private void RefreshPlacedItem()
    {
        if (placedItemRenderer != null)
            placedItemRenderer.enabled = GameProgress.GetState(requiredItemId) == StoryItemState.Placed;
    }

    private bool playerInside;
    private bool hasInvestigated;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.transform.root.CompareTag("Player"))
        {
            playerInside = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.transform.root.CompareTag("Player"))
        {
            playerInside = false;
        }
    }

    private void Update()
    {
        if (act04Flow != null && act04Flow.IsPlayingSequence) return;
        if (!playerInside) return;// 只有玩家在触发器内才允许交互

        DialogueManager dm = DialogueManager.Instance;// 获取对话管理器实例
        if (dm == null || dm.IsOpen || Time.timeScale == 0f) return;
        if (Time.frameCount == dm.LastStateChangeFrame) return;// 防止在同一帧内重复触发对话

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Interact(dm);
        }

    }

    private void Interact(DialogueManager dm)
    {
        StoryItemState state = GameProgress.GetState(requiredItemId);

        // Show the pedestal inscription once before allowing placement.
        if (state != StoryItemState.Placed && !hasInvestigated &&
            missingItemLines != null && missingItemLines.Length > 0)
        {
            hasInvestigated = true;
            dm.StartDialogue(missingItemLines);
            return;
        }

        if(state == StoryItemState.Uncollected)
        {
            dm.StartDialogue(missingItemLines);
            return;
        }

        if(state == StoryItemState.Placed)
        {
            dm.StartDialogue(alreadyPlacedLines);
            return;
        }
        if(GameProgress.TryPlace(requiredItemId, requiredItemId))
        {
            RefreshPlacedItem();
            dm.StartDialogue(placedLines);
        }

    }

    [ContextMenu("测试/获得三件代表物")]
    private void DebugCollectItems()
    {
        if (!Application.isPlaying) return;

        GameProgress.TryCollect(StoryItemId.MessageCard);
        GameProgress.TryCollect(StoryItemId.Clock);
        GameProgress.TryCollect(StoryItemId.MemorialPlaque);
    }
}

