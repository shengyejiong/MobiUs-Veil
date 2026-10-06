using UnityEngine;

/// <summary>
/// 跨幕剧情标记（只放"剧情进度"这类开关）。
///
/// ⚠️ 道具状态【不在这里】—— 留言卡 / 挂钟 / 纪念牌的"未拿 / 在背包 / 已放置"
///    统一由 GameProgress（数据层）管理，别再各写一套。
/// </summary>
public static class GameState
{
    // ===== 跨幕剧情标记（注释写清：谁写、谁读）=====
    public static bool Act1WindowClosed;   // 写：第一幕 DoorNarrative  |  读：第二幕（窗户外观）
    public static bool Act3NameWritten;    // 写：第三幕              |  读：第四幕

    public static int CurrentAct = 1;      // 当前第几幕（转场、存档用）

    // ⚠️「快速进入 Play 模式」下 static 不会自动清零，这里手动重置
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetAll()
    {
        Act1WindowClosed = false;
        Act3NameWritten = false;
        CurrentAct = 1;
    }
}
