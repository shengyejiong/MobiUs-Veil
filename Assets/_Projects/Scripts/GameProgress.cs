using UnityEngine;

public enum StoryItemId
{
    MessageCard,
    Clock,
    MemorialPlaque
}
public enum StoryItemState
{
    Uncollected,
    InBag,
    Placed
}
public static class GameProgress
{
    private static StoryItemState messageCardState = StoryItemState.Uncollected;
    private static StoryItemState clockState = StoryItemState.Uncollected;
    private static StoryItemState memorialPlaqueState = StoryItemState.Uncollected;

    public static StoryItemState GetState(StoryItemId itemId)
    {
        switch (itemId)
        {
            case StoryItemId.MessageCard:
                return messageCardState;
            case StoryItemId.Clock:
                return clockState;
            case StoryItemId.MemorialPlaque:
                return memorialPlaqueState;
            default:
                throw new System.ArgumentOutOfRangeException(itemId.ToString(), "Invalid StoryItemId");
        }

    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetForNewGame()
    {
        messageCardState = StoryItemState.Uncollected;
        clockState = StoryItemState.Uncollected;
        memorialPlaqueState = StoryItemState.Uncollected;
    }

    public static bool TryCollect(StoryItemId itemId)
    {
        switch (itemId)
        {
            case StoryItemId.MessageCard:
                if (messageCardState == StoryItemState.Uncollected)
                {
                    messageCardState = StoryItemState.InBag;
                    return true;
                }
                break;
            case StoryItemId.Clock:
                if (clockState == StoryItemState.Uncollected)
                {
                    clockState = StoryItemState.InBag;
                    return true;
                }
                break;
            case StoryItemId.MemorialPlaque:
                if (memorialPlaqueState == StoryItemState.Uncollected)
                {
                    memorialPlaqueState = StoryItemState.InBag;
                    return true;
                }
                break;
        }
        return false; // 物品已经拾取或放置，无法再次拾取
    }

    //玩家选的物品和需要的物品是否匹配，如果匹配则放置成功，返回true，否则返回false
    public static bool TryPlace(StoryItemId selectedItemId, StoryItemId requiredItemId)
    {
        switch(selectedItemId)
        {
            case StoryItemId.MessageCard:
                if (messageCardState == StoryItemState.InBag && selectedItemId == requiredItemId)
                {
                    messageCardState = StoryItemState.Placed;
                    return true;
                }
                break;
            case StoryItemId.Clock:
                if (clockState == StoryItemState.InBag && selectedItemId == requiredItemId)
                {
                    clockState = StoryItemState.Placed;
                    return true;
                }
                break;
            case StoryItemId.MemorialPlaque:
                if (memorialPlaqueState == StoryItemState.InBag && selectedItemId == requiredItemId)
                {
                    memorialPlaqueState = StoryItemState.Placed;
                    return true;
                }
                break;
        } 
        
        return false;
    }

}
