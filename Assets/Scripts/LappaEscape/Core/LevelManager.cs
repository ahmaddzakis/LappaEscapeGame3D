using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LappaEscape.Core
{
    /// <summary>
    /// Pengelola status level, checkpoint, kondisi menang (Win) dan kalah (Game Over).
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [Header("Status Level")]
        public bool isGameOver = false;
        public bool isLevelCompleted = false;

        [Header("Titik Spawn & Checkpoint")]
        public Transform activeCheckpoint;
        public Transform initialSpawnPoint;

        // Events
        public static event Action OnPlayerDied;
        public static event Action OnPlayerRespawned;
        public static event Action OnStageClear;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void SetCheckpoint(Transform newCheckpoint)
        {
            activeCheckpoint = newCheckpoint;
            Debug.Log($"<color=cyan>[LevelManager] Checkpoint aktif diperbarui ke: {newCheckpoint.position}</color>");
        }

        public Vector3 GetRespawnPosition()
        {
            if (activeCheckpoint != null)
                return activeCheckpoint.position;

            if (initialSpawnPoint != null)
                return initialSpawnPoint.position;

            var startNode = GridManager.Instance != null ? GridManager.Instance.GetStartNode() : null;
            if (startNode != null)
                return startNode.WorldPosition;

            return Vector3.zero;
        }

        public void TriggerPlayerDeath()
        {
            if (isGameOver || isLevelCompleted) return;

            isGameOver = true;
            Debug.Log("<color=red>[LevelManager] Pemain gugur! Menyiapkan respawn ke Checkpoint...</color>");
            OnPlayerDied?.Invoke();

            if (TurnManager.Instance != null)
                TurnManager.Instance.PauseGame(true);
        }

        public void RespawnPlayer(Player.LappaPlayerController player)
        {
            isGameOver = false;
            Vector3 respawnPos = GetRespawnPosition();
            if (player != null)
            {
                player.RespawnAt(respawnPos);
            }

            if (TurnManager.Instance != null)
                TurnManager.Instance.PauseGame(false);

            OnPlayerRespawned?.Invoke();
            Debug.Log($"<color=green>[LevelManager] Pemain berhasil respawn di {respawnPos}</color>");
        }

        public void TriggerStageClear()
        {
            if (isLevelCompleted || isGameOver) return;

            isLevelCompleted = true;
            Debug.Log("<color=yellow>[LevelManager] SELAMAT! STAGE 1 CLEAR!</color>");
            OnStageClear?.Invoke();

            if (TurnManager.Instance != null)
                TurnManager.Instance.PauseGame(true);
        }

        public void RestartCurrentScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
