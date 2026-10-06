using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 一键美化对话框与交互提示（字号 / 字体 / 颜色 / 对齐 / 行距）。
///
/// 默认是一套「深色面板 + 米白正文 + 暖色说话人」的叙事游戏配色，
/// 数值都写在下面的常量里，不喜欢可以直接改这个文件，或者在 Inspector 里微调。
///
/// 菜单：Tools → UI → 美化当前场景 / 美化所有场景（批量）
/// </summary>
public static class DialogueUiPolish
{
    private const string CjkFontPath = "Assets/_Projects/Fonts/CJK SDF.asset";

    // ---------------- 想改风格就改这里 ----------------
    private static readonly Color PanelColor = new Color(0.06f, 0.07f, 0.11f, 0.92f);   // 对话框底色：深蓝黑
    private static readonly Color SpeakerColor = new Color(1.00f, 0.82f, 0.55f, 1f);    // 说话人：暖琥珀
    private static readonly Color BodyColor = new Color(0.94f, 0.94f, 0.94f, 1f);       // 正文：米白
    private static readonly Color HintColor = new Color(1.00f, 1.00f, 1.00f, 0.55f);    // 继续提示：半透明白
    private static readonly Color PromptColor = new Color(1.00f, 0.97f, 0.90f, 1f);     // 交互提示：暖白

    private const float SpeakerSize = 34f;
    private const float BodySize = 30f;
    private const float HintSize = 20f;
    private const float PromptSize = 26f;

    private const float BodyLineSpacing = 8f;   // 中文正文行距，5~12 比较舒服
    // ---------------------------------------------------

    [MenuItem("Tools/UI/美化当前场景的对话框与提示", false, 1)]
    public static void PolishCurrentScene()
    {
        List<string> report = new List<string>();
        int changed = ApplyToActiveScene(report);

        if (changed == 0)
        {
            EditorUtility.DisplayDialog("没找到 UI",
                "当前场景里没找到 DialoguePanel / SpeakerText / ContentText / ClickHint / InteractionPrompt。\n" +
                "请先打开有对话框的场景（例如 Act01_Longing）。", "好");
            return;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        ShowReport(report);
    }

    [MenuItem("Tools/UI/美化所有场景（批量）", false, 2)]
    public static void PolishAllScenes()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        string originalScene = SceneManager.GetActiveScene().path;
        List<string> report = new List<string>();
        int touchedScenes = 0;

        foreach (EditorBuildSettingsScene entry in EditorBuildSettings.scenes)
        {
            if (!entry.enabled)
            {
                continue;
            }

            Scene scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Single);
            List<string> sceneReport = new List<string>();

            if (ApplyToActiveScene(sceneReport) > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                touchedScenes++;
                report.Add($"── {System.IO.Path.GetFileNameWithoutExtension(entry.path)}");
                report.AddRange(sceneReport);
            }
        }

        if (!string.IsNullOrEmpty(originalScene))
        {
            EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);
        }

        report.Insert(0, $"共美化了 {touchedScenes} 个场景。");
        ShowReport(report);
    }

    /// <summary>把美化套用到当前打开的场景，返回改动的元素个数。</summary>
    private static int ApplyToActiveScene(List<string> report)
    {
        TMP_FontAsset cjk = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CjkFontPath);

        if (cjk == null)
        {
            Debug.LogError($"[UI 美化] 找不到中文字体：{CjkFontPath}");
            return 0;
        }

        int changed = 0;

        // ① 对话框底色：白半透明 → 深色，白字才看得清
        GameObject panel = FindInScene("DialoguePanel");

        if (panel != null)
        {
            Image image = panel.GetComponent<Image>();

            if (image != null && image.color != PanelColor)
            {
                Undo.RecordObject(image, "Polish Dialogue UI");
                image.color = PanelColor;
                EditorUtility.SetDirty(image);
                report.Add("  • 对话框底色 → 深蓝黑（白字更清楚）");
                changed++;
            }
        }

        // ② 说话人：暖色 + 加粗 + 字号降到 34
        changed += ApplyText("SpeakerText", cjk, SpeakerSize, SpeakerColor,
            FontStyles.Bold, 0f, TextAlignmentOptions.TopLeft, report, "说话人");

        // ③ 正文：米白 + 字号 30 + 行距 8
        changed += ApplyText("ContentText", cjk, BodySize, BodyColor,
            FontStyles.Normal, BodyLineSpacing, TextAlignmentOptions.TopLeft, report, "正文");

        // ④ 继续提示：小一号、半透明、右下角
        changed += ApplyText("ClickHint", cjk, HintSize, HintColor,
            FontStyles.Normal, 0f, TextAlignmentOptions.BottomRight, report, "继续提示");

        // ⑤ 交互提示：字号 26、居中、暖白
        changed += ApplyText("InteractionPrompt", cjk, PromptSize, PromptColor,
            FontStyles.Normal, 0f, TextAlignmentOptions.Center, report, "交互提示");

        return changed;
    }

    private static int ApplyText(string objectName, TMP_FontAsset font, float size, Color color,
        FontStyles style, float lineSpacing, TextAlignmentOptions alignment,
        List<string> report, string label)
    {
        GameObject go = FindInScene(objectName);

        if (go == null)
        {
            return 0;
        }

        TMP_Text text = go.GetComponent<TMP_Text>();

        if (text == null)
        {
            return 0;
        }

        Undo.RecordObject(text, "Polish Dialogue UI");

        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.fontStyle = style;
        text.lineSpacing = lineSpacing;
        text.alignment = alignment;
        text.enableAutoSizing = false;

        EditorUtility.SetDirty(text);
        text.SetAllDirty();

        report.Add($"  • {label}：字号 {size:0}，字体改用中文字体，对齐/颜色已调整");
        return 1;
    }

    private static GameObject FindInScene(string objectName)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName)
                {
                    return child.gameObject;
                }
            }
        }

        return null;
    }

    private static void ShowReport(List<string> report)
    {
        string text = string.Join("\n", report);
        Debug.Log("[UI 美化]\n" + text);

        EditorUtility.DisplayDialog(
            "美化完成",
            text + "\n\n场景已保存。\n" +
            "想微调：直接在 Inspector 里改字号/颜色，或者改 Assets/_Projects/Editor/DialogueUiPolish.cs 顶部的常量后重新点一次。\n" +
            "不满意可以 Ctrl+Z 撤销。",
            "好");
    }
}
