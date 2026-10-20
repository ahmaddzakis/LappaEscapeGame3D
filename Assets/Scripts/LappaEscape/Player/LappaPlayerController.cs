using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using LappaEscape.Core;

namespace LappaEscape.Player
{
    /// <summary>
    /// Kontroler karakter Lappa dengan Unity New Input System.
    /// Mendukung input diskrit (1 ketuk = 1 langkah petak), pencarian tetangga cerdas,
    /// rotasi halus, dan integrasi siklus TurnManager.
    /// </summary>
    public class LappaPlayerController : MonoBehaviour
    {
        [Header("Pengaturan Gerak Grid")]
        [Tooltip("Jarak 1 petak standar (2 meter)")]
        public float tileSize = 2.0f;

        [Tooltip("Kecepatan meluncur antar-petak")]
        public float moveSpeed = 6.0f;

        [Tooltip("Kecepatan berputar menghadap arah langkah")]
        public float rotateSpeed = 18.0f;

        [Header("Komponen Karakter")]
        public Animator animator;
        public CharacterController characterController;

        [Header("Node Saat Ini")]
        public GridNode currentNode;

        [Header("Status Gerak")]
        public bool isMoving = false;

        // New Input System Action
        private InputAction moveAction;
        private bool wasInputHeld = false;

        private static readonly int IsWalkingHash = Animator.StringToHash("isWalking");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int DieHash = Animator.StringToHash("Die");

        private void Awake()
        {
            SetupNewInputActions();
        }

        private void OnEnable()
        {
            SetupNewInputActions();
            if (moveAction != null)
            {
                moveAction.Enable();
            }
        }

        private void OnDisable()
        {
            if (moveAction != null)
            {
                moveAction.Disable();
            }
        }

        private void SetupNewInputActions()
        {
            if (moveAction != null) return;

            // Inisialisasi New Input System Action secara dinamis
            moveAction = new InputAction("LappaMove", InputActionType.Value);

            // Composite Dpad / 2D Vector (WASD, Arrow Keys, Gamepad)
            moveAction.AddCompositeBinding("2DVector(mode=1)") // mode 1 = digital normalized
                .With("Up", "<Keyboard>/w")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/s")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/a")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/d")
                .With("Right", "<Keyboard>/rightArrow")
                .With("Up", "<Gamepad>/dpad/up")
                .With("Down", "<Gamepad>/dpad/down")
                .With("Left", "<Gamepad>/dpad/left")
                .With("Right", "<Gamepad>/dpad/right");
        }

        private void Start()
        {
            if (characterController == null)
                characterController = GetComponent<CharacterController>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (!CompareTag("Player"))
                tag = "Player";

            SnapToNearestNode();
        }

        private void Update()
        {
            if (isMoving) return;

            // Periksa apakah giliran pemain aktif
            if (TurnManager.Instance != null && !TurnManager.Instance.CanPlayerMove())
                return;

            Vector2 inputVector = ReadInput();

            if (inputVector != Vector2.zero)
            {
                if (!wasInputHeld)
                {
                    wasInputHeld = true;
                    ProcessMoveInput(inputVector);
                }
            }
            else
            {
                wasInputHeld = false;
            }
        }

