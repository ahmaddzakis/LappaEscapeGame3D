using System.Collections;
using UnityEngine;
using LappaEscape.Core;

namespace LappaEscape.Environment
{
    public enum BridgeColor
    {
        Red,
        DarkRed,
        Purple
    }

    /// <summary>
    /// Pengontrol jembatan warna dinamis (naik/turun).
    /// Terhubung dengan tuas toggle warna bersesuaian.
    /// </summary>
    [RequireComponent(typeof(GridNode))]
    public class BridgeController : MonoBehaviour
    {
        [Header("Konfigurasi Jembatan")]
        public BridgeColor bridgeColor = BridgeColor.Red;
        public bool isRaisedInitially = false;
        public float moveSpeed = 8.0f;

        [Header("Tinggi Posisi")]
        public float raisedY = 0f;
        public float loweredY = -3.0f;

        [Header("Status Saat Ini")]
        public bool isRaised = false;

        private GridNode gridNode;
        private Coroutine moveCoroutine;

        private void Awake()
        {
            gridNode = GetComponent<GridNode>();
            gridNode.nodeType = NodeType.Bridge;
        }

        private void Start()
        {
            SetRaised(isRaisedInitially, false);
        }

        public void Toggle()
        {
            SetRaised(!isRaised, true);
        }

        public void SetRaised(bool raised, bool animate = true)
        {
            isRaised = raised;
            if (gridNode != null)
            {
                gridNode.isWalkable = isRaised;
            }

            Vector3 targetPos = new Vector3(transform.position.x, isRaised ? raisedY : loweredY, transform.position.z);

            if (animate && Application.isPlaying)
            {
                if (moveCoroutine != null) StopCoroutine(moveCoroutine);
                moveCoroutine = StartCoroutine(AnimateMove(targetPos));
            }
            else
            {
                transform.position = targetPos;
            }
        }

        private IEnumerator AnimateMove(Vector3 targetPos)
        {
            while (Vector3.Distance(transform.position, targetPos) > 0.02f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = targetPos;
        }
    }
}
