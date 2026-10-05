using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 一键让项目支持中文（解决 "点击继续" 显示成方块的问题）。
///
/// 原理：
///   1) 用系统里的中文字体（微软雅黑 / 黑体 / 等线 / 宋体）生成一个 TMP 字体资源，
///      模式是 Dynamic OS —— 汉字在运行时按需加入图集，所以以后对白里出现新汉字不用重新生成。
///   2) 把这个中文字体挂到项目里拉丁字体（Anton SDF、LiberationSans SDF）的 Fallback 列表上。
///      于是英文继续用原来的字体，汉字自动回退到中文字体。
///
/// 用法：菜单 Tools → 中文支持 → 一键配置中文字体
/// </summary>
public static class ChineseFontSetup
{
    private const string OutputFolder = "Assets/_Projects/Fonts";
    private const string OutputPath = OutputFolder + "/CJK SDF.asset";

    // 需要挂回退字体的拉丁字体（项目里在用的）
    private static readonly string[] FallbackTargets =
    {
        "Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Anton SDF.asset",
        "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset",
    };

    // 按优先级尝试的系统中文字体（Windows 自带）
    private static readonly string[,] SystemFontCandidates =
    {
        { "Microsoft YaHei", "Regular" },
        { "Microsoft YaHei", "Normal" },
        { "SimHei", "Regular" },
        { "DengXian", "Regular" },
        { "SimSun", "Regular" },
    };

    [MenuItem("Tools/中文支持/一键配置中文字体", false, 1)]
    public static void Setup()
    {
        TMP_FontAsset cjk = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputPath);

        if (cjk == null)
        {
            cjk = CreateCjkFontAsset();

            if (cjk == null)
            {
                return;
            }
        }
        else
        {
            Debug.Log($"[中文支持] 复用已存在的字体资源：{OutputPath}");
        }

        int changed = ApplyFallback(cjk);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[中文支持] 配置完成：中文字体已挂到 {changed} 个拉丁字体的 Fallback 上。" +
                  "现在汉字（例如「点击继续」）可以正常显示了，场景和预制体都不用改。");

        EditorUtility.DisplayDialog(
            "中文支持配置完成",
            $"中文字体：{Path.GetFileNameWithoutExtension(OutputPath)}\n" +
            $"已挂到 {changed} 个拉丁字体的 Fallback 上。\n\n" +
            "回到场景里 Play 一下，「点击继续」应该正常显示中文了。",
            "好");
    }

    [MenuItem("Tools/中文支持/还原（移除中文字体回退）", false, 2)]
    public static void RemoveFallback()
    {
        TMP_FontAsset cjk = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputPath);
        int changed = 0;

        foreach (string path in FallbackTargets)
        {
            TMP_FontAsset latin = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);

            if (latin == null || latin.fallbackFontAssetTable == null)
            {
                continue;
            }

            if (cjk != null && latin.fallbackFontAssetTable.Remove(cjk))
            {
                EditorUtility.SetDirty(latin);
                changed++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[中文支持] 已从 {changed} 个字体上移除中文回退。");
    }

    private static TMP_FontAsset CreateCjkFontAsset()
    {
        if (!AssetDatabase.IsValidFolder(OutputFolder))
        {
            Directory.CreateDirectory(OutputFolder);
            AssetDatabase.Refresh();
        }

        for (int i = 0; i < SystemFontCandidates.GetLength(0); i++)
        {
            string family = SystemFontCandidates[i, 0];
            string style = SystemFontCandidates[i, 1];

            TMP_FontAsset created = null;

            try
            {
                // 这个重载会用系统字体创建 Dynamic OS 字体资源：
                // 90 号采样、9 像素 padding、SDFAA、1024 图集、支持多图集
                created = TMP_FontAsset.CreateFontAsset(family, style, 90);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[中文支持] 用系统字体「{family} {style}」生成失败：{e.Message}");
            }

            if (created == null)
            {
                continue;
            }

            created.name = "CJK SDF";

            AssetDatabase.CreateAsset(created, OutputPath);

            if (created.material != null)
            {
                AssetDatabase.AddObjectToAsset(created.material, created);
            }

            if (created.atlasTextures != null)
            {
                foreach (Texture2D texture in created.atlasTextures)
                {
                    if (texture != null)
                    {
                        AssetDatabase.AddObjectToAsset(texture, created);
                    }
                }
            }

            AssetDatabase.SaveAssets();

            Debug.Log($"[中文支持] 已用系统字体「{family} {style}」生成字体资源：{OutputPath}");
            return created;
        }

        Debug.LogError("[中文支持] 没找到可用的系统中文字体（试过：微软雅黑 / 黑体 / 等线 / 宋体）。" +
                       "请把任意中文 ttf 放进 Assets 后手动用 Window → TextMeshPro → Font Asset Creator 生成，再告诉我。");
        return null;
    }

    private static int ApplyFallback(TMP_FontAsset cjk)
    {
        int changed = 0;

        foreach (string path in FallbackTargets)
        {
            TMP_FontAsset latin = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);

            if (latin == null)
            {
                Debug.LogWarning($"[中文支持] 找不到字体资源：{path}");
                continue;
            }

            if (latin.fallbackFontAssetTable == null)
            {
                latin.fallbackFontAssetTable = new List<TMP_FontAsset>();
            }

            if (!latin.fallbackFontAssetTable.Contains(cjk))
            {
                latin.fallbackFontAssetTable.Add(cjk);
                EditorUtility.SetDirty(latin);
                changed++;
                Debug.Log($"[中文支持] 已把中文字体挂到：{Path.GetFileName(path)}");
            }
            else
            {
                Debug.Log($"[中文支持] 已经挂过了：{Path.GetFileName(path)}");
            }
        }

        return changed;
    }
}
