﻿﻿using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public class Act04Flow : MonoBehaviour
{
    [SerializeField] private Transform curtainRoot;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private float openDistance = 16.5f;
    [SerializeField] private float openDuration = 2f;

    [Header("Ļ������ǽ��")]
    [SerializeField] private Transform coveringCurtain;
    [SerializeField, Min(0.1f)] private float coverDuration = 0.8f;
    [SerializeField] private GameObject stageStep;

    [Header("��̨��ͷ")]
    [SerializeField] private CinemachineCamera stageCamera;
    [SerializeField] private CinemachineBrain cameraBrain;
    [SerializeField, Min(0.1f)] private float cameraBlendDuration = 2f;

    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private SpriteRenderer memoryDisplay;

    [Header("���仭�浭�뵭��")]
    [SerializeField] private Sprite memoryOneSprite;
    [SerializeField] private Sprite memoryTwoSprite;
    [SerializeField] private Sprite memoryThreeSprite;
    [SerializeField, Range(0f, 0.99f)] private float memoryMaxAlpha = 0.85f;
    [SerializeField, Min(0.01f)] private float memoryFadeDuration = 0.7f;
    [SerializeField, Min(0f)] private float memoryMinHold = 1.5f;

    [SerializeField] private DialogueLine[] memoryOneLines;
    [SerializeField] private DialogueLine[] memoryTwoLines;
    [SerializeField] private DialogueLine[] memoryThreeLines;
    [SerializeField] private DialogueLine[] afterMemoryLines;
    [SerializeField] private GameObject stagePedestals;
    [SerializeField] private ScreenFader screenFader;

    [Header("结局自动切换")]
    [Tooltip("床边结局演完后，自动切换到哪个场景")]
    [SerializeField] private string nextSceneName = "Ending_Reality";

    [Tooltip("舞台撤具后，玩家 N 秒内没去调查床就自动进结局（0 = 不自动，慢慢逛）")]
    [SerializeField, Min(0f)] private float autoAdvanceSeconds = 5f;

    public bool IsGameEnded { get; private set; }
    private Vector3 closedCurtainPosition;

    public bool CanUseBed { get; private set; }//��������Ļ�Ƿ��Ѿ�����

    public bool IsPlayingSequence { get; private set; }

    private bool openingStarted;//��Ļ
    private bool stageCameraActive;
    private CinemachineBlendDefinition savedCameraBlend;

    private bool AllItemsPlaced()
    {
        return GameProgress.GetState(StoryItemId.MessageCard) == StoryItemState.Placed
            && GameProgress.GetState(StoryItemId.Clock) == StoryItemState.Placed
            && GameProgress.GetState(StoryItemId.MemorialPlaque) == StoryItemState.Placed;
    }

    private void Start()
    {
        if (memoryDisplay != null)
        {
            SetMemoryAlpha(0f);
            memoryDisplay.enabled = false;
        }
    }

    private void Update()
    {
        if (IsGameEnded) return;

        if (openingStarted) return;//���ظ�����

        DialogueManager dm = DialogueManager.Instance;
        if (dm == null || dm.IsOpen || Time.timeScale == 0f) return;

        if (!AllItemsPlaced()) return;

        openingStarted = true;
        IsPlayingSequence = true;
        StartCoroutine(OpenCurtain());

    }

    private IEnumerator OpenCurtain()
    {
        playerInput.DeactivateInput();
        playerMovement.SetMovementLocked(true);
        yield return SetStageCamera(true);
        Vector3 startPos = curtainRoot.position;
        closedCurtainPosition = startPos;
        yield return CoverWall();
        Vector3 endPos = startPos + Vector3.left * openDistance;
        float elapsed = 0f;
        while (elapsed < openDuration)
        {
            float t = elapsed / openDuration;
            curtainRoot.position = Vector3.Lerp(startPos, endPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        curtainRoot.position = endPos;

        // Leave the fabric at the left edge; only the wall group returns later.
        if (coveringCurtain != null)
            coveringCurtain.SetParent(curtainRoot.parent, true);
        if (stageStep != null) stageStep.SetActive(true);
        yield return PlayMemories();

        playerMovement.SetMovementLocked(true);
        yield return RestoreWall();

        stagePedestals.SetActive(false);
        yield return SetStageCamera(false);
        CanUseBed = true;

        playerMovement.SetMovementLocked(false);
        playerInput.ActivateInput();
        IsPlayingSequence = false;

        // ★ 自动推进：撤具后给玩家 autoAdvanceSeconds 秒自由活动时间，
        //   如果一直没去调查床，就自动进入结局（按 E 上床仍然立即生效）
        if (autoAdvanceSeconds > 0f) StartCoroutine(AutoAdvanceToEnding());
    }

    private IEnumerator SetStageCamera(bool active)
    {
        if (stageCamera == null) yield break;
        if (cameraBrain == null) cameraBrain = FindFirstObjectByType<CinemachineBrain>();
        if (active)
        {
            if (cameraBrain != null)
            {
                savedCameraBlend = cameraBrain.DefaultBlend;
                cameraBrain.DefaultBlend = new CinemachineBlendDefinition(
                    CinemachineBlendDefinition.Styles.EaseInOut, Mathf.Max(0.1f, cameraBlendDuration));
            }
            stageCameraActive = true;
        }
        stageCamera.enabled = active;
        yield return null;
        if (cameraBrain != null)
        {
            while (cameraBrain.IsBlending) yield return null;
        }
        else yield return new WaitForSeconds(Mathf.Max(0.1f, cameraBlendDuration));

        if (!active)
        {
            if (cameraBrain != null) cameraBrain.DefaultBlend = savedCameraBlend;
            stageCameraActive = false;
        }
    }

    private void OnDisable()
    {
        if (!stageCameraActive) return;
        if (stageCamera != null) stageCamera.enabled = false;
        if (cameraBrain != null) cameraBrain.DefaultBlend = savedCameraBlend;
        stageCameraActive = false;
    }

    private IEnumerator CoverWall()
    {
        if (coveringCurtain == null) yield break;
        SpriteRenderer fabricRenderer = coveringCurtain.GetComponent<SpriteRenderer>();
        Color color = fabricRenderer != null ? fabricRenderer.color : Color.white;
        color.a = 0f;
        if (fabricRenderer != null) fabricRenderer.color = color;
        coveringCurtain.gameObject.SetActive(true);

        float elapsed = 0f;
        float duration = Mathf.Max(0.1f, coverDuration);
        while (elapsed < duration)
        {
            color.a = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            if (fabricRenderer != null) fabricRenderer.color = color;
            elapsed += Time.deltaTime;
            yield return null;
        }
        color.a = 1f;
        if (fabricRenderer != null) fabricRenderer.color = color;
    }

    private IEnumerator RestoreWall()
    {
        playerInput.DeactivateInput();
        playerMovement.SetMovementLocked(true);
        Vector3 startPos = curtainRoot.position;
        Vector3 endPos = closedCurtainPosition;
        float elapsed = 0f;
        while (elapsed < openDuration)
        {
            float t = elapsed / openDuration;
            curtainRoot.position = Vector3.Lerp(startPos, endPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        curtainRoot.position = endPos;
    }

    private void SetMemoryAlpha(float alpha)
    {
        if (memoryDisplay != null)
            memoryDisplay.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
    }

    private IEnumerator FadeMemory(float targetAlpha)
    {
        if (memoryDisplay == null) yield break;
        float startAlpha = memoryDisplay.color.a;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, memoryFadeDuration);
        while (elapsed < duration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            SetMemoryAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
            elapsed += Time.deltaTime;
            yield return null;
        }
        SetMemoryAlpha(targetAlpha);
    }

    private IEnumerator ShowMemory(Sprite image, DialogueLine[] lines)
    {
        yield return new WaitUntil(() =>
            Time.timeScale > 0f &&
            (Keyboard.current == null || !Keyboard.current.eKey.isPressed));

        if (memoryDisplay != null)
        {
            memoryDisplay.sprite = image;
            SetMemoryAlpha(0f);
            memoryDisplay.enabled = image != null;
        }
        yield return FadeMemory(Mathf.Clamp(memoryMaxAlpha, 0f, 0.99f));

        DialogueManager dm = DialogueManager.Instance;
        if (dm != null && lines != null && lines.Length > 0) dm.StartDialogue(lines);
        float held = 0f;
        while ((dm != null && dm.IsOpen) || held < memoryMinHold)
        {
            if (playerMovement != null) playerMovement.SetMovementLocked(true);
            held += Time.deltaTime;
            yield return null;
        }
        if (playerMovement != null) playerMovement.SetMovementLocked(true);
        yield return FadeMemory(0f);
        if (memoryDisplay != null) memoryDisplay.enabled = false;
    }

    private IEnumerator PlayMemories()
    {
        yield return ShowMemory(memoryOneSprite, memoryOneLines);
        yield return ShowMemory(memoryTwoSprite, memoryTwoLines);
        yield return ShowMemory(memoryThreeSprite, memoryThreeLines);

        yield return new WaitUntil(() =>
            Time.timeScale > 0f &&
            (Keyboard.current == null || !Keyboard.current.eKey.isPressed));
        DialogueManager dm = DialogueManager.Instance;
        if (dm != null && afterMemoryLines != null && afterMemoryLines.Length > 0)
        {
            dm.StartDialogue(afterMemoryLines);
            while (dm != null && dm.IsOpen) yield return null;
        }
    }

    /// <summary>撤具后的自动推进：等一会儿如果玩家没去床边，就自动进结局</summary>
    private IEnumerator AutoAdvanceToEnding()
    {
        float waited = 0f;
        while (waited < autoAdvanceSeconds)
        {
            // 玩家已经上床了 / 正在放演出 / 已经结束 -> 直接退出
            if (!CanUseBed || IsPlayingSequence || IsGameEnded) yield break;

            // 玩家正在看对话就先不计时（别打断阅读）
            DialogueManager dm = DialogueManager.Instance;
            bool talking = dm != null && dm.IsOpen;

            if (!talking && Time.timeScale > 0f) waited += Time.deltaTime;
            yield return null;
        }

        if (CanUseBed && !IsPlayingSequence && !IsGameEnded)
        {
            Debug.Log("[第四幕] 玩家一直没去床边，自动进入结局");
            EndGame();
        }
    }


    public void EndGame()
    {
        if (!CanUseBed || IsPlayingSequence || IsGameEnded) return;

        CanUseBed = false;
        IsPlayingSequence = true;
        StartCoroutine(FinishGame());
    }

    private IEnumerator FinishGame()
    {
        IsGameEnded = true;
        playerInput.DeactivateInput();
        playerMovement.SetMovementLocked(true);

        yield return screenFader.FadeTo(1f);

        Time.timeScale = 1f;
        yield return SceneManager.LoadSceneAsync(nextSceneName);
    }

}
