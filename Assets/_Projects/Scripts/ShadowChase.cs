using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 第三幕：黑影追逐（策划案 330-364 行）
///
/// 规则：
///   · 玩家唯一的出路就是跑到打开的房门，走入出口自动触发
///   · 黑影从后方逼近，速度比玩家慢（玩家 5），给玩家反应时间和可辨认的出口
///   · 被碰到 → 短黑屏 + "你已经知道了。" → 回到床头检查点重来（保留姓名与资料）
///   · 对话期间不追；第一次开门循环后，黑影回到起点重新逼近
///   · 追逐中不能整理背包（B 键提示"现在无法整理背包"）
///
/// 用法：挂在黑影物体上即可。Player / ScreenFader / PlayerMovement 会自动查找。
/// </summary>
public class ShadowChase : MonoBehaviour
{
    public static ShadowChase Instance;

    [Header("追逐参数")]
    [Tooltip("黑影速度（玩家是 5，这里要比玩家慢，建议 2.2 ~ 3）")]
    [SerializeField] private float chaseSpeed = 2.6f;

    [Tooltip("靠到多近算被抓")]
    [SerializeField] private float catchDistance = 0.7f;

    [Tooltip("出现后先给玩家几秒反应时间（这段时间黑影不动）")]
    [SerializeField] private float reactTime = 1.5f;

    [Header("起点 / 检查点")]
    [Tooltip("被抓后玩家回到哪里。房间坐标参考：床 x -6.5~-3.5 / y 0.5~4.0（有碰撞体，别放进去）；床尾下方空地约 (-4.6, 0)")]
    [SerializeField] private Vector2 respawnPosition = new Vector2(-4.6f, 0f);

    [Header("被抓表现")]
    [SerializeField] private ScreenFader screenFader;
    [SerializeField] private DialogueLine[] caughtLines;        // "你已经知道了。"
    [SerializeField] private float blackFadeSeconds = 0.35f;    // 短黑屏（临时改 fader 的时长）
    [SerializeField] private float blackHoldSeconds = 0.4f;

    [Header("追逐中按 B 的提示")]
    [SerializeField] private DialogueLine[] noBackpackLines;    // "现在无法整理背包"

    [Header("重置时要复原的东西")]
    [Tooltip("留空 = 自动按名字找 StageBrace / StageBackdrop / StageMark / StageLightGap")]
    [SerializeField] private GameObject[] stageTraces;

    [Header("音效（可选）")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip appearClip;

    [Header("追逐 BGM（黑影出现时切换）")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioClip chaseBgm;

    private Transform player;
    private PlayerMovement playerMovement;
    private Rigidbody2D playerBody;
    private PlayerInput playerInput;
    private SpriteRenderer[] renderers;
    private Animator animator;
    private Vector2 startPosition;
    private float reactTimer;
    private bool caught;
    private bool loopHandled;
    private SceneTransition sceneTransition;
    private bool animatorWasEnabled;
    private bool inputWasActive;

    /// <summary>黑影正在追人（B 键会被拦下来）</summary>
    public bool IsChasing { get; private set; }
    public bool IsCaught => caught;
    public bool IsDoorSequencePaused { get; private set; }

    private void Awake()
    {
        Instance = this;
        startPosition = transform.position;
        renderers = GetComponentsInChildren<SpriteRenderer>(true);

        // 黑影不参与物理：把自己的碰撞体全关掉（抓到是按距离判定的），
        // 免得它变成一堵会移动的墙把玩家卡住
        foreach (var c in GetComponentsInChildren<Collider2D>(true))
        {
            if (c != null) c.enabled = false;
        }

        // 兜底：自动找玩家和淡入淡出
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerMovement = p.GetComponent<PlayerMovement>();
            playerBody = p.GetComponent<Rigidbody2D>();
            playerInput = p.GetComponent<PlayerInput>();
        }
        if (screenFader == null) screenFader = FindFirstObjectByType<ScreenFader>();
        sceneTransition = FindFirstObjectByType<SceneTransition>();

        // 兜底：自动找四块舞台痕迹（它们是隐藏状态，所以不能用 GameObject.Find）
        if (stageTraces == null || stageTraces.Length == 0)
        {
            string[] names = { "StageBrace", "StageBackdrop", "StageMark", "StageLightGap" };
            var list = new System.Collections.Generic.List<GameObject>();
            var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in roots)
            {
                foreach (var tr in root.GetComponentsInChildren<Transform>(true))
                {
                    if (System.Array.IndexOf(names, tr.name) >= 0) list.Add(tr.gameObject);
                }
            }
            stageTraces = list.ToArray();
            Debug.Log($"[黑影] 自动找到 {stageTraces.Length} 块舞台痕迹");
        }

        SetVisible(false);      // 阅读资料期间不出现（策划案 311 行）
    }

    private void Update()
    {
        if (caught || IsDoorSequencePaused || player == null) return;
        if (Time.timeScale <= 0f || (sceneTransition != null && sceneTransition.IsTransitioning)) return;

        // ---- 什么时候出现：名字写完 + 「是否拿走」询问答完 ----
        if (!IsChasing)
        {
            if (Act03Story.NameWritten && Act03Story.PlaqueAsked &&
    GameProgress.GetState(StoryItemId.MemorialPlaque) != StoryItemState.Uncollected)
            {
                StartChase();
            }
            return;
        }

        // ---- 第一次开门（空间循环）之后：黑影回到起点，重新给反应时间 ----
        if (Act03Story.DoorLooped && !loopHandled)
        {
            RestartAfterDoorLoop();
        }

        // ---- 对话期间不追（策划案 345 行）----
        DialogueManager dm = DialogueManager.Instance;
        if (dm != null && dm.IsOpen) return;

        // ---- 选项面板开着时也不追（对话 / 选择期间暂停追逐）----
        ChoiceUI cui = ChoiceUI.Get();
        if (cui != null && cui.IsOpen) return;

        // ---- 反应时间 ----
        if (reactTimer > 0f)
        {
            reactTimer -= Time.deltaTime;
            return;
        }

        // ---- 朝玩家移动（比玩家慢）----
        Vector2 pos = transform.position;
        Vector2 target = player.position;
        transform.position = Vector2.MoveTowards(pos, target, chaseSpeed * Time.deltaTime);

        // ---- 抓到判定 ----
        if (Vector2.Distance(transform.position, player.position) <= catchDistance)
        {
            StartCoroutine(CatchRoutine());
        }
    }

