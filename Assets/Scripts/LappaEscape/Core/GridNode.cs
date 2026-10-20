using System.Collections.Generic;
using UnityEngine;

namespace LappaEscape.Core
{
    public enum NodeType
    {
        Normal,         // Petak abu-abu normal
        Start,          // Titik mulai (kuning)
        Finish,         // Titik akhir / menang (cyan)
        Checkpoint,     // Titik simpan sementara (putih)
        Cracked,        // Ubin retak lava (ungu/lava)
        Bridge,         // Jembatan warna (merah, cokelat, ungu)
        SlidingBridge   // Jembatan geser searah panah
    }

    /// <summary>
    /// Komponen petak/node individual pada papan puzzle Lappa Escape.
    /// Menyimpan koordinat, tetangga jalur yang valid, serta status kelayakan pijak (Walkable).
    /// </summary>
    [SelectionBase]
    public class GridNode : MonoBehaviour
    {
        [Header("Identitas Node")]
        public Vector2Int gridCoordinate;
        public NodeType nodeType = NodeType.Normal;

        [Header("Status Keterbukaan")]
        [Tooltip("Apakah petak ini saat ini bisa diinjak?")]
        public bool isWalkable = true;

        [Header("Jalur Terblokir (Blocked Paths)")]
        [Tooltip("Daftar node tetangga yang diblokir tembok/rintangan (garis biru di denah)")]
        public List<GridNode> blockedNeighbors = new List<GridNode>();

        [Header("Tetangga Terhubung (Valid Neighbors)")]
        public List<GridNode> neighbors = new List<GridNode>();

        [Header("Objek yang Sedang Berdiri")]
        public GameObject currentOccupant;

        public Vector3 WorldPosition => transform.position;

        /// <summary>
        /// Mengecek apakah ada koneksi jalur valid dari node ini ke targetNode.
        /// </summary>
        public bool CanMoveTo(GridNode targetNode)
        {
            if (targetNode == null) return false;
            if (!targetNode.isWalkable) return false;
            if (blockedNeighbors.Contains(targetNode)) return false;
            if (targetNode.blockedNeighbors.Contains(this)) return false;

            // Jika tetangga eksplisit sudah didefinisikan, gunakan daftar tetangga
            if (neighbors.Count > 0)
            {
                return neighbors.Contains(targetNode);
            }

            // Fallback: jarak Manhattan / Euclidean tepat 2 unit (atau sqrt(8) untuk diagonal)
            float dist = Vector3.Distance(transform.position, targetNode.transform.position);
            return dist <= 3.0f;
        }

        private void OnDrawGizmos()
        {
            // Visualisasi node di Scene View
            switch (nodeType)
            {
                case NodeType.Start:
                    Gizmos.color = Color.yellow;
                    break;
                case NodeType.Finish:
                    Gizmos.color = Color.cyan;
                    break;
                case NodeType.Checkpoint:
                    Gizmos.color = Color.white;
                    break;
                case NodeType.Cracked:
                    Gizmos.color = new Color(0.8f, 0.2f, 0.8f);
                    break;
                case NodeType.Bridge:
                    Gizmos.color = isWalkable ? Color.red : new Color(0.4f, 0f, 0f, 0.4f);
                    break;
                case NodeType.SlidingBridge:
                    Gizmos.color = Color.magenta;
                    break;
                default:
                    Gizmos.color = isWalkable ? Color.gray : Color.black;
                    break;
            }

            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.05f, new Vector3(1.8f, 0.1f, 1.8f));

            // Gambar garis koneksi ke tetangga
            Gizmos.color = Color.green;
            foreach (var n in neighbors)
            {
                if (n != null && !blockedNeighbors.Contains(n))
                {
                    Gizmos.DrawLine(transform.position + Vector3.up * 0.1f, n.transform.position + Vector3.up * 0.1f);
                }
            }

            // Gambar garis merah untuk blocked path
            Gizmos.color = Color.blue;
            foreach (var b in blockedNeighbors)
            {
                if (b != null)
                {
                    Vector3 mid = (transform.position + b.transform.position) * 0.5f + Vector3.up * 0.2f;
                    Gizmos.DrawCube(mid, new Vector3(0.5f, 0.4f, 0.1f));
                }
            }
        }
    }
}
