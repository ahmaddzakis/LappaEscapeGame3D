using System.Collections;
using UnityEngine;
using LappaEscape.Core;

namespace LappaEscape.Environment
{
    /// <summary>
    /// Jembatan geser yang berpindah posisi secara linier searah panah desain level.
    /// Digerakkan oleh tuas putih (Sliding Lever).
    /// </summary>
    public class SlidingBridge : MonoBehaviour
    {
        [Header("Posisi Geser")]
        public Transform positionA;
        public Transform positionB;
        public float slideSpeed = 5.0f;

        [Header("Node Terkait")]
        public GridNode nodeA;
        public GridNode nodeB;

        [Header("Status")]
        public bool isAtPositionB = false;

        private Coroutine slideRoutine;

        private void Start()
        {
            UpdateNodeWalkability();
        }

        public void TogglePosition()
        {
            isAtPositionB = !isAtPositionB;
            Transform targetTransform = isAtPositionB ? positionB : positionA;

            if (slideRoutine != null) StopCoroutine(slideRoutine);
            slideRoutine = StartCoroutine(SlideTo(targetTransform.position));
        }

        private IEnumerator SlideTo(Vector3 targetPos)
        {
            while (Vector3.Distance(transform.position, targetPos) > 0.02f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, slideSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = targetPos;
            UpdateNodeWalkability();
        }

        private void UpdateNodeWalkability()
        {
            if (nodeA != null) nodeA.isWalkable = !isAtPositionB;
            if (nodeB != null) nodeB.isWalkable = isAtPositionB;
        }
    }
}
