using UnityEngine;
using UnityEngine.InputSystem;

public class KioskPointerControl : MonoBehaviour
{
    bool unlocked;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        ApplyState();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            unlocked = true;
            ApplyState();
        }

        // If something flips it back on (focus/UI/scene load), force it off again.
        if (!unlocked && Cursor.visible)
            ApplyState();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && !unlocked)
            ApplyState();
    }

    void OnApplicationPause(bool paused)
    {
        if (!paused && !unlocked)
            ApplyState();
    }

    void ApplyState()
    {
        Cursor.visible = unlocked;
        Cursor.lockState = unlocked ? CursorLockMode.None : CursorLockMode.Locked;
    }
}