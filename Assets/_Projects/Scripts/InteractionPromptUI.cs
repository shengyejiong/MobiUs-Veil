using UnityEngine;

/// <summary>
/// 全场景唯一的交互提示（例如 "Press E to react"）。
///
/// 多个 InteractionTrigger 共用它：谁离玩家近、谁需要显示，就由谁"申请"，
/// 先申请的先用，走开或开始对话时自动归还。这样加新物品时不用再复制 UI、也不用拖任何引用。
///
/// 用法：
///   1. 把这个脚本挂在 Canvas 下面【唯一】的那个 InteractionPrompt 物体上
///   2. 在 Inspector 里调好 World Offset（提示相对物体原点的高度）
///   3. 这个物体在场景里必须保持【激活勾选】状态（脚本会在运行时自己控制显隐）
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class InteractionPromptUI : MonoBehaviour
{
    public static InteractionPromptUI Instance { get; private set; }

    [Header("位置")]
    [Tooltip("相对申请者（InteractionRange）的偏移，例如 (0, 1, 0) = 头顶上方 1 个单位")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1f, 0f);

    [Header("相机")]
    [Tooltip("留空会自动使用 Camera.main")]
    [SerializeField] private Camera targetCamera;

    [Header("屏幕边界")]
    [Tooltip("物体贴近屏幕边缘时，把提示拉回屏幕范围内")]
    [SerializeField] private bool clampToScreen = true;

    [Tooltip("拉回时距离屏幕边缘保留的像素")]
    [SerializeField] private Vector2 screenMargin = new Vector2(90f, 40f);

    private RectTransform rect;
    private Canvas canvas;

    // 当前是谁在占用提示（用组件实例本身做身份标识）
    private Component owner;
    private Transform followTarget;

    private void Awake()
    {
        Instance = this;
        rect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private static bool warnedMissingInstance;

    /// <summary>申请显示提示，并让它跟随 target。</summary>
    public static void Show(Component requester, Transform target)
    {
        if (Instance != null)
        {
            Instance.RequestShow(requester, target);
            return;
        }

        // 最常见的两种情况：
        //   1) InteractionPrompt 物体在场景里被取消了勾选（未激活）→ Awake 不会执行 → Instance 为空
        //   2) 忘了给 InteractionPrompt 挂上这个脚本
        if (!warnedMissingInstance)
        {
            warnedMissingInstance = true;
            Debug.LogWarning(
                "InteractionPromptUI 没有生效！请检查 Canvas 下的 InteractionPrompt 物体：" +
                "① 是否挂了 InteractionPromptUI 脚本；② 是否保持【激活勾选】（不能取消勾选）。");
        }
    }

    /// <summary>归还提示，只有当前占用者调用才生效。</summary>
    public static void Hide(Component requester)
    {
        if (Instance != null)
        {
            Instance.RequestHide(requester);
        }
    }

    private void RequestShow(Component requester, Transform target)
    {
        if (requester == null || target == null)
        {
            return;
        }

        // 先到先得：已经有别人在用了就不抢，避免两个物品的提示互相闪烁
        if (owner != null && owner != requester)
        {
            return;
        }

        owner = requester;
        followTarget = target;

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        UpdatePosition();
    }

    private void RequestHide(Component requester)
    {
        // 当前不是你在用，就不要乱关（可能是另一个物品正在显示）
        if (owner != requester)
        {
            return;
        }

        owner = null;
        followTarget = null;
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        // 占用者被删除时（owner 会变成 Unity 的"假 null"），自动把提示收起来
        if (owner == null || followTarget == null)
        {
            owner = null;
            followTarget = null;

            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }

            return;
        }

        UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (rect == null || followTarget == null)
        {
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return;
        }

        Vector3 screenPoint = targetCamera.WorldToScreenPoint(followTarget.position + worldOffset);

        // 目标跑到相机背后：挪出屏幕，避免出现镜像的错误位置
        if (screenPoint.z < 0f)
        {
            rect.position = new Vector3(-10000f, -10000f, 0f);
            return;
        }

        if (clampToScreen)
        {
            screenPoint.x = Mathf.Clamp(screenPoint.x, screenMargin.x, Screen.width - screenMargin.x);
            screenPoint.y = Mathf.Clamp(screenPoint.y, screenMargin.y, Screen.height - screenMargin.y);
        }

        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // Overlay 画布：RectTransform.position 就是屏幕像素坐标，z 必须归零
            rect.position = new Vector3(screenPoint.x, screenPoint.y, 0f);
        }
        else
        {
            // Screen Space - Camera / World Space 画布：需要转换到画布所在平面
            RectTransform canvasRect = canvas.transform as RectTransform;
            Camera uiCamera = canvas.worldCamera != null ? canvas.worldCamera : targetCamera;

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    canvasRect, screenPoint, uiCamera, out Vector3 worldPoint))
            {
                rect.position = worldPoint;
            }
        }
    }
}
