using UnityEngine;

/// <summary>
/// 第二幕（暮）本幕状态。
/// 只有"本幕内部"用得到的东西放这里；跨幕的写进 GameState；道具状态用 GameProgress。
/// </summary>
public static class Act02Story
{
    public static bool PhoneChecked;   // 是否调查过手机
    public static bool ClockSolved;    // 是否把墙钟拨到 22:47（门解锁）

    // 注意：挂钟"有没有被拿走"不在这里记录 —— 统一读 GameProgress.GetState(StoryItemId.Clock)

    // ⚠️「快速进入 Play 模式」下 static 不会自动清零，这里手动重置
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetAll()
    {
        PhoneChecked = false;
        ClockSolved = false;
    }
}
