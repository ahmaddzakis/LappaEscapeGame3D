using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Kamera sudut tinggi (High-Angle 3D Isometric Diorama) untuk mode Skeleton GO.
/// Membingkai arena dungeon dengan elegan, mengikuti pergerakan karakter secara smooth,
/// dan memberikan efek parallax 3D halus saat mouse bergerak.
/// </summary>
[ExecuteAlways]
public class DioramaCamera : MonoBehaviour
{
    [Header("Target Karakter")]
    [Tooltip("Karakter yang diikuti kamera (otomatis dicari jika kosong)")]
    public Transform target;

    [Header("Sudut & Jarak Diorama")]
    [Tooltip("Jarak kamera dari titik fokus karakter")]
    public float distance = 11.5f;

    [Tooltip("Sudut elevasi kamera dari atas (derajat, 40-55 ideal untuk diorama)")]
    [Range(20f, 80f)]
    public float pitchAngle = 46f;

    [Tooltip("Sudut orientasi diagonal ruangan (45 derajat = isometrik klasik)")]
    [Range(0f, 360f)]
    public float yawAngle = 45f;

    [Tooltip("Offset ketinggian titik fokus di atas karakter")]
    public float targetHeightOffset = 1.0f;

    [Header("Kehalusan (Smoothing)")]
    [Tooltip("Waktu kehalusan kamera mengikuti pergerakan (detik)")]
    public float followSmoothTime = 0.16f;

    [Header("Efek Parallax Mouse (Kedalaman 3D)")]
    [Tooltip("Beri sentuhan 3D hidup dengan kemiringan halus mengikuti kursor mouse")]
    public bool enableMouseParallax = true;

    [Tooltip("Kekuatan efek kemiringan parallax")]
    public float parallaxStrength = 0.5f;

    private Vector3 currentVelocity;
    private Vector2 currentParallax;

    void Start()
    {
        FindTargetIfNeeded();
        ApplyTransformImmediate();
    }

    void LateUpdate()
    {
        if (target == null)
        {
            FindTargetIfNeeded();
            if (target == null) return;
        }

        // Hitung parallax halus saat Play Mode
        if (Application.isPlaying && enableMouseParallax && Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            float normX = (mousePos.x / Screen.width - 0.5f) * 2f;
            float normY = (mousePos.y / Screen.height - 0.5f) * 2f;
            Vector2 targetParallax = new Vector2(normX, normY) * parallaxStrength;
            currentParallax = Vector2.Lerp(currentParallax, targetParallax, Time.deltaTime * 3.5f);
        }
        else
        {
            currentParallax = Vector2.zero;
        }

        Quaternion rotation = Quaternion.Euler(pitchAngle - currentParallax.y, yawAngle + currentParallax.x, 0f);
        Vector3 focusPoint = target.position + Vector3.up * targetHeightOffset;
        Vector3 desiredPosition = focusPoint - (rotation * Vector3.forward * distance);

        if (Application.isPlaying)
        {
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, followSmoothTime);
            transform.rotation = rotation;
        }
        else
        {
            ApplyTransformImmediate();
        }
    }

    private void FindTargetIfNeeded()
    {
        if (target != null) return;

        var gridMover = FindAnyObjectByType<GridPlayerMovement>();
        if (gridMover != null)
        {
            target = gridMover.transform;
            return;
        }

        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            target = playerObj.transform;
            return;
        }

        var charObj = GameObject.Find("UD_demo_character");
        if (charObj != null)
        {
            target = charObj.transform;
        }
    }

    [ContextMenu("Align Diorama View Now")]
    public void ApplyTransformImmediate()
    {
        if (target == null) return;
        Quaternion rotation = Quaternion.Euler(pitchAngle, yawAngle, 0f);
        Vector3 focusPoint = target.position + Vector3.up * targetHeightOffset;
        transform.position = focusPoint - (rotation * Vector3.forward * distance);
        transform.rotation = rotation;
    }
}
