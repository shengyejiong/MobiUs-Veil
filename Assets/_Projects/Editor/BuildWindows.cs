using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// 命令行打包用（也可以从菜单 Tools/打包 Windows 执行）。
///
/// 命令行用法：
///   Unity.exe -quit -batchmode -projectPath &lt;项目路径&gt; \
///             -executeMethod BuildWindows.BuildWin64 -logFile &lt;日志路径&gt;
///
/// 输出目录：桌面/MobiUs-Veil_Win/（可用 -buildOutput 参数覆盖）
/// </summary>
public static class BuildWindows
{
    private const string DefaultFolderName = "MobiUs-Veil_Win";

    [MenuItem("Tools/打包 Windows 版")]
    public static void BuildWin64()
    {
        string outputDir = ResolveOutputDir();

        // 收集 Build Settings 里勾选的场景
        string[] scenes = GetEnabledScenes();
        if (scenes.Length == 0)
        {
            Debug.LogError("[打包] Build Settings 里没有任何勾选的场景，无法打包");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }

        Debug.Log("[打包] 输出目录: " + outputDir);
        Debug.Log("[打包] 场景 " + scenes.Length + " 个:");
        foreach (string s in scenes) Debug.Log("    " + s);

        Directory.CreateDirectory(outputDir);

        BuildPlayerOptions opt = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(outputDir, "MobiUs-Veil.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(opt);
        BuildSummary summary = report.summary;

        Debug.Log("[打包] 结果: " + summary.result
                  + "  大小: " + (summary.totalSize / 1024f / 1024f).ToString("0.0") + " MB"
                  + "  耗时: " + summary.totalTime.TotalSeconds.ToString("0") + " 秒"
                  + "  错误: " + summary.totalErrors);

        if (summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("[打包] 失败！请看上面日志");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }

        Debug.Log("[打包] ✅ 成功！可执行文件: " + Path.Combine(outputDir, "MobiUs-Veil.exe"));
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }

    private static string ResolveOutputDir()
    {
        // 允许命令行覆盖：-buildOutput "D:\xxx"
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-buildOutput") return args[i + 1];
        }
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (string.IsNullOrEmpty(desktop)) desktop = Directory.GetCurrentDirectory();
        return Path.Combine(desktop, DefaultFolderName);
    }

    private static string[] GetEnabledScenes()
    {
        var list = new System.Collections.Generic.List<string>();
        foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
        {
            if (s != null && s.enabled) list.Add(s.path);
        }
        return list.ToArray();
    }
}
