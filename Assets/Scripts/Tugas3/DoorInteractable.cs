using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Pintu interaktif: saat player dekat muncul teks "Press 'E' to open",
/// tekan E untuk membuka/menutup pintu (toggle).
/// Pasang di GameObject "Door".
/// </summary>
[DisallowMultipleComponent]
public class DoorInteractable : MonoBehaviour
{
    [Header("Interaction")]
    [Tooltip("Kosong = dicari otomatis (GridPlayerMovement atau tag Player)")]
    public Transform player;

    [Tooltip("Area deteksi berbentuk persegi mengikuti ukuran pintu, lalu diperlebar sebesar nilai ini. " +
             "X = perluasan ke kiri-kanan (sumbu X dunia), Y = perluasan ke depan-belakang (sumbu Z dunia). " +
             "Prompt muncul & tombol E aktif jika player berada di dalam area ini.")]
    public Vector2 areaPadding = new Vector2(1f, 1f);

    [Tooltip("Geser titik pusat area dari pusat pintu (world unit). Berguna jika ingin area lebih ke depan atau belakang pintu.")]
    public Vector3 centerOffset = Vector3.zero;

    [Header("Gizmo")]
    [Tooltip("Aktif = kotak area deteksi selalu tampil di Scene view. Nonaktif = hanya saat Door dipilih.")]
    public bool alwaysShowGizmo = true;

    public string openPrompt = "Press 'E' to open";
    public string closePrompt = "Press 'E' to close";

    [Header("Door Motion")]
    [Tooltip("Titik engsel (opsional). Kosong = pintu berputar di pivot-nya sendiri. " +
             "Kalau pintu berputar di tengah, buat empty child di tepi pintu lalu isi di sini.")]
    public Transform hinge;

    [Tooltip("Sudut buka (derajat). Ubah ke negatif jika pintu terbuka ke arah yang salah.")]
    public float openAngle = 90f;

    [Tooltip("Kecepatan ayun pintu (derajat per detik)")]
    public float openSpeed = 120f;

    [Header("Fixes")]
    [Tooltip("Tambahkan BoxCollider otomatis jika pintu belum punya collider (supaya pintu tertutup tidak bisa ditembus)")]
    public bool autoAddCollider = true;

    [Tooltip("Tampilkan log di Console saat pintu di-toggle (berguna untuk debugging)")]
    public bool debugLog = true;

    private bool isOpen;
    private bool wasInRange;
    private float currentAngle;

    private Vector3 closedPosition;
    private Quaternion closedRotation;
    private Vector3 hingePosition;
    private Bounds interactionBounds;

    void Awake()
    {
        DisableConflictingAnimator();
        if (autoAddCollider) EnsureCollider();
        WarnIfStillStatic();

        // Posisi saat tertutup disimpan, lalu rotasi dihitung absolut dari sini (tanpa drift)
        closedPosition = transform.position;
        closedRotation = transform.rotation;
        hingePosition = hinge != null ? hinge.position : transform.position;

        // Area interaksi dihitung dari kondisi pintu tertutup, sehingga tetap stabil
        // di sekitar kusen walaupun pintu sedang terbuka.
        interactionBounds = CalculateBounds();
    }

    void Update()
    {
        if (player == null) FindPlayer();

        bool inRange = player != null && IsPlayerInRange();

        if (inRange)
        {
            InteractionPrompt.Show(this, isOpen ? closePrompt : openPrompt);

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            {
                isOpen = !isOpen;
                if (debugLog)
                    Debug.Log($"[DoorInteractable] '{name}' -> {(isOpen ? "membuka" : "menutup")}", this);
            }
        }
        else if (wasInRange)
        {
            InteractionPrompt.Hide(this);
        }

        wasInRange = inRange;

        AnimateDoor();
    }

    void OnDisable()
    {
        InteractionPrompt.Hide(this);
        wasInRange = false;
    }

    private void AnimateDoor()
    {
        float target = isOpen ? openAngle : 0f;
        if (Mathf.Approximately(currentAngle, target)) return;

        currentAngle = Mathf.MoveTowards(currentAngle, target, openSpeed * Time.deltaTime);

        // Putar mengelilingi titik engsel pada sumbu vertikal (Y dunia)
        Quaternion rotation = Quaternion.AngleAxis(currentAngle, Vector3.up);
        transform.SetPositionAndRotation(
            hingePosition + rotation * (closedPosition - hingePosition),
            rotation * closedRotation);
    }

    private bool IsPlayerInRange()
    {
        Vector3 center = interactionBounds.center + centerOffset;
        Vector3 p = player.position;

        // Ukuran pintu (saat tertutup) + padding. Tinggi diabaikan.
        float halfX = interactionBounds.extents.x + areaPadding.x;
        float halfZ = interactionBounds.extents.z + areaPadding.y;

        return Mathf.Abs(p.x - center.x) <= halfX &&
               Mathf.Abs(p.z - center.z) <= halfZ;
    }

