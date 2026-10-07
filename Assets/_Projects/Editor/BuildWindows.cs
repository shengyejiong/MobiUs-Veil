using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    internal static AudioClip PrivateMenuMusic { get; private set; }

    [MenuItem("Tools/打包 Windows 版（原主菜单音乐）")]
    public static void BuildWin64WithPrivateMenuMusic()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string sourcePath = Path.Combine(Directory.GetParent(projectRoot).FullName,
            "PrivateAssets", "Hmix", "砂の雫.ogg");
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "-privateMenuMusic") sourcePath = args[i + 1];

        if (!File.Exists(sourcePath))
            throw new BuildFailedException("原主菜单音乐的私人副本不存在：" + sourcePath);

        const string assetPath = "Assets/_LocalBuildAudio/MenuBgm.ogg";
        string localPath = Path.Combine(projectRoot, assetPath);
        Directory.CreateDirectory(Path.GetDirectoryName(localPath));
        File.Copy(sourcePath, localPath, true);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
        if (importer == null) throw new BuildFailedException("无法导入私人主菜单音乐。");
        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.Streaming;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = 0.7f;
        importer.defaultSampleSettings = settings;
        importer.SaveAndReimport();

        PrivateMenuMusic = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
        if (PrivateMenuMusic == null) throw new BuildFailedException("私人主菜单音乐导入失败。");
        try
        {
            BuildWin64();
        }
        finally
        {
            PrivateMenuMusic = null;
        }
    }

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

// Only changes the scene being serialized into the player, not its saved YAML.
public sealed class PrivateMenuMusicSceneProcessor : IProcessSceneWithReport
{
    public int callbackOrder => 0;

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        if (report == null || scene.name != "MainMenu" || BuildWindows.PrivateMenuMusic == null) return;

        int replaced = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (AudioSource source in root.GetComponentsInChildren<AudioSource>(true))
            {
                if (source.gameObject.name != "BGM") continue;
                source.clip = BuildWindows.PrivateMenuMusic;
                replaced++;
            }
        }
        if (replaced != 1)
            throw new BuildFailedException("MainMenu 应有且只有一个 BGM AudioSource，实际为：" + replaced);
        Debug.Log("[构建音乐] MainMenu 已使用私人原曲，保存的场景继续保留 CC0 音乐。");
    }
}
