using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Animator animator; 
    bool isWalking = false; // 用于跟踪玩家是否在移动

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool movementLocked;

    public bool IsMovementLocked => movementLocked;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if(!movementLocked && moveInput.sqrMagnitude > 0f)
        {
            isWalking = true;
            animator.SetBool("IsWalking", isWalking);
        }
        else
        {
            isWalking = false;
            animator.SetBool("IsWalking", isWalking);
        }
    }

    // PlayerInput 使用 Send Messages 时，参数必须使用 InputValue
    public void OnMove(InputValue value)
    {
        if (movementLocked)
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = value.Get<Vector2>();
    }

    public void SetMovementLocked(bool locked)
    {
        movementLocked = locked;

        if (locked)
        {
            moveInput = Vector2.zero;

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
    }

    private void FixedUpdate()
    {
        if (movementLocked)
        {
            moveInput = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // 防止同时按两个方向时斜向速度过快
        if (moveInput.sqrMagnitude > 1f)
        {
            moveInput.Normalize();
        }

        rb.linearVelocity = moveInput * moveSpeed;
    }

    private void OnDisable()
    {
        moveInput = Vector2.zero;
        movementLocked = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
}