using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LappaEscape.Core;

namespace LappaEscape.Enemy
{
    public enum DemonType
    {
        Sentry,   // Diam mengawasi satu arah
        Patroller // Bergerak bolak-balik antar node setiap giliran
    }

    /// <summary>
    /// Kecerdasan buatan musuh (Demon) dengan aturan eliminasi hadap:
    /// - Pemain kalah jika menyerang Face-to-Face (dari depan tatapan Demon).
    /// - Demon kalah jika diserang dari Samping (Flank) atau Belakang (Backstab).
    /// </summary>
    public class DemonAI : MonoBehaviour
    {
        [Header("Tipe & Status Demon")]
        public DemonType demonType = DemonType.Sentry;
        public bool isAlive = true;
        public bool IsAlive => isAlive;

        [Header("Arah Hadap Penglihatan")]
        [Tooltip("Arah mata memandang (Zona Bahaya Mematikan bagi pemain)")]
        public Vector3 facingDirection = Vector3.forward;
        public Vector3 FacingDirection => facingDirection;

        [Header("Rute Patroli (Jika Patroller)")]
        public List<GridNode> patrolNodes = new List<GridNode>();
        private int currentPatrolIndex = 0;
        private bool isPatrolForward = true;

        [Header("Node Saat Ini")]
        public GridNode currentNode;

        [Header("Komponen & Animasi")]
        public Animator animator;
        public GameObject facingIndicator;

        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int DieHash = Animator.StringToHash("Die");

        private void OnEnable()
        {
            TurnManager.OnEnemyTurnStart += HandleEnemyTurn;
        }

        private void OnDisable()
        {
            TurnManager.OnEnemyTurnStart -= HandleEnemyTurn;
        }

        private void Start()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            // Pastikan arah hadap rotasi sesuai facingDirection
            if (facingDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(facingDirection, Vector3.up);
            }

            SnapToNode();
        }

        public void SnapToNode()
        {
            if (GridManager.Instance != null)
            {
                currentNode = GridManager.Instance.GetNodeAt(transform.position, 1.5f);
                if (currentNode != null)
                {
                    transform.position = currentNode.WorldPosition;
                    currentNode.currentOccupant = gameObject;
                }
            }
        }

        private void HandleEnemyTurn()
        {
            if (!isAlive || demonType != DemonType.Patroller) return;

            // Logika patroli 1 langkah
            if (patrolNodes.Count >= 2)
            {
                StepPatrol();
            }
        }

        private void StepPatrol()
        {
            int nextIndex = currentPatrolIndex + (isPatrolForward ? 1 : -1);
            if (nextIndex >= patrolNodes.Count)
            {
                isPatrolForward = false;
                nextIndex = patrolNodes.Count - 2;
            }
            else if (nextIndex < 0)
            {
                isPatrolForward = true;
                nextIndex = 1;
            }

            currentPatrolIndex = nextIndex;
            GridNode targetNode = patrolNodes[currentPatrolIndex];

            if (targetNode != null && targetNode.isWalkable)
            {
                Vector3 moveDir = (targetNode.WorldPosition - transform.position).normalized;
                facingDirection = moveDir;
                StartCoroutine(MoveToPatrolNode(targetNode, moveDir));
            }
        }

        private IEnumerator MoveToPatrolNode(GridNode targetNode, Vector3 dir)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            Vector3 targetPos = targetNode.WorldPosition;

            while (Vector3.Distance(transform.position, targetPos) > 0.02f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, 4f * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 15f * Time.deltaTime);
                yield return null;
            }

            transform.position = targetPos;
            transform.rotation = targetRot;

            if (currentNode != null) currentNode.currentOccupant = null;
            currentNode = targetNode;
            currentNode.currentOccupant = gameObject;
        }

        public void Attack()
        {
            if (animator != null) animator.SetTrigger(AttackHash);
            Debug.Log($"<color=red>[DemonAI] '{name}' MEMBUNUH PEMAIN (Face-to-Face Attack)!</color>");
        }

        public void Die()
        {
            if (!isAlive) return;

            isAlive = false;
            if (currentNode != null) currentNode.currentOccupant = null;

            if (animator != null) animator.SetTrigger(DieHash);
            Debug.Log($"<color=green>[DemonAI] '{name}' DIELIMINASI DARI SAMPING/BELAKANG!</color>");

            StartCoroutine(DeathVanishRoutine());
        }

        private IEnumerator DeathVanishRoutine()
        {
            // Efek menyusut / menghilang ke bawah
            yield return new WaitForSeconds(0.4f);
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;
            while (elapsed < 0.25f)
            {
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / 0.25f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            gameObject.SetActive(false);
        }

        private void OnDrawGizmos()
        {
            // Gambar panah arah hadap di Scene view
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position + Vector3.up * 1f, facingDirection * 1.5f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 1f + facingDirection * 1.5f, 0.2f);
        }
    }
}
