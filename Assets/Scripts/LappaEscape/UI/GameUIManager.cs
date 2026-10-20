using UnityEngine;
using UnityEngine.UI;
using LappaEscape.Core;
using LappaEscape.Player;

namespace LappaEscape.UI
{
    /// <summary>
    /// Pengelola Antarmuka Pengguna (UI) dan feedback Game Over / Stage Clear.
    /// Membangun Canvas runtime otomatis jika belum ada di scene.
    /// </summary>
    public class GameUIManager : MonoBehaviour
    {
        public static GameUIManager Instance { get; private set; }

        [Header("Panel UI")]
        public GameObject winPanel;
        public GameObject losePanel;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            BuildRuntimeUIIfNeeded();
        }

        private void OnEnable()
        {
            LevelManager.OnStageClear += HandleStageClear;
            LevelManager.OnPlayerDied += HandlePlayerDied;
            LevelManager.OnPlayerRespawned += HandlePlayerRespawned;
        }

        private void OnDisable()
        {
            LevelManager.OnStageClear -= HandleStageClear;
            LevelManager.OnPlayerDied -= HandlePlayerDied;
            LevelManager.OnPlayerRespawned -= HandlePlayerRespawned;
        }

        private void HandleStageClear()
        {
            if (winPanel != null) winPanel.SetActive(true);
            if (losePanel != null) losePanel.SetActive(false);
        }

        private void HandlePlayerDied()
        {
            if (losePanel != null) losePanel.SetActive(true);
            if (winPanel != null) winPanel.SetActive(false);
        }

        private void HandlePlayerRespawned()
        {
            if (losePanel != null) losePanel.SetActive(false);
        }

        public void OnClickRestart()
        {
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.RestartCurrentScene();
            }
        }

        public void OnClickRespawn()
        {
            var player = Object.FindAnyObjectByType<LappaPlayerController>();
            if (LevelManager.Instance != null && player != null)
            {
                LevelManager.Instance.RespawnPlayer(player);
            }
        }

        private void BuildRuntimeUIIfNeeded()
        {
            if (winPanel != null && losePanel != null) return;

            // Buat Canvas runtime sederhana jika belum dirancang manual
            var canvasObj = new GameObject("Runtime_GameUI_Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // Panel Menang
            winPanel = CreateOverlayPanel(canvasObj.transform, "WinPanel", new Color(0.1f, 0.4f, 0.2f, 0.85f), "STAGE 1 CLEAR!\nSelamat, kamu berhasil kabur!");
            var btnWin = CreateButton(winPanel.transform, "Main Lagi (Restart)", new Vector2(0, -60));
            btnWin.onClick.AddListener(OnClickRestart);
            winPanel.SetActive(false);

            // Panel Kalah
            losePanel = CreateOverlayPanel(canvasObj.transform, "LosePanel", new Color(0.5f, 0.05f, 0.05f, 0.85f), "YOU DIED\nKamu tereliminasi oleh bahaya lava / demon!");
            var btnRespawn = CreateButton(losePanel.transform, "Respawn di Checkpoint", new Vector2(0, -40));
            btnRespawn.onClick.AddListener(OnClickRespawn);

            var btnRestartLose = CreateButton(losePanel.transform, "Ulangi Dari Awal", new Vector2(0, -100));
            btnRestartLose.onClick.AddListener(OnClickRestart);
            losePanel.SetActive(false);
        }

        private GameObject CreateOverlayPanel(Transform parent, string name, Color bgColor, string message)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = panel.AddComponent<Image>();
            img.color = bgColor;

            // Text
            var textObj = new GameObject("MessageText");
            textObj.transform.SetParent(panel.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchoredPosition = new Vector2(0, 60);
            textRect.sizeDelta = new Vector2(600, 140);

            var txt = textObj.AddComponent<Text>();
            txt.text = message;
            txt.fontSize = 28;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return panel;
        }

        private Button CreateButton(Transform parent, string label, Vector2 pos)
        {
            var btnObj = new GameObject("Btn_" + label);
            btnObj.transform.SetParent(parent, false);

            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(240, 45);

            var img = btnObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);

            var btn = btnObj.AddComponent<Button>();

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var txt = textObj.AddComponent<Text>();
            txt.text = label;
            txt.fontSize = 18;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return btn;
        }
    }
}
