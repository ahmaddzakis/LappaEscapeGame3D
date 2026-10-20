using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LappaEscape.Environment
{
    public enum LeverType
    {
        ColorBridgeToggle,   // Tuas warna (merah, cokelat, ungu)
        SlidingBridgeToggle  // Tuas putih geser
    }

    /// <summary>
    /// Komponen tuas interaktif yang memicu perubahan status jembatan saat diinjak pemain.
    /// </summary>
    public class LeverInteractable : MonoBehaviour
    {
        [Header("Tipe Tuas")]
        public LeverType leverType = LeverType.ColorBridgeToggle;

        [Header("Target Jembatan Warna")]
        public BridgeColor targetColor = BridgeColor.Red;
        public List<BridgeController> linkedBridges = new List<BridgeController>();

        [Header("Target Jembatan Geser")]
        public SlidingBridge linkedSlidingBridge;

        [Header("Visual Tuas")]
        public Transform leverHandle;
        public float toggleAngle = 40.0f;

        [Header("Status")]
        public bool isToggled = false;

        private void Start()
        {
            // Jika linkedBridges kosong, cari otomatis di scene yang warnanya cocok
            if (leverType == LeverType.ColorBridgeToggle && linkedBridges.Count == 0)
            {
                var allBridges = UnityEngine.Object.FindObjectsByType<BridgeController>(FindObjectsInactive.Include);
                foreach (var b in allBridges)
                {
                    if (b.bridgeColor == targetColor)
                    {
                        linkedBridges.Add(b);
                    }
                }
            }
        }

        public void Interact()
        {
            isToggled = !isToggled;
            StartCoroutine(AnimateLeverHandle());

            if (leverType == LeverType.ColorBridgeToggle)
            {
                foreach (var bridge in linkedBridges)
                {
                    if (bridge != null) bridge.Toggle();
                }
                Debug.Log($"<color=yellow>[Lever] Tuas warna {targetColor} diaktifkan! Jembatan ter-update.</color>");
            }
            else if (leverType == LeverType.SlidingBridgeToggle)
            {
                if (linkedSlidingBridge != null)
                {
                    linkedSlidingBridge.TogglePosition();
                    Debug.Log("<color=white>[Lever] Tuas jembatan geser diaktifkan!</color>");
                }
            }
        }

        private IEnumerator AnimateLeverHandle()
        {
            if (leverHandle == null) yield break;

            float targetZ = isToggled ? toggleAngle : -toggleAngle;
            Quaternion targetRot = Quaternion.Euler(0f, 0f, targetZ);

            float elapsed = 0f;
            while (elapsed < 0.15f)
            {
                leverHandle.localRotation = Quaternion.Slerp(leverHandle.localRotation, targetRot, 20f * Time.deltaTime);
                elapsed += Time.deltaTime;
                yield return null;
            }
            leverHandle.localRotation = targetRot;
        }
    }
}
