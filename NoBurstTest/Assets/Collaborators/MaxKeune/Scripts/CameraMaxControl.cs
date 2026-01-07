using UnityEngine;
using UnityEngine.InputSystem;

public class CameraControllerNewInputSystem : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 2f;

    [Header("Mouse Look Settings")]
    public float mouseSensitivity = 100f;
    public float pitchMin = -90f;
    public float pitchMax = 90f;

    private float yaw = 0f;
    private float pitch = 0f;

    private Vector2 moveInput = Vector2.zero;
    private Vector2 lookInput = Vector2.zero;

    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction lookAction;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();

        // Grab the actions from the default map
        moveAction = playerInput.actions["Move"];
        lookAction = playerInput.actions["Look"];

        // Subscribe to callbacks
        moveAction.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        moveAction.canceled += ctx => moveInput = Vector2.zero;

        lookAction.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        lookAction.canceled += ctx => lookInput = Vector2.zero;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleMouseLook();
        HandleMovement();
    }

    private void HandleMouseLook()
    {
        yaw += lookInput.x * mouseSensitivity * Time.deltaTime;
        pitch -= lookInput.y * mouseSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        transform.eulerAngles = new Vector3(pitch, yaw, 0f);
    }

    private void HandleMovement()
    {
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        move.Normalize();

        float speed = moveSpeed;
        if (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed)
            speed *= sprintMultiplier;

        transform.position += move * speed * Time.deltaTime;
    }
}




