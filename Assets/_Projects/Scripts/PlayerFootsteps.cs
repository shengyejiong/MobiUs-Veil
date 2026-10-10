using UnityEngine;

/// <summary>按实际物理位移播放木地板脚步声，暂停、对话锁定及传送时重置。</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Rigidbody2D), typeof(PlayerMovement))]
public sealed class PlayerFootsteps : MonoBehaviour
{
    [Header("木地板脚步声")]
    [SerializeField] private AudioClip[] woodClips;
    [SerializeField, Min(0.05f)] private float stepInterval = 0.32f;
    [SerializeField, Range(0f, 1f)] private float volume = 0.25f;

    private Rigidbody2D body;
    private PlayerMovement movement;
    private AudioSource audioSource;
    private Vector2 previousPosition;
    private float elapsed;
    private int lastClipIndex = -1;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();
        GameObject emitter = new GameObject("FootstepAudio");
        emitter.transform.SetParent(transform, false);
        audioSource = emitter.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        previousPosition = body.position;
    }

    private void OnEnable()
    {
        if (body != null) previousPosition = body.position;
        ResetPlayback();
    }

    private bool CanPlay()
    {
        return Time.timeScale > 0f && body != null && body.simulated
            && movement != null && movement.isActiveAndEnabled
            && !movement.IsMovementLocked && woodClips != null && woodClips.Length > 0;
    }

    private void Update()
    {
        // FixedUpdate 在暂停时不执行，所以在这里及时停止声音。
        if (!CanPlay())
        {
            if (body != null) previousPosition = body.position;
            ResetPlayback();
        }
    }

    private void FixedUpdate()
    {
        Vector2 position = body.position;
        float distance = Vector2.Distance(position, previousPosition);
        previousPosition = position;

        // 检查实际移动；较大的瞬时位移视为传送，不发出脚步声。
        if (!CanPlay() || distance < 0.002f || distance > 1f)
        {
            ResetPlayback();
            return;
        }

        elapsed += Time.fixedDeltaTime;
        if (elapsed < Mathf.Max(0.05f, stepInterval)) return;
        elapsed = 0f;

        int count = woodClips.Length;
        int index = Random.Range(0, count);
        if (count > 1 && lastClipIndex >= 0 && lastClipIndex < count)
        {
            index = Random.Range(0, count - 1);
            if (index >= lastClipIndex) index++;
        }
        if (woodClips[index] == null) return;

        lastClipIndex = index;
        audioSource.volume = volume;
        audioSource.PlayOneShot(woodClips[index]);
    }

    private void ResetPlayback()
    {
        elapsed = 0f;
        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
    }

    private void OnDisable()
    {
        ResetPlayback();
    }
}
