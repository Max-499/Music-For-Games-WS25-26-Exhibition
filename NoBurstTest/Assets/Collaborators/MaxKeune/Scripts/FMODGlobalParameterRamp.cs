using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections;

public class FMODGlobalParameterRamp : MonoBehaviour
{
    [Header("Global Parameter")]
    [Tooltip("Name of the FMOD global parameter to control")]
    [SerializeField] private string parameterName = "humanity";

    [Header("Ramp Duration (Seconds)")]
    [Tooltip("Duration of the linear ramp from 0 to 1")]
    [Range(20f, 240f)]
    [SerializeField] private float rampDuration = 60f;

    private void Start()
    {
        // Initialize parameter to 0
        RuntimeManager.StudioSystem.setParameterByName(parameterName, 0f);
        StartCoroutine(RampGlobalParameter());
    }

    private IEnumerator RampGlobalParameter()
    {
        float elapsed = 0f;

        while (elapsed < rampDuration)
        {
            elapsed += Time.deltaTime;
            float value = Mathf.Clamp01(elapsed / rampDuration);

            RuntimeManager.StudioSystem.setParameterByName(parameterName, value);

            yield return null;
        }

        // Ensure final value is exactly 1
        RuntimeManager.StudioSystem.setParameterByName(parameterName, 1f);
    }
}

