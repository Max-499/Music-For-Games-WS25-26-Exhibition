using UnityEngine;
using UnityEngine.InputSystem; // New Input System
using UnityEngine.SceneManagement;

public class ExitToLauncher : MonoBehaviour
{
    public bool enableKeyboardExit = true;
    public bool enableGamepadExit = true;

    public float autoExitAfter = -1f; // -1 disables

    float timer = 0f;

    void Update()
    {
        // Keyboard ESC -> go to scene 0
        if (enableKeyboardExit)
        {
            if (Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                GoToLauncher();
            }
        }

        // Gamepad Start -> go to scene 0
        if (enableGamepadExit)
        {
            var gamepad = Gamepad.current;
            if (gamepad != null &&
                gamepad.startButton.wasPressedThisFrame)
            {
                GoToLauncher();
            }
        }

        // Optional auto-exit
        if (autoExitAfter > 0f)
        {
            timer += Time.deltaTime;
            if (timer >= autoExitAfter)
            {
                GoToLauncher();
            }
        }
    }

    void GoToLauncher()
    {
        Debug.Log("Returning to launcher (scene 0).");
        SceneManager.LoadScene(0);
    }
}