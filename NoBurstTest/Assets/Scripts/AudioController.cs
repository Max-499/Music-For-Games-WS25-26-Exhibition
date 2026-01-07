using Assets.Scripts;
using UnityEngine;

public class AudioController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public FMODUnity.EventReference fmodEvent;

    private Camera actualCamera;
    private CameraControllerOrbit cameraController;
    private FMOD.Studio.EventInstance instance;

    private float zoomMax = 0f;
    private float zoomMin = 0f;

    [SerializeField] [Range(0f, 1f)]
    private float droneState = 0f;

    void Start()
    {
        actualCamera = GetComponentInChildren<Camera>();
        cameraController = GetComponent<CameraControllerOrbit>();

        zoomMax = cameraController.ZoomMax;
        zoomMin = cameraController.ZoomMin;

        instance = FMODUnity.RuntimeManager.CreateInstance(fmodEvent);
        instance.start();
    }

    // Update is called once per frame
    void Update()
    {
        droneState = Mathf.Clamp01(Mathf.Abs(actualCamera.transform.localPosition.z - zoomMax) / (zoomMin - zoomMax));
        instance.setParameterByName("DroneState", droneState);
    }
}
