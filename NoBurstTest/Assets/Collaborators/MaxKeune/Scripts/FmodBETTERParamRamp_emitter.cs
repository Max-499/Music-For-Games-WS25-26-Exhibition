using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections;

public class FMODEmitterMultiParameterRamp : MonoBehaviour
{
    [Header("Studio Event Emitter")]
    [SerializeField] private StudioEventEmitter emitter;

    [Header("Parameters")]
    [SerializeField] private string[] parameterNames;

    [Header("Ramp Duration (Seconds)")]
    [Range(20f, 240f)]
    [SerializeField] private float rampDuration = 60f;

    private void Start()
    {
        if (emitter == null)
        {
            Debug.LogError("Emitter not assigned!");
            return;
        }

        // Initialize all parameters to 0
        foreach (string parameter in parameterNames)
        {
            emitter.EventInstance.setParameterByName(parameter, 0f);
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
                emitter.EventInstance.setParameterByName(parameter, value);
            }

            yield return null;
        }

        // Ensure all parameters end at 1
        foreach (string parameter in parameterNames)
        {
            emitter.EventInstance.setParameterByName(parameter, 1f);
        }
    }
}
