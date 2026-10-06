using UnityEngine;
using UnityEngine.InputSystem;

public class BedEnding : MonoBehaviour
{
    [SerializeField] private Act04Flow act04Flow;
    [SerializeField] private DialogueLine[] lockedLines;
    [SerializeField] private DialogueLine[] bedLines;

    private bool playerInside;
    private bool readyToSleep;//这次靠近床是否已经调查过

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.transform.root.CompareTag("Player"))
        {
            playerInside = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.transform.root.CompareTag("Player"))
        {
            playerInside = false;
            readyToSleep = false;
        }
    }

    private void Update()
    {
        if (act04Flow == null || act04Flow.IsPlayingSequence) return;
        if (!playerInside) return;// 只有玩家在触发器内才允许交互

        DialogueManager dm = DialogueManager.Instance;// 获取对话管理器实例
        if (dm == null || dm.IsOpen || Time.timeScale == 0f) return;
        if (Time.frameCount == dm.LastStateChangeFrame) return;// 防止在同一帧内重复触发对话

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Interact(dm);
        }

    }

    private void Interact(DialogueManager dm)
    {
        //床还没开放
        if (!act04Flow.CanUseBed)
        {
            dm.StartDialogue(lockedLines);
            return;
        }

        //床已开放，第一次按 E：先调查。
        if (!readyToSleep)
        {
            readyToSleep = true;
            dm.StartDialogue(bedLines);
            return;
        }

        //已调查，再按一次新的 E：确认上床。
        readyToSleep = false;
        act04Flow.EndGame();
    }
}
