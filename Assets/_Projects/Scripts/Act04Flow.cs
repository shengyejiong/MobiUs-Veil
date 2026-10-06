using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Act04Flow : MonoBehaviour
{
    [SerializeField] private Transform curtainRoot;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private float openDistance = 16.5f;
    [SerializeField] private float openDuration = 2f;

    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private SpriteRenderer memoryDisplay;

    [SerializeField] private DialogueLine[] memoryOneLines;
    [SerializeField] private DialogueLine[] memoryTwoLines;
    [SerializeField] private DialogueLine[] memoryThreeLines;
    [SerializeField] private DialogueLine[] afterMemoryLines;
    [SerializeField] private GameObject stagePedestals;
    [SerializeField] private ScreenFader screenFader;
    [SerializeField] private GameObject endPanel;

    public bool IsGameEnded { get; private set; }
    private Vector3 closedCurtainPosition;

    public bool CanUseBed { get; private set; }//整个第四幕是否已经演完

    public bool IsPlayingSequence { get; private set; }

    private bool openingStarted;//开幕

    private bool AllItemsPlaced()
    {
        return GameProgress.GetState(StoryItemId.MessageCard) == StoryItemState.Placed
            && GameProgress.GetState(StoryItemId.Clock) == StoryItemState.Placed
            && GameProgress.GetState(StoryItemId.MemorialPlaque) == StoryItemState.Placed;
    }

    private void Update()
    {
        if (openingStarted) return;//防重复播放

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
        Vector3 startPos = curtainRoot.position;
        closedCurtainPosition = startPos;
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
        yield return PlayMemories();

        playerMovement.SetMovementLocked(true);
        yield return CloseCurtain();

        stagePedestals.SetActive(false);
        CanUseBed = true;

        playerMovement.SetMovementLocked(false);
        playerInput.ActivateInput();
        IsPlayingSequence = false;
    }

    private IEnumerator CloseCurtain()
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
        playerMovement.SetMovementLocked(false);
        playerInput.ActivateInput();
    }

    private IEnumerator ShowMemory(Color tint, DialogueLine[] lines)
    {
        yield return new WaitUntil(() =>
            Time.timeScale > 0f &&
            (Keyboard.current == null || !Keyboard.current.eKey.isPressed));

        memoryDisplay.color = tint;

        DialogueManager dm = DialogueManager.Instance;
        dm.StartDialogue(lines);

        yield return new WaitUntil(() => !dm.IsOpen);
    }

    private IEnumerator PlayMemories()
    {
        Color stageColor = memoryDisplay.color;

        yield return ShowMemory(new Color(1f, 0.8f, 0.55f), memoryOneLines);
        yield return ShowMemory(new Color(0.65f, 0.7f, 0.9f), memoryTwoLines);
        yield return ShowMemory(new Color(0.55f, 0.6f, 0.65f), memoryThreeLines);
        yield return ShowMemory(stageColor, afterMemoryLines);
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
        playerInput.DeactivateInput();
        playerMovement.SetMovementLocked(true);

        yield return screenFader.FadeTo(1f);

        endPanel.SetActive(true);
        endPanel.transform.SetAsLastSibling();

        IsGameEnded = true;
        IsPlayingSequence = false;
        Time.timeScale = 0f;
    }

}