    private void StartChase()
    {
        IsChasing = true;
        loopHandled = false;
        SetVisible(true);
        reactTimer = reactTime;

        if (bgmSource != null && chaseBgm != null)
        {
            bgmSource.Stop();
            bgmSource.clip = chaseBgm;
            bgmSource.loop = true;
            bgmSource.Play();
        }

        if (audioSource != null && appearClip != null) audioSource.PlayOneShot(appearClip);

        Debug.Log("[黑影] 出现了 —— 房门打开，跑向门口自动出门");
    }

    public void SetDoorSequencePaused(bool paused)
    {
        if (IsDoorSequencePaused == paused) return;
        IsDoorSequencePaused = paused;
        if (playerInput != null)
        {
            if (paused)
            {
                inputWasActive = playerInput.inputIsActive;
                playerInput.DeactivateInput();
            }
            else if (inputWasActive) playerInput.ActivateInput();
        }
        if (animator != null)
        {
            if (paused)
            {
                animatorWasEnabled = animator.enabled;
                animator.enabled = false;
            }
            else animator.enabled = animatorWasEnabled;
        }
        if (playerMovement != null)
        {
            DialogueManager dm = DialogueManager.Instance;
            playerMovement.SetMovementLocked(paused || (dm != null && dm.IsOpen));
        }
    }

    /// <summary>第一次出门循环：主角回到床下检查点，黑影重新从起点逼近。</summary>
    public void RestartAfterDoorLoop()
    {
        if (!IsChasing || caught || player == null) return;

        loopHandled = true;
        if (playerMovement != null) playerMovement.SetMovementLocked(true);
        MovePlayerToCheckpoint();
        transform.position = startPosition;
        reactTimer = reactTime;

        DialogueManager dm = DialogueManager.Instance;
        if (playerMovement != null) playerMovement.SetMovementLocked(dm != null && dm.IsOpen);
        Debug.Log("[黑影] 空间循环 → 主角回到床下，黑影回到起点，继续第二轮追逐");
    }

    private void MovePlayerToCheckpoint()
    {
        if (playerBody != null)
        {
            playerBody.linearVelocity = Vector2.zero;
            playerBody.angularVelocity = 0f;
            playerBody.position = respawnPosition;
        }
        else if (player != null)
        {
            player.position = new Vector3(respawnPosition.x, respawnPosition.y, player.position.z);
        }
    }

    /// <summary>被抓：短黑屏 + "你已经知道了。" + 回到床头检查点</summary>
    private IEnumerator CatchRoutine()
    {
        caught = true;

        Debug.Log("[黑影] 被抓到 → 回到床头检查点");

        if (playerMovement != null) playerMovement.SetMovementLocked(true);

        float oldFade = 0f;
        if (screenFader != null)
        {
            oldFade = screenFader.fadeDuration;
            screenFader.fadeDuration = blackFadeSeconds;
            yield return screenFader.FadeTo(1f);
        }

        if (blackHoldSeconds > 0f) yield return new WaitForSecondsRealtime(blackHoldSeconds);

        // 黑屏上显示"你已经知道了。"
        DialogueManager dm = DialogueManager.Instance;
        if (dm != null && caughtLines != null && caughtLines.Length > 0)
        {
            dm.StartDialogue(caughtLines);
            while (dm != null && dm.IsOpen) yield return null;
        }

        // ---- 复位 ----
        MovePlayerToCheckpoint();
        transform.position = startPosition;

        // 房门与视觉阶段恢复第一轮（策划案 360 行）
        Act03Story.DoorLooped = false;
        loopHandled = false;
        if (stageTraces != null)
        {
            foreach (GameObject g in stageTraces) { if (g != null) g.SetActive(false); }
        }

        if (screenFader != null)
        {
            yield return screenFader.FadeTo(0f);
            screenFader.fadeDuration = oldFade;
        }

        reactTimer = reactTime;     // 再给一次反应时间
        if (playerMovement != null) playerMovement.SetMovementLocked(false);

        caught = false;
    }

    /// <summary>追逐中按 B 时由 GameMenuController 调用</summary>
    public void ShowCantUseBackpack()
    {
        if (IsDoorSequencePaused) return;
        Debug.Log("[黑影] 现在无法整理背包");
        DialogueManager dm = DialogueManager.Instance;
        if (dm != null && noBackpackLines != null && noBackpackLines.Length > 0 && !dm.IsOpen)
        {
            dm.StartDialogue(noBackpackLines);
        }
    }

    /// <summary>只控制"看得见/看不见"—— 不碰碰撞体（抓到是按距离判定，黑影不需要碰撞体）
    /// 如果 ghost 有 Animator，一并停掉，避免动画把渲染器又打开</summary>
    private void SetVisible(bool visible)
    {
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (animator != null) animator.enabled = visible;

        if (renderers == null) return;
        foreach (var r in renderers) { if (r != null) r.enabled = visible; }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(respawnPosition, 0.5f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, catchDistance);
    }
}
