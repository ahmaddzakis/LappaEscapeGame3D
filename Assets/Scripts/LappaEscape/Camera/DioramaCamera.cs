using UnityEngine;
using UnityEngine.InputSystem;
using LappaEscape.Player;

namespace LappaEscape.CameraControl
{
    /// <summary>
    /// Kamera isometrik diorama bergaya Lara Croft GO / Hitman GO.
    /// Sudut pandang tetap (Pitch 46°, Yaw 45°) dengan smooth follow dan mouse parallax.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class DioramaCamera : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Transform karakter pemain yang diikuti")]
        public Transform target;

        [Header("Sudut Isometrik Diorama")]
        [Range(20f, 75f)]
        public float pitchAngle = 46f;

        [Range(-180f, 180f)]
        public float yawAngle = 45f;

        [Tooltip("Jarak kamera ke target")]
        public float distance = 14.0f;

        [Tooltip("Tinggi offset titik fokus di atas kaki pemain")]
        public float focusHeightOffset = 1.2f;

        [Header("Kehalusan Gerak (Smoothing)")]
        public float followSmoothTime = 0.16f;

        [Header("Efek Mouse Parallax")]
        public bool enableMouseParallax = true;
        public float maxParallaxAngle = 2.5f;
        public float parallaxSmoothTime = 0.2f;

        private Vector3 currentVelocity;
        private Vector3 smoothedTargetPos;
        private Vector2 currentParallax;
        private Vector2 parallaxVelocity;

        private void Start()
        {
            if (target == null)
            {
                var player = UnityEngine.Object.FindAnyObjectByType<LappaPlayerController>();
                if (player != null) target = player.transform;
            }

            if (target != null)
            {
                smoothedTargetPos = target.position;
                ApplyTransformImmediate();
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                var player = UnityEngine.Object.FindAnyObjectByType<LappaPlayerController>();
                if (player != null) target = player.transform;
                if (target == null) return;
            }

            if (Application.isPlaying)
            {
                smoothedTargetPos = Vector3.SmoothDamp(
                    smoothedTargetPos,
                    target.position,
                    ref currentVelocity,
                    followSmoothTime);

                UpdateCameraTransform();
            }
            else
            {
                smoothedTargetPos = target.position;
                ApplyTransformImmediate();
            }
        }

        private void UpdateCameraTransform()
        {
            Vector2 targetParallax = Vector2.zero;
            if (enableMouseParallax && Mouse.current != null)
            {
                Vector2 mousePos = Mouse.current.position.ReadValue();
                float nx = (mousePos.x / Screen.width - 0.5f) * 2f;
                float ny = (mousePos.y / Screen.height - 0.5f) * 2f;
                targetParallax = new Vector2(
                    Mathf.Clamp(nx, -1f, 1f) * maxParallaxAngle,
                    Mathf.Clamp(ny, -1f, 1f) * maxParallaxAngle);
            }

            currentParallax = Vector2.SmoothDamp(
                currentParallax,
                targetParallax,
                ref parallaxVelocity,
                parallaxSmoothTime);

            Quaternion baseRotation = Quaternion.Euler(
                pitchAngle - currentParallax.y,
                yawAngle + currentParallax.x,
                0f);

            Vector3 focusPoint = smoothedTargetPos + Vector3.up * focusHeightOffset;
            Vector3 desiredPosition = focusPoint - (baseRotation * Vector3.forward * distance);

            transform.SetPositionAndRotation(desiredPosition, baseRotation);
        }

        [ContextMenu("Apply Transform Immediate")]
        public void ApplyTransformImmediate()
        {
            if (target == null) return;
            Quaternion rot = Quaternion.Euler(pitchAngle, yawAngle, 0f);
            Vector3 focusPoint = target.position + Vector3.up * focusHeightOffset;
            transform.SetPositionAndRotation(focusPoint - (rot * Vector3.forward * distance), rot);
        }
    }
}
