using UnityEngine;

public class CursorDiagnostics : MonoBehaviour
{
    float t;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
//Debug.Log("BOOTSTRAP: cursor hide applied");
//Debug.Log($"BOOTSTRAP: visible={Cursor.visible} lock={Cursor.lockState}");
       // Debug.Log("BOOTSTRAP: cursor hide applied");
       // Debug.Log($"BOOTSTRAP: visible={Cursor.visible} lock={Cursor.lockState}");
    }

    void Update()
    {
        t += Time.unscaledDeltaTime;
        if (t > 1f)
        {
            t = 0f;
            Dump("Tick");
        }
    }

    void ApplyHiddenLocked()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Dump(string tag)
    {
        //Debug.Log($"{tag}: visible={Cursor.visible}, lockState={Cursor.lockState}, focused={Application.isFocused}");
    }
}