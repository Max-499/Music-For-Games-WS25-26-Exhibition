using UnityEngine;
using UnityEngine.InputSystem;

public class CursorEscapeToggle : MonoBehaviour
{
    void Awake()
    {
        HideAndLock();
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ShowAndRelease();
        }
    }

    void HideAndLock()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void ShowAndRelease()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}