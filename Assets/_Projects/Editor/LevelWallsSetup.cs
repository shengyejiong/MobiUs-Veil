using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 给关卡加一圈"实体墙"，让角色走不出关卡范围。
///
/// 说明：CinemachineConfiner2D 限制的是【相机】能到哪，不会挡住角色。
/// 角色要"走不出去"必须有真正的实体碰撞体（isTrigger = false）。
///
/// 这个工具会以"相机边界多边形"的范围为基准，在四周生成 4 块厚墙
/// （生成在场景根节点下的 LevelWalls 物体上，可以随时拖动或删掉重来）。
///
/// 菜单：Tools → 关卡 → 给关卡加"走不出去"的墙
/// </summary>
public static class LevelWallsSetup
{
    private const string WallsObjectName = "LevelWalls";

    // 墙的厚度（往关卡外面长，不影响内部可走区域）
    private const float WallThickness = 2f;

    // 往内缩一点，让角色停在边界内侧，不会贴着画面最边缘
    private const float Inset = 0.4f;

    [MenuItem("Tools/关卡/给关卡加“走不出去”的墙", false, 1)]
    public static void CreateWalls()
    {
        // ---------- ① 取范围：优先用相机边界多边形 ----------
        CinemachineConfiner2D confiner = Object.FindFirstObjectByType<CinemachineConfiner2D>();

        if (confiner == null || confiner.BoundingShape2D == null)
        {
            EditorUtility.DisplayDialog(
                "找不到参考范围",
                "场景里没有 CinemachineConfiner2D，或者它没有指定 Bounding Shape 2D。\n" +
                "先用 Tools → 相机 → 一键修好相机 配好相机边界，再来加墙。",
                "好");
            return;
        }

        Bounds bounds = confiner.BoundingShape2D.bounds;

        float minX = bounds.min.x + Inset;
        float maxX = bounds.max.x - Inset;
        float minY = bounds.min.y + Inset;
        float maxY = bounds.max.y - Inset;

        if (maxX - minX < 1f || maxY - minY < 1f)
        {
            EditorUtility.DisplayDialog("范围太小", "算出来的关卡范围小于 1 个单位，请先检查相机边界多边形。", "好");
            return;
        }

        // ---------- ② 创建 / 复用 LevelWalls 物体 ----------
        GameObject walls = GameObject.Find(WallsObjectName);

        if (walls == null)
        {
            walls = new GameObject(WallsObjectName);
            Undo.RegisterCreatedObjectUndo(walls, "Create LevelWalls");
        }

        walls.transform.position = Vector3.zero;
        walls.transform.rotation = Quaternion.identity;
        walls.transform.localScale = Vector3.one;

        // 清掉旧的墙，保证可以反复点这个菜单
        foreach (BoxCollider2D old in walls.GetComponents<BoxCollider2D>())
        {
            Undo.DestroyObjectImmediate(old);
        }

        // ---------- ③ 四周生成 4 块厚墙（往范围外面长）----------
        float centerX = (minX + maxX) * 0.5f;
        float centerY = (minY + maxY) * 0.5f;

        float width = maxX - minX;
        float height = maxY - minY;

        // 下
        AddWall(walls, new Vector2(centerX, minY - WallThickness * 0.5f),
            new Vector2(width + WallThickness * 2f, WallThickness));

        // 上
        AddWall(walls, new Vector2(centerX, maxY + WallThickness * 0.5f),
            new Vector2(width + WallThickness * 2f, WallThickness));

        // 左
        AddWall(walls, new Vector2(minX - WallThickness * 0.5f, centerY),
            new Vector2(WallThickness, height + WallThickness * 2f));

        // 右
        AddWall(walls, new Vector2(maxX + WallThickness * 0.5f, centerY),
            new Vector2(WallThickness, height + WallThickness * 2f));

        // ---------- ④ 保存 ----------
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        string info =
            $"可走区域：({minX:F2}, {minY:F2}) ~ ({maxX:F2}, {maxY:F2})\n" +
            $"墙厚：{WallThickness}，内缩：{Inset}\n" +
            "水平范围来自相机边界多边形";

        Debug.Log("[关卡墙] 已生成 " + WallsObjectName + "\n" + info);

        EditorUtility.DisplayDialog(
            "墙已加好",
            info + "\n\n场景已保存。按 Play 试试走过去，角色应该停在边界内。\n" +
            "想调整范围：改 LevelWalls 上 4 个 BoxCollider2D 的 Offset / Size，或者再点一次这个菜单重来。",
            "好");
    }

    [MenuItem("Tools/关卡/删掉关卡墙", false, 2)]
    public static void RemoveWalls()
    {
        GameObject walls = GameObject.Find(WallsObjectName);

        if (walls == null)
        {
            EditorUtility.DisplayDialog("没找到", $"场景里没有 {WallsObjectName} 物体。", "好");
            return;
        }

        Undo.DestroyObjectImmediate(walls);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log($"[关卡墙] 已删除 {WallsObjectName}");
    }

    private static void AddWall(GameObject parent, Vector2 offset, Vector2 size)
    {
        BoxCollider2D wall = Undo.AddComponent<BoxCollider2D>(parent);
        wall.offset = offset;
        wall.size = size;
        wall.isTrigger = false;
    }
}
