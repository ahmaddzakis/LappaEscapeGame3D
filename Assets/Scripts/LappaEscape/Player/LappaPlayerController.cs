using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using LappaEscape.Core;

namespace LappaEscape.Player
{
    /// <summary>
    /// Kontroler pergerakan karakter petualang Lappa berbasis petak (Grid / Step-by-Step).
    /// Mengelola input diskrit, pergerakan mulus antar-node, validasi rintangan, dan interaksi tempur.
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

        [Tooltip("Input menyesuaikan sudut pandang kamera isometrik 45 derajat")]
        public bool useIsometricInput = true;

        [Header("Komponen Karakter")]
        public Animator animator;
        public CharacterController characterController;

        [Header("Node Saat Ini")]
        public GridNode currentNode;

        private bool isMoving = false;
        private static readonly int IsWalkingHash = Animator.StringToHash("isWalking");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int DieHash = Animator.StringToHash("Die");

        private void Start()
        {
            if (characterController == null)
                characterController = GetComponent<CharacterController>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            // Pasang tag Player
            if (!CompareTag("Player"))
                tag = "Player";

            // Temukan node awal tempat karakter berdiri
            SnapToNearestNode();
        }

        private void Update()
        {
            if (isMoving) return;

            // Cek apakah saat ini fase input pemain diizinkan
            if (TurnManager.Instance != null && !TurnManager.Instance.CanPlayerMove())
                return;

            Vector2 input = ReadDiscreteInput();
            if (input != Vector2.zero)
            {
                Vector3 direction = CalculateMoveDirection(input);
                if (direction != Vector3.zero)
                {
                    TryMoveInDirection(direction);
                }
            }
        }

        private Vector2 ReadDiscreteInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return Vector2.zero;

            // Gunakan wasPressedThisFrame agar 1 kali tekan = tepat 1 langkah
            if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame) return Vector2.up;
            if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame) return Vector2.down;
            if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame) return Vector2.left;
            if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame) return Vector2.right;

            return Vector2.zero;
        }

        private Vector3 CalculateMoveDirection(Vector2 input)
        {
            if (useIsometricInput)
            {
                // Dalam kamera isometrik 45 derajat:
                // W: serong kanan-atas (+Z, +X)
                // S: serong kiri-bawah (-Z, -X)
                // A: serong kiri-atas (+Z, -X)
                // D: serong kanan-bawah (-Z, +X)
                if (input == Vector2.up) return (Vector3.forward + Vector3.right).normalized;
                if (input == Vector2.down) return (Vector3.back + Vector3.left).normalized;
                if (input == Vector2.left) return (Vector3.forward + Vector3.left).normalized;
                if (input == Vector2.right) return (Vector3.back + Vector3.right).normalized;
            }
            else
            {
                if (input == Vector2.up) return Vector3.forward;
                if (input == Vector2.down) return Vector3.back;
                if (input == Vector2.left) return Vector3.left;
                if (input == Vector2.right) return Vector3.right;
            }

            return Vector3.zero;
        }

        private void TryMoveInDirection(Vector3 direction)
        {
            Vector3 targetPos = transform.position + direction * tileSize;

            // Cari node tujuan di GridManager
            GridNode targetNode = null;
            if (GridManager.Instance != null)
            {
                targetNode = GridManager.Instance.GetNodeAt(targetPos, 1.4f);
            }

            // Validasi apakah perpindahan diizinkan
            if (targetNode == null || !targetNode.isWalkable)
            {
                // Tidak ada lantai / jalan terhalang -> cukup putar badan menghadap arah tersebut
                StartCoroutine(RotateTowards(direction));
                return;
            }

            if (currentNode != null && !currentNode.CanMoveTo(targetNode))
            {
                // Jalur antar dua node diblokir tembok (Blocked Path)
                StartCoroutine(RotateTowards(direction));
                return;
            }

            // Cek interaksi Combat dengan musuh (Demon) jika petak target ada Demon
            var demon = targetNode.GetComponentInChildren<Enemy.DemonAI>();
            if (demon != null && demon.IsAlive)
            {
                // Evaluasi arah serangan
                Vector3 moveDirNorm = direction.normalized;
                Vector3 demonFaceDir = demon.FacingDirection.normalized;

                // Jika berhadapan langsung (Face-to-Face): sudut hadap berlawanan (Dot ~ -1)
                float dot = Vector3.Dot(moveDirNorm, demonFaceDir);
                if (dot < -0.4f)
                {
                    // Pemain masuk dari DEPAN wajah Demon -> Pemain KALAH!
                    StartCoroutine(CombatPlayerDefeatedSequence(demon, direction));
                    return;
                }
                else
                {
                    // Pemain masuk dari SAMPING atau BELAKANG -> Demon KALAH!
                    StartCoroutine(CombatDemonDefeatedSequence(demon, targetNode, direction));
                    return;
                }
            }

            // Langkah normal
            StartCoroutine(StepRoutine(targetNode, direction));
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
            destination.y = transform.position.y; // Pertahankan tinggi Y permukaan lantai

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

            // Update posisi node saat ini
            if (currentNode != null) currentNode.currentOccupant = null;
            currentNode = targetNode;
            currentNode.currentOccupant = gameObject;

            isMoving = false;

            // Evaluasi trigger khusus pada node
            EvaluateNodeEvents(currentNode);

            // Kabarkan selesai melangkah ke TurnManager
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.NotifyPlayerStepFinished(transform.position);
            }
        }

        private void EvaluateNodeEvents(GridNode node)
        {
            if (node == null) return;

            // Cek Finish Node
            if (node.nodeType == NodeType.Finish)
            {
                if (LevelManager.Instance != null)
                    LevelManager.Instance.TriggerStageClear();
                return;
            }

            // Cek Checkpoint Node
            var checkpoint = node.GetComponent<Environment.CheckpointNode>();
            if (checkpoint != null)
            {
                checkpoint.ActivateCheckpoint();
            }

            // Cek Tuas / Lever
            var lever = node.GetComponentInChildren<Environment.LeverInteractable>();
            if (lever != null)
            {
                lever.Interact();
            }

            // Cek Ubin Retak Lava
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

            // Putar tubuh menghadap demon
            yield return StartCoroutine(RotateTowards(direction));

            // Demon menyerang pemain
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

            // Serang demon dari samping / belakang
            yield return StartCoroutine(RotateTowards(direction));
            if (animator != null) animator.SetTrigger(AttackHash);

            demon.Die();
            yield return new WaitForSeconds(0.25f);

            // Melangkah masuk ke petak demon yang sudah tumbang
            yield return StartCoroutine(StepRoutine(targetNode, direction));
        }

        public void SnapToNearestNode()
        {
            if (GridManager.Instance != null)
            {
                currentNode = GridManager.Instance.GetNodeAt(transform.position, 2.5f);
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
