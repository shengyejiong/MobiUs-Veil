using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class CurtainNarrative : MonoBehaviour
{
    [Header("文本")]
    [SerializeField] private DialogueLine[] lookLines;     // 还没被请求：只能观察
    [SerializeField] private DialogueLine[] closedLines;   // 扣好之后（含爱人的道谢）

    [Header("扣窗反馈")]
    [SerializeField] private SpriteRenderer curtainRenderer;
    [SerializeField] private Sprite closedSprite;                              // 没有美术就先留空
    [SerializeField] private Color closedTint = new Color(0.7f, 0.8f, 1f);     // 占位：偏冷色=关上了
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip closeClip;

    [SerializeField] private DialogueLine thanksLine;   // 爱人："嗯，谢谢。"（只在被请求后追加）
    private bool playerInside;
    private bool hasLooked;   // 本次 Play 里是否已经"看过"窗帘

    private void Update()
    {
        DialogueManager dm = DialogueManager.Instance;

        if (dm == null)
        {
            InteractionPromptUI.Hide(this);
            return;
        }

        bool canInteract = playerInside && !dm.IsOpen;

        if (canInteract) InteractionPromptUI.Show(this, transform);
        else InteractionPromptUI.Hide(this);

        // 防抖三条件：和门、和 InteractionTrigger 保持一致
        bool canPress = canInteract
            && Time.frameCount != dm.LastStateChangeFrame
            && Time.timeScale > 0f;

        if (canPress && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Interact(dm);
        }
    }

        private void Interact(DialogueManager dm)
    {
        Debug.Log($"窗帘: req={Act01Story.WindowRequested} closed={Act01Story.WindowClosed} looked={hasLooked}");

        // ① 已经扣好：只出文本，不重复动作
        if (Act01Story.WindowClosed)
        {
            dm.StartDialogue(closedLines);
            return;
        }

        // ② 爱人已经请求过 → 直接扣
        if (Act01Story.WindowRequested)
        {
            CloseCurtain(dm);
            return;
        }

        // ③ 还没被请求：第一次先"看"（保留策划要的那句观察文本），
        //    第二次就能提前扣 —— 策划案允许"提前扣窗"
        if (!hasLooked)
        {
            hasLooked = true;
            dm.StartDialogue(lookLines);
            return;
        }

        CloseCurtain(dm);
    }

    private void CloseCurtain(DialogueManager dm)
    {
        Act01Story.WindowClosed = true;

        if (curtainRenderer != null)
        {
            if (closedSprite != null) curtainRenderer.sprite = closedSprite;
            else curtainRenderer.color = closedTint;
        }

        if (audioSource != null && closeClip != null)
        {
            audioSource.PlayOneShot(closeClip);
        }

        List<DialogueLine> lines = new List<DialogueLine>();
        if (closedLines != null) lines.AddRange(closedLines);

        // 只有"被请求之后"才说谢谢；提前扣的话她还没开口，不说谢
        if (Act01Story.WindowRequested && thanksLine != null) lines.Add(thanksLine);

        dm.StartDialogue(lines.ToArray());
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.transform.root.CompareTag("Player")) playerInside = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.transform.root.CompareTag("Player"))
        {
            playerInside = false;
            InteractionPromptUI.Hide(this);
        }
    }
}