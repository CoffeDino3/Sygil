using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;

    public float SpeedMultiplier = 1f;
    public Vector2 FacingDir { get; private set; } = Vector2.up;
    Rigidbody2D rb;
    Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
        if (moveInput.sqrMagnitude > 0.01f)
            FacingDir = moveInput.normalized;
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed * SpeedMultiplier;
    }
}