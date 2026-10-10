using UnityEngine;

/// <summary>第一幕窗外树叶风声：读取关窗状态，独立于 BGM 控制音量。</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class Act01WindowAmbience : MonoBehaviour
{
    [SerializeField] private AudioClip windClip;

    [Header("风声音量")]
    [SerializeField, Range(0f, 1f)] private float openVolume = 0.28f;
    [SerializeField, Range(0f, 1f)] private float closedVolume = 0.045f;
    [Tooltip("关窗后风声渐弱的时间，使用游戏时间，暂停时停止渐变")]
    [SerializeField, Min(0.01f)] private float closeFadeSeconds = 1f;
    [Tooltip("进入场景时，风声从安静渐入的时间")]
    [SerializeField, Min(0f)] private float startFadeSeconds = 0.6f;

    private AudioSource audioSource;
    private float curtainBlend;
    private float startBlend;
    private bool pausedByGame;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f;
    }

    private void OnEnable()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (windClip == null)
        {
            Debug.LogWarning("第一幕窗外风声没有分配音频。", this);
            return;
        }

        curtainBlend = Act01Story.WindowClosed ? 1f : 0f;
        startBlend = startFadeSeconds <= 0f ? 1f : 0f;
        pausedByGame = false;
        audioSource.clip = windClip;
        ApplyVolume();
        audioSource.Play();
        if (Time.timeScale <= 0f)
        {
            audioSource.Pause();
            pausedByGame = true;
        }
    }

    private void Update()
    {
        if (audioSource == null || windClip == null) return;

        // AudioSource 不会自动随 timeScale 暂停；恢复时从原播放位置继续。
        if (Time.timeScale <= 0f)
        {
            if (!pausedByGame)
            {
                audioSource.Pause();
                pausedByGame = true;
            }
            return;
        }
        if (pausedByGame)
        {
            audioSource.UnPause();
            pausedByGame = false;
        }

        float target = Act01Story.WindowClosed ? 1f : 0f;
        curtainBlend = Mathf.MoveTowards(curtainBlend, target, Time.deltaTime / Mathf.Max(0.01f, closeFadeSeconds));
        startBlend = startFadeSeconds <= 0f ? 1f
            : Mathf.MoveTowards(startBlend, 1f, Time.deltaTime / startFadeSeconds);
        ApplyVolume();
    }

    private void ApplyVolume()
    {
        float closedBlend = Mathf.SmoothStep(0f, 1f, curtainBlend);
        float fadeIn = Mathf.SmoothStep(0f, 1f, startBlend);
        audioSource.volume = Mathf.Clamp01(Mathf.Lerp(openVolume, closedVolume, closedBlend)) * fadeIn;
    }

    private void OnDisable()
    {
        if (audioSource != null) audioSource.Stop();
        pausedByGame = false;
    }
}
