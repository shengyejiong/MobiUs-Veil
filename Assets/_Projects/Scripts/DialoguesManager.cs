using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    public int LastStateChangeFrame { get; private set; } = -1;

    [Header("对话 UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text contentText;
    [SerializeField] private TMP_Text clickHint;

    [Header("玩家")]
    [SerializeField] private PlayerMovement playerMovement;

    private DialogueLine[] currentLines;
    private int currentIndex;

    public bool IsOpen { get; private set; }
    public string CurrentSpeaker => IsOpen && currentLines != null
        && currentIndex >= 0 && currentIndex < currentLines.Length
        && currentLines[currentIndex] != null
        ? currentLines[currentIndex].speaker : string.Empty;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        IsOpen = false;

        if (playerMovement == null)
        {
            playerMovement = FindFirstObjectByType<PlayerMovement>();
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }

    private void Update()
    {
        //打开对话框后，防止在同一帧中立即后推对话
        if (!IsOpen || Time.frameCount == LastStateChangeFrame || Time.timeScale == 0f)
        {
            return;
        }

        bool nextPressed =
            (Mouse.current != null &&
             Mouse.current.leftButton.wasPressedThisFrame) ||
            (Keyboard.current != null &&
             (Keyboard.current.spaceKey.wasPressedThisFrame ||
                Keyboard.current.eKey.wasPressedThisFrame
             ));

        if (nextPressed)
        {
            ShowNextLine();
        }
    }

    public void StartDialogue(DialogueLine[] lines)
    {
        if (lines == null || lines.Length == 0)
        {
            Debug.LogWarning("InteractionTrigger 没有设置对话内容。");
            return;
        }

        currentLines = lines;
        currentIndex = 0;
        IsOpen = true;
        LastStateChangeFrame = Time.frameCount;
        SetPlayerMovementLocked(true);

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (currentLines == null ||
            currentIndex >= currentLines.Length)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = currentLines[currentIndex];

        if (speakerText != null)
        {
            speakerText.text = line.speaker;
        }

        if (contentText != null)
        {
            contentText.text = line.content;
        }

        if (clickHint != null)
        {
            clickHint.text = "按E/空格或点击继续";
        }
    }

    private void ShowNextLine()
    {
        currentIndex++;

        if (currentIndex >= currentLines.Length)
        {
            EndDialogue();
            return;
        }

        ShowCurrentLine();
    }

    public void EndDialogue()
    {
        IsOpen = false;
        LastStateChangeFrame = Time.frameCount;
        currentLines = null;
        currentIndex = 0;

        SetPlayerMovementLocked(false);

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }

    private void SetPlayerMovementLocked(bool locked)
    {
        if (playerMovement == null)
        {
            playerMovement = FindFirstObjectByType<PlayerMovement>();
        }

        if (playerMovement != null)
        {
            playerMovement.SetMovementLocked(locked);
        }
        else
        {
            Debug.LogWarning("DialogueManager 没有找到 PlayerMovement。");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            if(playerMovement != null)
            {
                SetPlayerMovementLocked(false);
            }
            Instance = null;
        }
    }
}
