using UnityEngine;
using LappaEscape.Core;

namespace LappaEscape.Environment
{
    /// <summary>
    /// Titik simpan sementara (Checkpoint).
    /// Mengubah spawnpoint aktif saat diinjak agar pemain tidak mengulang dari awal jika gugur.
    /// </summary>
    [RequireComponent(typeof(GridNode))]
    public class CheckpointNode : MonoBehaviour
    {
        [Header("Status")]
        public bool isActivated = false;

        [Header("Visual Feedback")]
        public GameObject activeIndicator;
        public Light checkpointLight;

        public void ActivateCheckpoint()
        {
            if (isActivated) return;

            isActivated = true;
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.SetCheckpoint(transform);
            }

            if (activeIndicator != null)
                activeIndicator.SetActive(true);

            if (checkpointLight != null)
                checkpointLight.color = Color.cyan;

            Debug.Log($"<color=white>[Checkpoint] Checkpoint aktif di {name}!</color>");
        }
    }
}
