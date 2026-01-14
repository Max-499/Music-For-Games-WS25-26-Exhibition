using UnityEngine;
using UnityEngine.InputSystem;   // required for Keyboard.current

public class AKeySoundTrigger : MonoBehaviour
{
    public string fmodEvent;

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.aKey.wasPressedThisFrame)
        {
            Debug.Log("A pressed");

            if (!string.IsNullOrEmpty(fmodEvent))
            {
                FMODUtility.PlayOneShot(fmodEvent);
            }
            else
            {
                Debug.LogWarning("No FMOD event assigned.");
            }
        }
    }
}