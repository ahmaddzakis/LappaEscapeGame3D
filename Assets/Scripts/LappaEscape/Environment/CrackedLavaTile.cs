using System.Collections;
using UnityEngine;
using LappaEscape.Core;
using LappaEscape.Player;

namespace LappaEscape.Environment
{
    /// <summary>
    /// Ubin retak lava yang runtuh jika diinjak 2 kali.
    /// Injak ke-1: berpendar merah membara.
    /// Injak ke-2: runtuh ke dalam kubangan lava dan pemain gugur jika tetap berdiri.
    /// </summary>
    [RequireComponent(typeof(GridNode))]
    public class CrackedLavaTile : MonoBehaviour
    {
        [Header("Pengaturan Durabilitas")]
        public int currentSteps = 0;
        public int maxSteps = 2;

        [Header("Visual Feedback")]
        public Renderer tileRenderer;
        public Color crackedColor = new Color(1f, 0.4f, 0f); // Oranye lava
        public GameObject lavaPuddleEffect;

        private GridNode gridNode;
        private Color originalColor;
        private bool isCollapsed = false;

        private void Awake()
        {
            gridNode = GetComponent<GridNode>();
            if (tileRenderer == null)
                tileRenderer = GetComponentInChildren<Renderer>();

            if (tileRenderer != null && tileRenderer.material != null)
                originalColor = tileRenderer.material.color;
        }

        public void OnSteppedOn(LappaPlayerController player)
        {
            if (isCollapsed)
            {
                // Pemain menginjak lava terbuka -> Kematian
                if (LevelManager.Instance != null)
                    LevelManager.Instance.TriggerPlayerDeath();
                return;
            }

            currentSteps++;

            if (currentSteps == 1)
            {
                // Injak ke-1: Tanda bahaya (retak membara)
                StartCoroutine(CrackingShakeEffect());
                if (tileRenderer != null && tileRenderer.material != null)
                {
                    tileRenderer.material.color = crackedColor;
                }
                Debug.Log($"<color=orange>[CrackedTile] '{name}' mulai retak dan membara!</color>");
            }
            else if (currentSteps >= maxSteps)
            {
                // Injak ke-2: Runtuh ke lava!
                StartCoroutine(CollapseSequence(player));
            }
        }

        private IEnumerator CrackingShakeEffect()
        {
            Vector3 origPos = transform.position;
            float elapsed = 0f;
            while (elapsed < 0.2f)
            {
                float x = Random.Range(-0.06f, 0.06f);
                float z = Random.Range(-0.06f, 0.06f);
                transform.position = origPos + new Vector3(x, 0, z);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = origPos;
        }

        private IEnumerator CollapseSequence(LappaPlayerController player)
        {
            isCollapsed = true;
            gridNode.isWalkable = false;

            Debug.Log($"<color=red>[CrackedTile] '{name}' RUNTUH KE LAVA!</color>");

            // Efek runtuh ke bawah
            Vector3 targetFallPos = transform.position + Vector3.down * 4.0f;
            float elapsed = 0f;
            while (elapsed < 0.35f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetFallPos, 12f * Time.deltaTime);
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (lavaPuddleEffect != null)
                lavaPuddleEffect.SetActive(true);

            // Pemain ikut jatuh ke lava jika masih berada di ubin ini
            if (player != null && player.currentNode == gridNode)
            {
                if (LevelManager.Instance != null)
                {
                    LevelManager.Instance.TriggerPlayerDeath();
                }
            }
        }

        public void ResetTile()
        {
            isCollapsed = false;
            currentSteps = 0;
            if (gridNode != null) gridNode.isWalkable = true;
            if (tileRenderer != null && tileRenderer.material != null)
                tileRenderer.material.color = originalColor;
            if (lavaPuddleEffect != null)
                lavaPuddleEffect.SetActive(false);
        }
    }
}
