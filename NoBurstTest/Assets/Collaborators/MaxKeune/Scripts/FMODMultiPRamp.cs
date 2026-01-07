using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections;

public class FMODMultiParameterRamp : MonoBehaviour
{
    [Header("FMOD Event")]
    [SerializeField] private EventReference placeholderEvent;

    [Header("Parameters")]
    [Tooltip("FMOD parameter names (must match exactly)")]
    [SerializeField] private string[] parameterNames;

    [Header("Ramp Duration (Seconds)")]
    [Tooltip("20 seconds to 4 minutes")]
    [Range(20f, 240f)]
    [SerializeField] private float rampDuration = 60f;

    private EventInstance eventInstance;

    private void Start()
    {
        eventInstance = RuntimeManager.CreateInstance(placeholderEvent);

        // Initialize all parameters to 0
        foreach (string parameter in parameterNames)
        {
            eventInstance.setParameterByName(parameter, 0f);
        }

        StartCoroutine(RampParameters());
    }

    private IEnumerator RampParameters()
    {
        float elapsed = 0f;

        while (elapsed < rampDuration)
        {
            elapsed += Time.deltaTime;
            float value = Mathf.Clamp01(elapsed / rampDuration);

            foreach (string parameter in parameterNames)
            {
                eventInstance.setParameterByName(parameter, value);
            }

            yield return null;
        }

        // Ensure all parameters end at exactly 1.0
        foreach (string parameter in parameterNames)
        {
            eventInstance.setParameterByName(parameter, 1f);
        }
    }

    private void OnDestroy()
    {
        eventInstance.release();
    }
}

