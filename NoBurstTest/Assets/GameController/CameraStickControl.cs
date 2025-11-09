using UnityEngine;
using UnityEngine.InputSystem;

public class CameraStickControl : MonoBehaviour
{
    public InputActionAsset inputActions;
    public string actionMapName = "Player";
    public string moveActionName = "Move";

    public Transform targetCamera;
    public float moveSpeed = 0.5f;
    public float clampX = 2f;
    public float clampY = 2f;

    private InputAction moveAction;
    private Vector3 initialPosition;

    void OnEnable()
    {
        if (inputActions == null)
        {
            Debug.LogError("InputActionAsset not assigned.");
            return;
        }

        var map = inputActions.FindActionMap(actionMapName);
        if (map == null)
        {
            Debug.LogError($"Action map '{actionMapName}' not found.");
            return;
        }

        moveAction = map.FindAction(moveActionName);
        if (moveAction == null)
        {
            Debug.LogError($"Move action '{moveActionName}' not found in map '{actionMapName}'.");
            return;
        }

        moveAction.Enable();

        if (targetCamera != null)
            initialPosition = targetCamera.position;
        else
            Debug.LogError("Target Camera not assigned.");
    }

    void OnDisable()
    {
        moveAction?.Disable();
    }

    void Update()
    {
        if (moveAction == null || targetCamera == null)
            return;

        Vector2 input = moveAction.ReadValue<Vector2>();

        Vector3 offset = new Vector3(input.x, input.y, 0f) * moveSpeed;
        Vector3 newPosition = initialPosition + offset;

        newPosition.x = Mathf.Clamp(newPosition.x, initialPosition.x - clampX, initialPosition.x + clampX);
        newPosition.y = Mathf.Clamp(newPosition.y, initialPosition.y - clampY, initialPosition.y + clampY);

        targetCamera.position = newPosition;
    }
}
