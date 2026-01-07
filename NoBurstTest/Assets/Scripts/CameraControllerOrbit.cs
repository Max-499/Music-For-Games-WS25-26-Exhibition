using UnityEngine;
using UnityEngine.InputSystem;
using Camera = UnityEngine.Camera;

namespace Assets.Scripts
{
    public class CameraControllerOrbit : MonoBehaviour
    {
        [Header("Configurable Properties")]
        public float LookOffset = 1f;
        public float DefaultZoom = 10f;
        public float ZoomMax = 5f;
        public float ZoomMin = 20f;

        [Tooltip("Degrees per second")]
        public float RotationSpeed = 120f;

        [Tooltip("How fast the camera catches up to the target rotation")]
        public float RotationLerpSpeed = 6f;

        [Header("Pitch Limits")]
        public float MinPitch = 15f;
        public float MaxPitch = 80f;

        // Camera
        private Camera _actualCamera;

        // Rotation state (target vs current)
        private float _targetYaw;
        private float _targetPitch;
        private float _currentYaw;
        private float _currentPitch;

        // Input
        private Vector2 _rotationInput;

        // Zoom
        private float _currentZoom;
        private float _zoomLerpSpeed = 6f;
        private Vector3 _cameraPositionTarget;

        void Start()
        {
            _actualCamera = GetComponentInChildren<Camera>();

            _currentZoom = DefaultZoom;

            Vector3 euler = transform.rotation.eulerAngles;
            _targetYaw = _currentYaw = euler.y;
            _targetPitch = _currentPitch = euler.x;

            UpdateCameraTarget();

            _actualCamera.transform.localPosition = _cameraPositionTarget;
            _actualCamera.transform.localRotation = Quaternion.identity;
        }

        // ---------- INPUT ----------

        public void OnRotate(InputAction.CallbackContext context)
        {
            _rotationInput = context.ReadValue<Vector2>();
        }

        public void OnZoom(InputAction.CallbackContext context)
        {
            if (context.phase != InputActionPhase.Performed)
                return;

            _currentZoom = Mathf.Clamp(
                _currentZoom - context.ReadValue<Vector2>().y,
                ZoomMax,
                ZoomMin
            );

            UpdateCameraTarget();
        }

        // ---------- LOGIC ----------

        private void LateUpdate()
        {
            // Update target rotation from input
            _targetYaw += _rotationInput.x * RotationSpeed * Time.deltaTime;
            _targetPitch -= _rotationInput.y * RotationSpeed * Time.deltaTime;
            _targetPitch = Mathf.Clamp(_targetPitch, MinPitch, MaxPitch);

            // Smooth toward target
            _currentYaw = Mathf.Lerp(
                _currentYaw,
                _targetYaw,
                Time.deltaTime * RotationLerpSpeed
            );

            _currentPitch = Mathf.Lerp(
                _currentPitch,
                _targetPitch,
                Time.deltaTime * RotationLerpSpeed
            );

            // Apply rotation to the rig
            transform.rotation = Quaternion.Euler(
                _currentPitch,
                _currentYaw,
                0f
            );

            // Smooth zoom
            _actualCamera.transform.localPosition = Vector3.Lerp(
                _actualCamera.transform.localPosition,
                _cameraPositionTarget,
                Time.deltaTime * _zoomLerpSpeed
            );
        }

        private void UpdateCameraTarget()
        {
            _cameraPositionTarget =
                (Vector3.up * LookOffset)
                + Vector3.back * _currentZoom;
        }
    }
}
