using UnityEngine;

/// <summary>
/// 第三幕（墓）本幕状态。
/// ⚠️ 策划案特别提醒：追逐的"循环阶段"不能只靠"当前是第三幕"判断，所以这里单独记。
/// </summary>
public static class Act03Story
{
    public static bool FactsRead;      // 死亡资料已读（医院通知 + 事后记录）
    public static bool NameWritten;    // 纪念牌已写下"林晚"
    public static bool PlaqueTaken;    // 纪念牌已拿走（GameProgress 也记，这里方便判断）
    public static bool DoorLooped;     // 第一次开门（空间循环）已经发生

    // ⚠️「快速进入 Play 模式」下 static 不会自动清零，这里手动重置
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetAll()
    {
        FactsRead = false;
        NameWritten = false;
        PlaqueTaken = false;
        DoorLooped = false;
    }
}
