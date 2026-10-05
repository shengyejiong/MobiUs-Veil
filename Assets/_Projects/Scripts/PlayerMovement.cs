using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 moveInput;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    //物理系统更新时来计算速度，防止帧率不稳定导致的移动速度不稳定
    private void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;// 表示每秒移动速度，不需要再乘DeltaTime
    }


}
