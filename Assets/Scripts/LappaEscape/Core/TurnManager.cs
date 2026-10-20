using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LappaEscape.Core
{
    /// <summary>
    /// Fase giliran dalam siklus permainan Lappa Escape.
    /// </summary>
    public enum TurnPhase
    {
        PlayerInput,      // Menunggu input tombol pemain
        PlayerMoving,     // Pemain sedang bergerak antar-petak (input dikunci)
        EnvironmentTurn,  // Lingkungan bereaksi (ubin retak runtuh, jembatan bergeser)
        EnemyTurn         // Seluruh musuh (Demon) mengevaluasi dan bergerak/menyerang
    }

    /// <summary>
    /// Manajer pusat siklus giliran (Turn-Based Loop) untuk Lappa Escape.
    /// Memastikan permainan berjalan deterministik tanpa unsur reaksi cepat.
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        public static TurnManager Instance { get; private set; }

        [Header("Status Giliran")]
        [SerializeField] private TurnPhase currentPhase = TurnPhase.PlayerInput;
        public TurnPhase CurrentPhase => currentPhase;

        [Header("Pengaturan Durasi Transisi")]
        [Tooltip("Jeda waktu (detik) sebelum giliran musuh dieksekusi")]
        public float delayBeforeEnemyTurn = 0.08f;

        [Tooltip("Jeda waktu (detik) setelah giliran musuh sebelum kontrol pemain dibuka")]
        public float delayAfterEnemyTurn = 0.08f;

        // Events
        public static event Action<TurnPhase> OnPhaseChanged;
        public static event Action<Vector3> OnPlayerStepFinished;
        public static event Action OnEnvironmentTurnStart;
        public static event Action OnEnemyTurnStart;
        public static event Action OnTurnCompleted;

        private bool isGameActive = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            SetPhase(TurnPhase.PlayerInput);
        }

        public bool CanPlayerMove()
        {
            return isGameActive && currentPhase == TurnPhase.PlayerInput;
        }

        public void NotifyPlayerMoving()
        {
            SetPhase(TurnPhase.PlayerMoving);
        }

        /// <summary>
        /// Dipanggil oleh PlayerController saat langkah pemain selesai mendarat di petak baru.
        /// </summary>
        public void NotifyPlayerStepFinished(Vector3 newPosition)
        {
            if (!isGameActive) return;

            OnPlayerStepFinished?.Invoke(newPosition);
            StartCoroutine(ExecuteTurnCycle(newPosition));
        }

        private IEnumerator ExecuteTurnCycle(Vector3 playerPos)
        {
            // 1. Fase Environment Turn
            SetPhase(TurnPhase.EnvironmentTurn);
            OnEnvironmentTurnStart?.Invoke();
            if (delayBeforeEnemyTurn > 0f)
                yield return new WaitForSeconds(delayBeforeEnemyTurn);

            // 2. Fase Enemy Turn
            SetPhase(TurnPhase.EnemyTurn);
            OnEnemyTurnStart?.Invoke();
            if (delayAfterEnemyTurn > 0f)
                yield return new WaitForSeconds(delayAfterEnemyTurn);

            // 3. Selesai 1 siklus giliran -> Buka input pemain lagi jika game masih aktif
            if (isGameActive)
            {
                SetPhase(TurnPhase.PlayerInput);
                OnTurnCompleted?.Invoke();
            }
        }

        public void SetPhase(TurnPhase newPhase)
        {
            currentPhase = newPhase;
            OnPhaseChanged?.Invoke(currentPhase);
        }

        public void PauseGame(bool pause)
        {
            isGameActive = !pause;
        }
    }
}
