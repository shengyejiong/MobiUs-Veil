using UnityEngine;

/// <summary>
/// 按"剧情道具状态"决定自己是显示还是隐藏。
///
/// 典型用法：
///   第三幕墙上 —— 已拿走挂钟 → 显示"空挂点"；没拿 → 显示"破损的钟"
///   第四幕底座 —— 只显示已经放上去的那件道具
///
/// 用法：把这个脚本挂在要控制显隐的物体上，选好 Item Id 和三个勾选项。
/// 物体在场景里保持【激活】状态即可（脚本会在 Awake 里自己决定）。
/// </summary>
public class ShowIfItemState : MonoBehaviour
{
    [Header("哪件道具")]
    [SerializeField] private StoryItemId itemId = StoryItemId.Clock;

    [Header("什么时候显示（可多选）")]
    [Tooltip("还没拿到时显示（东西还在场景里）")]
    [SerializeField] private bool showWhenUncollected = true;

    [Tooltip("在背包里时显示（已经被拿走了）")]
    [SerializeField] private bool showWhenInBag = false;

    [Tooltip("已经放到底座上时显示")]
    [SerializeField] private bool showWhenPlaced = false;

    private void Awake()
    {
        gameObject.SetActive(ShouldShow());
    }

    private bool ShouldShow()
    {
        switch (GameProgress.GetState(itemId))
        {
            case StoryItemState.Uncollected:
                return showWhenUncollected;

            case StoryItemState.InBag:
                return showWhenInBag;

            case StoryItemState.Placed:
                return showWhenPlaced;
        }

        return true;
    }
}
