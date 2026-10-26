using UnityEngine;

/// <summary>按人物脚底 Y 坐标，将桌子与桌面物品作为一组排在人物前后。</summary>
public sealed class TableDepthSorter : MonoBehaviour
{
    [Header("人物与桌子")]
    [SerializeField] private SpriteRenderer tableRenderer;
    [SerializeField] private SpriteRenderer[] tabletopRenderers;
    [SerializeField] private SpriteRenderer playerRenderer;
    [SerializeField] private Transform playerGroundPoint;

    [Tooltip("桌子前后切换线相对其 Transform 的世界 Y 偏移；不移动图片或碰撞体")]
    [SerializeField] private float tableGroundOffsetY;

    private int originalTableOrder;
    private int[] originalItemOrders;
    private int[] itemOffsets;
    private int maxItemOffset;
    private bool hasOriginalOrder;

    private void OnEnable()
    {
        if (tableRenderer == null) return;
        originalTableOrder = tableRenderer.sortingOrder;
        hasOriginalOrder = true;
        maxItemOffset = 0;
        originalItemOrders = new int[tabletopRenderers != null ? tabletopRenderers.Length : 0];
        itemOffsets = new int[originalItemOrders.Length];
        for (int i = 0; i < originalItemOrders.Length; i++)
        {
            SpriteRenderer item = tabletopRenderers[i];
            if (item == null) continue;
            originalItemOrders[i] = item.sortingOrder;
            // 桌面物品至少高于桌面一层，兼容原来与桌子同层的摆放。
            itemOffsets[i] = Mathf.Max(1, item.sortingOrder - originalTableOrder);
            if (item.sortingLayerID == tableRenderer.sortingLayerID)
                maxItemOffset = Mathf.Max(maxItemOffset, itemOffsets[i]);
        }
    }

    private void LateUpdate()
    {
        if (!hasOriginalOrder || tableRenderer == null || playerRenderer == null || playerGroundPoint == null) return;
        if (tableRenderer.sortingLayerID != playerRenderer.sortingLayerID) return;

        float tableY = tableRenderer.transform.position.y + tableGroundOffsetY;
        bool playerInFront = playerGroundPoint.position.y < tableY;
        // 为物品留出原来的层级间隔，避免物品仍排在人物前面。
        int tableOrder = playerRenderer.sortingOrder + (playerInFront ? -1 - maxItemOffset : 1);
        tableRenderer.sortingOrder = tableOrder;
        if (tabletopRenderers == null || originalItemOrders == null) return;
        for (int i = 0; i < Mathf.Min(tabletopRenderers.Length, originalItemOrders.Length); i++)
        {
            SpriteRenderer item = tabletopRenderers[i];
            if (item == null || item.sortingLayerID != tableRenderer.sortingLayerID) continue;
            item.sortingOrder = tableOrder + itemOffsets[i];
        }
    }

    private void OnDisable()
    {
        if (hasOriginalOrder && tableRenderer != null)
            tableRenderer.sortingOrder = originalTableOrder;
        if (hasOriginalOrder && tabletopRenderers != null && originalItemOrders != null)
        {
            for (int i = 0; i < Mathf.Min(tabletopRenderers.Length, originalItemOrders.Length); i++)
            {
                if (tabletopRenderers[i] != null)
                    tabletopRenderers[i].sortingOrder = originalItemOrders[i];
            }
        }
        hasOriginalOrder = false;
    }
}
