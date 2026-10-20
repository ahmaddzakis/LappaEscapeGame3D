using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Sistem pergerakan berbasis petak (Grid-based / Step-by-step) untuk mode Skeleton GO.
/// Karakter melangkah presisi sejauh tileSize (2 meter) setiap menekan arah WASD.
/// Memiliki deteksi rintangan (raycast) dan memicu OnStepFinished untuk sistem giliran AI.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class GridPlayerMovement : MonoBehaviour
{
    [Header("Pengaturan Grid")]
    [Tooltip("Ukuran 1 petak lantai dungeon (default: 2 meter)")]
    public float tileSize = 2f;

    [Tooltip("Kecepatan melangkah antar petak (makin tinggi makin snappy)")]
    public float moveSpeed = 5.5f;

    [Tooltip("Kecepatan karakter berputar menghadap arah langkah")]
    public float rotateSpeed = 18f;

    [Header("Deteksi Rintangan")]
    [Tooltip("Layer collider yang dianggap rintangan (tembok, pilar, pintu tertutup)")]
    public LayerMask obstacleLayer = ~0;

    [Header("Komponen")]
    public Animator animator;
    public CharacterController characterController;

    [Header("Status Kontrol")]
    [Tooltip("Terkunci saat transisi, cutscene, atau mati")]
    public bool isControlsLocked = false;

    // Event Turn-Based: Dipanggil setiap kali pemain selesai melangkah ke petak baru
    public static event Action<Vector3> OnStepFinished;

    private bool isMoving = false;
    private static readonly int IsWalkingHash = Animator.StringToHash("isWalking");

    void Start()
    {
        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator != null)
            animator.applyRootMotion = false;

        // Pastikan tag Player terpasang
        if (!CompareTag("Player"))
            tag = "Player";

        // Pas kan posisi awal karakter di tengah petak
        SnapToGrid();
    }

    void Update()
    {
        if (isMoving || isControlsLocked) return;

        Vector2 input = ReadInput();
        if (input != Vector2.zero)
        {
            Vector3 direction = GetGridDirection(input);
            if (direction != Vector3.zero)
            {
                TryMove(direction);
            }
        }
    }

    private Vector2 ReadInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return Vector2.zero;

        // Input discrete (wasPressedThisFrame) agar 1 kali tekan = tepat 1 langkah
        if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame) return Vector2.up;
        if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame) return Vector2.down;
        if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame) return Vector2.left;
        if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame) return Vector2.right;

        return Vector2.zero;
    }

    private Vector3 GetGridDirection(Vector2 input)
    {
        if (input == Vector2.up) return Vector3.forward;
        if (input == Vector2.down) return Vector3.back;
        if (input == Vector2.left) return Vector3.left;
        if (input == Vector2.right) return Vector3.right;
        return Vector3.zero;
    }

    private void TryMove(Vector3 direction)
    {
        Vector3 destination = transform.position + (direction * tileSize);

        // Raycast setinggi pinggang (0.8m) untuk mengecek apakah petak tujuan terhalang
        Vector3 rayStart = transform.position + Vector3.up * 0.8f;
        if (Physics.Raycast(rayStart, direction, out RaycastHit hit, tileSize, obstacleLayer, QueryTriggerInteraction.Ignore))
        {
            // Jangan anggap collider milik diri sendiri sebagai rintangan
            if (hit.transform != transform && !hit.transform.IsChildOf(transform))
            {
                // Putar tubuh menghadap rintangan tanpa melangkah maju
                StartCoroutine(RotateTowards(direction));
                return;
            }
        }

        // 2. CEK LANTAI: Karakter HANYA boleh melangkah jika di petak target ada prefab floor!
        // Jika tidak ada lantai (jurang / void), karakter tidak boleh melangkah ke udara kosong.
        Vector3 downCheck = destination + Vector3.up * 1.5f;
        if (!Physics.Raycast(downCheck, Vector3.down, out RaycastHit floorHit, 4f, obstacleLayer, QueryTriggerInteraction.Ignore))
        {
            // Tidak ada lantai di depan (ujung jurang/void), hanya putar tubuh tanpa melangkah
            StartCoroutine(RotateTowards(direction));
            return;
        }

        // Sesuaikan ketinggian Y agar menempel sempurna di permukaan lantai
        destination.y = floorHit.point.y;

        // Lakukan langkah ke petak target
        StartCoroutine(StepRoutine(destination, direction));
    }

    private IEnumerator StepRoutine(Vector3 destination, Vector3 direction)
    {
        isMoving = true;
        if (animator != null) animator.SetBool(IsWalkingHash, true);

        Quaternion targetRot = Quaternion.LookRotation(direction, Vector3.up);

        // Nonaktifkan CharacterController sebentar saat moving agar tidak menghambat lerp grid
        if (characterController != null) characterController.enabled = false;

        while (Vector3.Distance(transform.position, destination) > 0.02f)
        {
            transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = destination;
        transform.rotation = targetRot;

        if (characterController != null) characterController.enabled = true;

        if (animator != null) animator.SetBool(IsWalkingHash, false);
        isMoving = false;

        // Siarkan sinyal giliran selesai (akan diterima AI Butcher Demon)
        OnStepFinished?.Invoke(transform.position);
    }

    private IEnumerator RotateTowards(Vector3 direction)
    {
        Quaternion targetRot = Quaternion.LookRotation(direction, Vector3.up);
        float elapsed = 0f;
        while (elapsed < 0.12f)
        {
            elapsed += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
            yield return null;
        }
        transform.rotation = targetRot;
    }

    /// <summary>
    /// Menyelaraskan koordinat karakter tepat di tengah petak (kelipatan tileSize).
    /// </summary>
    [ContextMenu("Snap to Grid")]
    public void SnapToGrid()
    {
        float x = Mathf.Round(transform.position.x / tileSize) * tileSize;
        float z = Mathf.Round(transform.position.z / tileSize) * tileSize;
        transform.position = new Vector3(x, transform.position.y, z);
    }

    /// Dipanggil saat respawn atau saat reset level.
    public void Respawn(Vector3 position, Quaternion rotation)
    {
        StopAllCoroutines();
        isMoving = false;

        if (characterController != null) characterController.enabled = false;

        transform.position = position;
        transform.rotation = rotation;
        SnapToGrid();

        if (characterController != null) characterController.enabled = true;

        if (animator != null) animator.SetBool(IsWalkingHash, false);

        Debug.Log($"[GridPlayerMovement] Respawned ke {transform.position}");
    }
}
