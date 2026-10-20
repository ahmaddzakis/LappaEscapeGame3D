using System.Collections.Generic;
using UnityEngine;

namespace LappaEscape.Core
{
    /// <summary>
    /// Manajer graf grid papan puzzle.
    /// Menyimpan peta seluruh node, pencarian petak terdekat, dan validasi navigasi.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

        [Header("Pengaturan Ukuran Petak")]
        [Tooltip("Jarak spasi standar antar node (default: 2 meter)")]
        public float tileSize = 2.0f;

        [Header("Daftar Node Terdaftar")]
        [SerializeField] private List<GridNode> allNodes = new List<GridNode>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            ScanAllNodes();
        }

        [ContextMenu("Scan All Nodes in Scene")]
        public void ScanAllNodes()
        {
            allNodes.Clear();
            allNodes.AddRange(UnityEngine.Object.FindObjectsByType<GridNode>(FindObjectsInactive.Include));
        }

        public GridNode GetNodeAt(Vector3 worldPos, float tolerance = 1.0f)
        {
            GridNode closest = null;
            float minDist = tolerance;

            foreach (var node in allNodes)
            {
                if (node == null) continue;
                float d = Vector3.Distance(new Vector3(node.transform.position.x, 0, node.transform.position.z),
                                           new Vector3(worldPos.x, 0, worldPos.z));
                if (d < minDist)
                {
                    minDist = d;
                    closest = node;
                }
            }
            return closest;
        }

        public GridNode GetStartNode()
        {
            return allNodes.Find(n => n != null && n.nodeType == NodeType.Start);
        }

        public GridNode GetFinishNode()
        {
            return allNodes.Find(n => n != null && n.nodeType == NodeType.Finish);
        }

        public void RegisterNode(GridNode node)
        {
            if (node != null && !allNodes.Contains(node))
            {
                allNodes.Add(node);
            }
        }
    }
}