        private Vector2 ReadInput()
        {
            // 1. Coba baca dari New Input System moveAction
            if (moveAction != null)
            {
                Vector2 val = moveAction.ReadValue<Vector2>();
                if (val.sqrMagnitude > 0.1f) return val;
            }

            // 2. Failsafe baca langsung dari Keyboard.current (New Input System)
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) return Vector2.up;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) return Vector2.down;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) return Vector2.left;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) return Vector2.right;
            }

            // 3. Failsafe Gamepad
            var pad = Gamepad.current;
            if (pad != null)
            {
                if (pad.dpad.up.isPressed) return Vector2.up;
                if (pad.dpad.down.isPressed) return Vector2.down;
                if (pad.dpad.left.isPressed) return Vector2.left;
                if (pad.dpad.right.isPressed) return Vector2.right;
            }

            return Vector2.zero;
        }

        private void ProcessMoveInput(Vector2 input)
        {
            Vector3 direction = CalculateWorldDirection(input);
            if (direction == Vector3.zero) return;

            TryMoveInDirection(direction);
        }

        /// <summary>
        /// Mengubah input 2D (WASD) ke arah dunia 3D selaras dengan grid denah level.
        /// W / Up    = +Z (maju lurus di grid)
        /// S / Down  = -Z (mundur)
        /// A / Left  = -X (belok kiri di grid)
        /// D / Right = +X (belok kanan di grid)
        /// </summary>
        private Vector3 CalculateWorldDirection(Vector2 input)
        {
            // Tentukan arah dominan jika input analog/diagonal
            if (Mathf.Abs(input.y) >= Mathf.Abs(input.x))
            {
                return input.y > 0 ? Vector3.forward : Vector3.back;
            }
            else
            {
                return input.x > 0 ? Vector3.right : Vector3.left;
            }
        }

        private void TryMoveInDirection(Vector3 direction)
        {
            // Pastikan posisi node saat ini sudah valid
            if (currentNode == null)
            {
                SnapToNearestNode();
            }

            // 1. Temukan node target berdasarkan relasi tetangga atau posisi terdekat
            GridNode targetNode = FindTargetNode(direction);

            if (targetNode == null)
            {
                // Tidak ada jalan / rute di arah tersebut -> cukup putar badan
                StartCoroutine(RotateTowards(direction));
                return;
            }

            // 2. Validasi apakah node tujuan boleh diinjak (Walkable)
            if (!targetNode.isWalkable)
            {
                // Jembatan sedang tertutup / ubin runtuh -> hanya putar badan
                StartCoroutine(RotateTowards(direction));
                return;
            }

            // 3. Validasi apakah jalur diblokir tembok (Blocked Path)
            if (currentNode != null && !currentNode.CanMoveTo(targetNode))
            {
                StartCoroutine(RotateTowards(direction));
                return;
            }

            // 4. Cek Interaksi Pertarungan (Combat) dengan Demon
            var demon = targetNode.GetComponentInChildren<Enemy.DemonAI>();
            if (demon != null && demon.IsAlive)
            {
                Vector3 moveDirNorm = direction.normalized;
                Vector3 demonFaceDir = demon.FacingDirection.normalized;

                // Hitung sudut hadap (Face-to-Face vs Flank/Backstab)
                float dot = Vector3.Dot(moveDirNorm, demonFaceDir);
                if (dot < -0.35f)
                {
                    // Masuk dari DEPAN wajah Demon -> Pemain KALAH!
                    StartCoroutine(CombatPlayerDefeatedSequence(demon, direction));
                    return;
                }
                else
                {
                    // Masuk dari SAMPING atau BELAKANG -> Demon KALAH!
                    StartCoroutine(CombatDemonDefeatedSequence(demon, targetNode, direction));
                    return;
                }
            }

            // 5. Langkah normal
            StartCoroutine(StepRoutine(targetNode, direction));
        }

        private GridNode FindTargetNode(Vector3 direction)
        {
            // A. Cek dari daftar tetangga langsung currentNode
            if (currentNode != null && currentNode.neighbors.Count > 0)
            {
                GridNode bestNeighbor = null;
                float bestDot = 0.35f;

                foreach (var neighbor in currentNode.neighbors)
                {
                    if (neighbor == null) continue;

                    Vector3 toNeighbor = (neighbor.WorldPosition - currentNode.WorldPosition).normalized;
                    float dot = Vector3.Dot(direction.normalized, toNeighbor);

                    if (dot > bestDot)
                    {
                        bestDot = dot;
                        bestNeighbor = neighbor;
                    }
                }

                if (bestNeighbor != null)
                    return bestNeighbor;
            }

            // B. Fallback: Cari node berdasarkan estimasi koordinat di GridManager
            if (GridManager.Instance != null)
            {
                Vector3 estimatedPos = transform.position + direction.normalized * tileSize;
                return GridManager.Instance.GetNodeAt(estimatedPos, 1.4f);
            }

            return null;
        }

        private IEnumerator StepRoutine(GridNode targetNode, Vector3 direction)
        {
            isMoving = true;
            if (TurnManager.Instance != null)
                TurnManager.Instance.NotifyPlayerMoving();

            if (animator != null) animator.SetBool(IsWalkingHash, true);

            Quaternion targetRot = Quaternion.LookRotation(direction, Vector3.up);
            if (characterController != null) characterController.enabled = false;

            Vector3 destination = targetNode.WorldPosition;
            destination.y = transform.position.y;

            while (Vector3.Distance(transform.position, destination) > 0.02f)
            {
                transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = destination;
            transform.rotation = targetRot;

            if (characterController != null) characterController.enabled = true;
            if (animator != null) animator.SetBool(IsWalkingHash, false);

            if (currentNode != null) currentNode.currentOccupant = null;
            currentNode = targetNode;
            currentNode.currentOccupant = gameObject;

            isMoving = false;

            // Evaluasi event pada node tujuan
            EvaluateNodeEvents(currentNode);

            // Beri tahu TurnManager bahwa 1 langkah selesai
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.NotifyPlayerStepFinished(transform.position);
            }
        }

        private void EvaluateNodeEvents(GridNode node)
        {
            if (node == null) return;

            if (node.nodeType == NodeType.Finish)
            {
                if (LevelManager.Instance != null)
                    LevelManager.Instance.TriggerStageClear();
                return;
            }

            var checkpoint = node.GetComponent<Environment.CheckpointNode>();
            if (checkpoint != null)
            {
                checkpoint.ActivateCheckpoint();
            }

            var lever = node.GetComponentInChildren<Environment.LeverInteractable>();
            if (lever != null)
            {
                lever.Interact();
            }

            var cracked = node.GetComponent<Environment.CrackedLavaTile>();
            if (cracked != null)
            {
                cracked.OnSteppedOn(this);
            }
        }

        private IEnumerator RotateTowards(Vector3 direction)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction, Vector3.up);
            float elapsed = 0f;
            while (elapsed < 0.12f)
            {
                elapsed += Time.deltaTime;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
                yield return null;
            }
            transform.rotation = targetRot;
        }

        private IEnumerator CombatPlayerDefeatedSequence(Enemy.DemonAI demon, Vector3 direction)
        {
            isMoving = true;
            if (TurnManager.Instance != null)
                TurnManager.Instance.NotifyPlayerMoving();

            yield return StartCoroutine(RotateTowards(direction));

            demon.Attack();
            if (animator != null) animator.SetTrigger(DieHash);

            yield return new WaitForSeconds(0.6f);

            isMoving = false;
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.TriggerPlayerDeath();
            }
        }

        private IEnumerator CombatDemonDefeatedSequence(Enemy.DemonAI demon, GridNode targetNode, Vector3 direction)
        {
            isMoving = true;
            if (TurnManager.Instance != null)
                TurnManager.Instance.NotifyPlayerMoving();

            yield return StartCoroutine(RotateTowards(direction));
            if (animator != null) animator.SetTrigger(AttackHash);

            demon.Die();
            yield return new WaitForSeconds(0.25f);

            yield return StartCoroutine(StepRoutine(targetNode, direction));
        }

        public void SnapToNearestNode()
        {
            if (GridManager.Instance == null)
            {
                var gm = UnityEngine.Object.FindAnyObjectByType<GridManager>();
                if (gm != null) gm.ScanAllNodes();
            }

            if (GridManager.Instance != null)
            {
                currentNode = GridManager.Instance.GetNodeAt(transform.position, 3.5f);
                if (currentNode == null)
                {
                    currentNode = GridManager.Instance.GetStartNode();
                }

                if (currentNode != null)
                {
                    Vector3 pos = currentNode.WorldPosition;
                    pos.y = transform.position.y;
                    transform.position = pos;
                    currentNode.currentOccupant = gameObject;
                }
            }
        }

        public void RespawnAt(Vector3 respawnPosition)
        {
            StopAllCoroutines();
            isMoving = false;

            if (characterController != null) characterController.enabled = false;
            transform.position = respawnPosition;
            if (characterController != null) characterController.enabled = true;

            SnapToNearestNode();

            if (animator != null)
            {
                animator.SetBool(IsWalkingHash, false);
                animator.Play("Idle", 0, 0f);
            }
        }
    }
}
