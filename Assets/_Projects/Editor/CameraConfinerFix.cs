using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 相机（Cinemachine）一键修复工具。
///
/// 常见问题：CinemachineConfiner2D 的边界多边形（PolygonCollider2D）默认是"实体碰撞体"，
/// 玩家身上是动态 Rigidbody2D，一开始就站在这个多边形内部 → 物理引擎每帧把玩家往外推，
/// 表现就是"角色走路被挡住 / 被弹开"。
///
/// Cinemachine 的 Confiner 只看多边形的【形状】（内部用 ShapeCache 直接读取顶点，不走物理查询），
/// 所以把边界碰撞体勾成 Is Trigger 完全不影响它工作，但玩家就自由了。
///
/// 菜单：Tools → 相机
/// </summary>
public static class CameraConfinerFix
{
    [MenuItem("Tools/相机/一键修好相机（解锁移动 + 配好死区）", false, 1)]
    public static void FixAll()
    {
        List<string> report = new List<string>();

        // ---------- ① 边界碰撞体改成触发器：这是"移动被挡住"的根因 ----------
        CinemachineConfiner2D confiner = Object.FindFirstObjectByType<CinemachineConfiner2D>();

        if (confiner == null)
        {
            report.Add("✗ 场景里没找到 CinemachineConfiner2D（相机上少了 Confiner 组件？）");
        }
        else
        {
            Collider2D shape = confiner.BoundingShape2D;

            if (shape == null)
            {
                report.Add("✗ CinemachineConfiner2D 的 Bounding Shape 2D 是空的，请先指定边界多边形");
            }
            else if (shape.isTrigger)
            {
                report.Add($"✓ 边界碰撞体「{shape.name}」已经是 Is Trigger，没有挡住玩家");
            }
            else
            {
                Undo.RecordObject(shape, "Confiner IsTrigger");
                shape.isTrigger = true;
                EditorUtility.SetDirty(shape);
                report.Add($"✓ 已把边界碰撞体「{shape.name}」设为 Is Trigger —— 玩家不再被挡住");
            }
        }

        // ---------- ② 死区 / 硬限制 ----------
        CinemachinePositionComposer composer = Object.FindFirstObjectByType<CinemachinePositionComposer>();

        if (composer == null)
        {
            report.Add("✗ 场景里没找到 CinemachinePositionComposer（相机上没有 Position Composer？）");
        }
        else
        {
            Undo.RecordObject(composer, "Composer Dead Zone");

            // Composition 是结构体，先取出来改完再写回去
            var composition = composer.Composition;

            // 死区：目标在这个范围内移动时相机不动（单位是屏幕比例 0~1）
            // 横向 20%、纵向 12%：左右走一小段相机不抖，跳起落下时相机不跟着上下晃
            composition.DeadZone.Enabled = true;
            composition.DeadZone.Size = new Vector2(0.20f, 0.12f);

            // 硬限制：目标跑到屏幕 85% 位置时相机必须跟上，防止角色跑出画面
            composition.HardLimits.Enabled = true;
            composition.HardLimits.Size = new Vector2(0.85f, 0.85f);

            composer.Composition = composition;
            EditorUtility.SetDirty(composer);

            report.Add("✓ 死区：横向 20% / 纵向 12%（走路相机不抖、跳跃相机不上下晃）");
            report.Add("✓ 硬限制：85%（角色不会跑出画面）");
        }

        // ---------- ③ 保存场景 ----------
        Scene scene = EditorSceneManager.GetActiveScene();

        if (scene.IsValid() && scene.isDirty)
        {
            EditorSceneManager.MarkSceneDirty(scene);
        }

        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();

        string text = string.Join("\n", report);
        Debug.Log("[相机修复]\n" + text);

        EditorUtility.DisplayDialog(
            "相机修复完成",
            text + "\n\n场景已保存。回 Unity 按 Play 试试移动，手感不合适就调 " +
            "CinemachineCamera 上 Position Composer 的 Dead Zone / Damping。",
            "好");
    }

    [MenuItem("Tools/相机/按关卡范围重画边界矩形", false, 2)]
    public static void RedrawBoundsFromLevel()
    {
        CinemachineConfiner2D confiner = Object.FindFirstObjectByType<CinemachineConfiner2D>();

        if (confiner == null || confiner.BoundingShape2D == null)
        {
            EditorUtility.DisplayDialog("找不到边界", "场景里没有 CinemachineConfiner2D，或者它没有指定 Bounding Shape 2D。", "好");
            return;
        }

        PolygonCollider2D polygon = confiner.BoundingShape2D as PolygonCollider2D;

        if (polygon == null)
        {
            EditorUtility.DisplayDialog(
                "边界不是多边形",
                $"当前的边界类型是 {confiner.BoundingShape2D.GetType().Name}，这个工具只支持 PolygonCollider2D。",
                "好");
            return;
        }

        // 收集关卡范围：场景里所有渲染物，排除玩家 / UI / 相机相关
        Bounds bounds = new Bounds();
        bool hasBounds = false;
        int counted = 0;

        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is ParticleSystemRenderer)
                {
                    continue;
                }

                if (IsUnderIgnoredObject(renderer.transform))
                {
                    continue;
                }

                if (hasBounds)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
                else
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }

                counted++;
            }
        }

        if (!hasBounds || counted == 0)
        {
            EditorUtility.DisplayDialog("没找到关卡内容", "场景里没有可用于计算范围的渲染物。", "好");
            return;
        }

        const float padding = 0.5f;

        float minX = bounds.min.x - padding;
        float maxX = bounds.max.x + padding;
        float minY = bounds.min.y - padding;
        float maxY = bounds.max.y + padding;

        Undo.RecordObject(polygon, "Redraw Confiner Bounds");
        polygon.points = new[]
        {
            new Vector2(minX, minY),
            new Vector2(minX, maxY),
            new Vector2(maxX, maxY),
            new Vector2(maxX, minY),
        };
        EditorUtility.SetDirty(polygon);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log($"[相机修复] 已按 {counted} 个渲染物重画边界矩形：({minX:F2}, {minY:F2}) ~ ({maxX:F2}, {maxY:F2})");

        EditorUtility.DisplayDialog(
            "边界已重画",
            $"统计了 {counted} 个渲染物\n" +
            $"范围：({minX:F2}, {minY:F2}) ~ ({maxX:F2}, {maxY:F2})\n\n" +
            "如果相机跑出了关卡或者被卡太死，告诉我，我再帮你调。",
            "好");
    }

    private static bool IsUnderIgnoredObject(Transform transform)
    {
        Transform current = transform;

        while (current != null)
        {
            string name = current.name;

            if (name == "Player" || name == "PlayerA" || name == "Canvas" ||
                name == "EventSystem" || name == "CinemachineCamera" ||
                name == "CinemachineConfiner" || name == "Main Camera" ||
                name == "DialogueSystem" || name == "UI")
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }
}
