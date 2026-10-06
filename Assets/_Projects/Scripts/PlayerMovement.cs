using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer visualRenderer;
    bool isWalking = false; // 用于跟踪玩家是否在移动
    bool isSide = false; // 用于跟踪玩家是否在侧面移动
    bool isBack = false;

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
        if(Time.timeScale == 0f)
        {
            return; // 暂停时不更新动画状态
        }

        if (!movementLocked && moveInput.sqrMagnitude > 0f)
        {
            isWalking = true;
            animator.SetBool("IsWalking", isWalking);
        }
        else
        {
            isWalking = false;
            animator.SetBool("IsWalking", isWalking);
        }
        if (isWalking)
        {
            
            if (isWalking && Mathf.Abs(moveInput.x) > 0.01f)
            {
                isSide = true;
                isBack = false;
                animator.SetBool("IsSide", isSide);
                if (moveInput.x > 0f)
                {
                    visualRenderer.flipX = true; // 向右移动时翻转
                }
                else
                {
                    visualRenderer.flipX = false; // 向左移动时不翻转
                }
            }
            else
            {
                isSide = false;
                isBack = moveInput.y > 0f; // 向上移动时为背面
                animator.SetBool("IsSide", isSide);
                animator.SetBool("IsBack", isBack);
                visualRenderer.flipX = false; // 停止移动时不翻转
            }
        }

        animator.SetBool("IsSide", isSide);
        animator.SetBool("IsBack", isBack);
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