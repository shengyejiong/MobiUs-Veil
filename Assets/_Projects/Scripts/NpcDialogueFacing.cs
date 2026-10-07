using UnityEngine;

/// <summary>说到本 NPC 的对白时转向玩家，对话结束后恢复原姿态。</summary>
public sealed class NpcDialogueFacing : MonoBehaviour
{
    [SerializeField] private SpriteRenderer characterRenderer;
    [SerializeField] private Transform player;
    [SerializeField] private Sprite sideSprite;
    [SerializeField] private string speakerName = "林晚";
    [SerializeField] private bool sideSpriteFacesLeft = true;
    [SerializeField] private bool restoreAfterDialogue = true;

    private Sprite originalSprite;
    private bool originalFlipX;
    private bool facingPlayer;

    private void Awake()
    {
        if (characterRenderer == null) characterRenderer = GetComponent<SpriteRenderer>();
        if (characterRenderer != null)
        {
            originalSprite = characterRenderer.sprite;
            originalFlipX = characterRenderer.flipX;
        }
        if (player == null)
        {
            GameObject target = GameObject.FindGameObjectWithTag("Player");
            if (target != null) player = target.transform;
        }
    }

    private void LateUpdate()
    {
        DialogueManager dm = DialogueManager.Instance;
        if (dm == null || !dm.IsOpen)
        {
            if (facingPlayer && restoreAfterDialogue) RestorePose();
            facingPlayer = false;
            return;
        }
        if (!facingPlayer && dm.CurrentSpeaker != speakerName) return;
        if (characterRenderer == null || player == null || sideSprite == null) return;

        facingPlayer = true;
        characterRenderer.sprite = sideSprite;
        float dx = player.position.x - characterRenderer.transform.position.x;
        if (Mathf.Abs(dx) > 0.05f)
            characterRenderer.flipX = sideSpriteFacesLeft ? dx > 0f : dx < 0f;
    }

    private void RestorePose()
    {
        if (characterRenderer == null) return;
        characterRenderer.sprite = originalSprite;
        characterRenderer.flipX = originalFlipX;
    }

    private void OnDisable()
    {
        if (facingPlayer && restoreAfterDialogue) RestorePose();
        facingPlayer = false;
    }
}