    private void FindPlayer()
    {
        var grid = FindAnyObjectByType<GridPlayerMovement>();
        if (grid != null)
        {
            player = grid.transform;
            return;
        }

        var tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged != null) player = tagged.transform;
    }

    private Bounds CalculateBounds()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(transform.position, Vector3.one);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        return bounds;
    }

    // Pintu yang tidak punya collider bisa ditembus player.
    // Collider dibuat dari bounds mesh (di ruang lokal), jadi ikut berputar bersama pintu.
    private void EnsureCollider()
    {
        if (GetComponentInChildren<Collider>() != null) return;

        foreach (var meshFilter in GetComponentsInChildren<MeshFilter>())
        {
            if (meshFilter.sharedMesh == null) continue;

            var box = meshFilter.gameObject.AddComponent<BoxCollider>();
            box.center = meshFilter.sharedMesh.bounds.center;
            box.size = meshFilter.sharedMesh.bounds.size;
        }

        if (debugLog)
            Debug.Log($"[DoorInteractable] '{name}' tidak punya collider, BoxCollider ditambahkan otomatis.", this);
    }

    // Animator yang aktif pada pintu bisa menimpa transform setiap frame.
    private void DisableConflictingAnimator()
    {
        var animator = GetComponentInChildren<Animator>();
        if (animator != null && animator.enabled)
        {
            Debug.LogWarning($"[DoorInteractable] '{name}' punya Animator yang bisa menimpa gerakan pintu, Animator dinonaktifkan.", this);
            animator.enabled = false;
        }
    }

    private void WarnIfStillStatic()
    {
#if UNITY_EDITOR
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            var flags = GameObjectUtility.GetStaticEditorFlags(t.gameObject);
            if ((flags & StaticEditorFlags.BatchingStatic) != 0)
            {
                Debug.LogWarning($"[DoorInteractable] '{t.name}' masih 'Batching Static', sehingga tampilannya tidak bisa bergerak. " +
                                 "Matikan centang Static pada Door (dan child-nya), lalu Play lagi.", t);
                return;
            }
        }
#endif
    }

#if UNITY_EDITOR
    // Objek yang harus bergerak tidak boleh Batching Static. Otomatis dimatikan di Editor.
    void OnValidate()
    {
        if (Application.isPlaying) return;

        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            var flags = GameObjectUtility.GetStaticEditorFlags(t.gameObject);
            if ((flags & StaticEditorFlags.BatchingStatic) != 0)
            {
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags & ~StaticEditorFlags.BatchingStatic);
                EditorUtility.SetDirty(t.gameObject);
            }
        }
    }
#endif

    void OnDrawGizmos()
    {
        if (alwaysShowGizmo) DrawRadiusGizmo();
    }

    void OnDrawGizmosSelected()
    {
        if (!alwaysShowGizmo) DrawRadiusGizmo();
    }

    // Kotak area deteksi: persegi di lantai (kuning; hijau saat player di dalam area),
    // kotak kawat setinggi pintu, titik pusat (bola kecil), dan titik engsel (bola merah).
    private void DrawRadiusGizmo()
    {
        Bounds b = Application.isPlaying ? interactionBounds : CalculateBounds();
        Vector3 center = b.center + centerOffset;

        float halfX = b.extents.x + areaPadding.x;
        float halfZ = b.extents.z + areaPadding.y;
        float floorY = b.min.y + 0.02f;

        bool playerInside = Application.isPlaying && wasInRange;
        Color baseColor = playerInside ? Color.green : Color.yellow;

        // Persegi di lantai
        Vector3[] corners =
        {
            new Vector3(center.x - halfX, floorY, center.z - halfZ),
            new Vector3(center.x - halfX, floorY, center.z + halfZ),
            new Vector3(center.x + halfX, floorY, center.z + halfZ),
            new Vector3(center.x + halfX, floorY, center.z - halfZ),
        };

#if UNITY_EDITOR
        // Isi persegi semi transparan
        Handles.DrawSolidRectangleWithOutline(
            corners,
            new Color(baseColor.r, baseColor.g, baseColor.b, 0.12f),
            Color.clear);
#endif

        Gizmos.color = baseColor;
        for (int i = 0; i < corners.Length; i++)
        {
            Gizmos.DrawLine(corners[i], corners[(i + 1) % corners.Length]);
        }

        // Kotak kawat setinggi pintu
        Gizmos.DrawWireCube(
            new Vector3(center.x, b.center.y, center.z),
            new Vector3(halfX * 2f, b.size.y, halfZ * 2f));

        // Titik pusat area
        Gizmos.DrawSphere(center, 0.08f);

        // Titik engsel
        Gizmos.color = Color.red;
        Vector3 pivot = hinge != null ? hinge.position : transform.position;
        Gizmos.DrawWireSphere(pivot, 0.1f);
    }
}