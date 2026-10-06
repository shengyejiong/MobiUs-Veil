using UnityEngine;

public static class Act01Story
{
    public static bool DoorTalked;
    public static bool WindowRequested;
    public static bool WindowClosed;
    public static bool FarewellDone;

    // 「快速进入 Play 模式」（不重载 Domain）时 static 不会自动清零，这里手动重置
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        DoorTalked = false;
        WindowRequested = false;
        WindowClosed = false;
        FarewellDone = false;
    }
}