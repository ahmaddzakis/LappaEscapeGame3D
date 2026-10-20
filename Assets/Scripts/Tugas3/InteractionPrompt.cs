using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menampilkan teks prompt interaksi di bagian bawah layar, mis. "Press 'E' to open".
/// Canvas dan Text dibuat otomatis lewat kode, jadi tidak perlu setup UI manual di scene.
/// </summary>
public static class InteractionPrompt
{
    private static GameObject root;
    private static Text label;
    private static UnityEngine.Object owner;

    /// <summary>Tampilkan prompt. 'requester' dipakai agar hanya pemilik prompt yang bisa menyembunyikannya.</summary>
    public static void Show(UnityEngine.Object requester, string message)
    {
        EnsureCreated();
        owner = requester;
        if (label.text != message) label.text = message;
        if (!root.activeSelf) root.SetActive(true);
    }

    /// <summary>Sembunyikan prompt, hanya jika requester sama dengan yang terakhir menampilkannya.</summary>
    public static void Hide(UnityEngine.Object requester)
    {
        if (root == null) return;
        if (owner != requester) return;

        root.SetActive(false);
        owner = null;
    }

    private static void EnsureCreated()
    {
        if (root != null && label != null) return;

        // Canvas
        root = new GameObject("InteractionPromptCanvas");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Panel latar belakang semi transparan
        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root.transform, false);

        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 140f);
        panelRect.sizeDelta = new Vector2(560f, 80f);

        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        // Teks
        var textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObj.transform.SetParent(panel.transform, false);

        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        label = textObj.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (label.font == null) label.font = Font.CreateDynamicFontFromOSFont("Arial", 32);
        label.fontSize = 34;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;

        root.SetActive(false);
    }
}