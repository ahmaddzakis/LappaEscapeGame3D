using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Cinemachine;

/// <summary>
/// Editor tool untuk setup scene Skeleton GO (game grid berbasis giliran).
/// Akses via menu: Tools/Skeleton GO/...
/// </summary>
public static class SkeletonGoSetup
{
    private const string CharacterName = "UD_demo_character";

    // ─────────────────────────────────────────────────────────────
    //  MENU: Tools/Skeleton GO/Setup Diorama Mode (1-Click)
    // ─────────────────────────────────────────────────────────────
    [MenuItem("Tools/Skeleton GO/Setup Diorama Mode (1-Click)")]
    public static void SetupDioramaMode()
    {
        // 1. Cari objek karakter player
        GameObject charObj = GameObject.Find(CharacterName);
        if (charObj == null)
        {
            var tagged = GameObject.FindWithTag("Player");
            if (tagged != null) charObj = tagged;
        }

        if (charObj == null)
        {
            EditorUtility.DisplayDialog(
                "Setup Gagal",
                "Karakter '" + CharacterName + "' tidak ditemukan di scene!\n\nPastikan player sudah ada di Hierarchy.",
                "OK");
            return;
        }

        Undo.RecordObject(charObj, "Setup Grid Movement");

        // 2. Pasang / aktifkan GridPlayerMovement
        var gridMover = charObj.GetComponent<GridPlayerMovement>();
        if (gridMover == null)
            gridMover = Undo.AddComponent<GridPlayerMovement>(charObj);

        gridMover.enabled     = true;
        gridMover.tileSize    = 2f;
        gridMover.moveSpeed   = 5.5f;
        gridMover.rotateSpeed = 18f;
        gridMover.animator    = charObj.GetComponent<Animator>();
        gridMover.characterController = charObj.GetComponent<CharacterController>();

        // Pastikan tag Player terpasang
        if (!charObj.CompareTag("Player")) charObj.tag = "Player";

        // Snap karakter ke koordinat grid terdekat
        gridMover.SnapToGrid();
        EditorUtility.SetDirty(charObj);

        // 3. Setup Main Camera sebagai DioramaCamera
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            var camObj = GameObject.Find("Main Camera");
            if (camObj != null) mainCam = camObj.GetComponent<Camera>();
        }

        if (mainCam != null)
        {
            Undo.RecordObject(mainCam.gameObject, "Setup Diorama Camera");

            // Matikan CinemachineBrain agar kamera tidak dikontrol Cinemachine
            var brain = mainCam.GetComponent<CinemachineBrain>();
            if (brain != null) brain.enabled = false;

            var diorama = mainCam.GetComponent<DioramaCamera>();
            if (diorama == null)
                diorama = Undo.AddComponent<DioramaCamera>(mainCam.gameObject);

            diorama.enabled             = true;
            diorama.target              = charObj.transform;
            diorama.distance            = 11.5f;
            diorama.pitchAngle          = 46f;
            diorama.yawAngle            = 45f;
            diorama.followSmoothTime    = 0.16f;
            diorama.enableMouseParallax = true;
            diorama.ApplyTransformImmediate();

            EditorUtility.SetDirty(mainCam.gameObject);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        Debug.Log("<color=green>[SkeletonGoSetup] Berhasil beralih ke mode Diorama Grid (Skeleton GO)!</color>");
        EditorUtility.DisplayDialog(
            "Setup Berhasil!",
            "Mode Diorama Grid aktif!\n" +
            "- GridPlayerMovement aktif di " + charObj.name + "\n" +
            "- DioramaCamera aktif di Main Camera\n\n" +
            "Tekan Play untuk mencoba!",
            "Gas Main!");
    }

    // ─────────────────────────────────────────────────────────────
    //  MENU: Tools/Skeleton GO/Fix & Ensure Floor Colliders
    // ─────────────────────────────────────────────────────────────
    [MenuItem("Tools/Skeleton GO/Fix & Ensure Floor Colliders")]
    public static void EnsureFloorColliders()
    {
        var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);

        int added = 0;
        foreach (var t in allTransforms)
        {
            if (!t.name.ToLower().Contains("floor")) continue;

            var col = t.GetComponent<Collider>();
            if (col == null)
            {
                var box = Undo.AddComponent<BoxCollider>(t.gameObject);
                box.size   = new Vector3(2f, 0.4f, 2f);
                box.center = Vector3.zero;
                EditorUtility.SetDirty(t.gameObject);
                added++;
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        if (added > 0)
        {
            Debug.Log($"<color=green>[SkeletonGoSetup] Berhasil menambahkan BoxCollider pada {added} prefab Floor!</color>");
            EditorUtility.DisplayDialog(
                "Floor Colliders Siap",
                $"Berhasil memasang BoxCollider pada {added} objek Floor!",
                "OK");
        }
        else
        {
            Debug.Log("[SkeletonGoSetup] Semua prefab Floor sudah memiliki Collider.");
            EditorUtility.DisplayDialog(
                "Sudah Aman",
                "Semua lantai Floor di scene sudah memiliki Collider lengkap!",
                "OK");
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  MENU: Tools/Skeleton GO/Snap Player to Grid
    // ─────────────────────────────────────────────────────────────
    [MenuItem("Tools/Skeleton GO/Snap Player to Grid")]
    public static void SnapPlayerToGrid()
    {
        var gridMover = Object.FindAnyObjectByType<GridPlayerMovement>();
        if (gridMover == null)
        {
            EditorUtility.DisplayDialog(
                "Tidak Ditemukan",
                "Tidak ada GridPlayerMovement di scene.\nJalankan Setup Diorama Mode terlebih dahulu.",
                "OK");
            return;
        }

        Undo.RecordObject(gridMover.transform, "Snap Player to Grid");
        gridMover.SnapToGrid();
        EditorUtility.SetDirty(gridMover.gameObject);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"[SkeletonGoSetup] Player di-snap ke posisi grid: {gridMover.transform.position}");
        EditorUtility.DisplayDialog("Snap Selesai", $"Player sudah di-snap ke: {gridMover.transform.position}", "OK");
    }

    // ─────────────────────────────────────────────────────────────
    //  MENU: Tools/Skeleton GO/Fix Dungeon Doors Static
    // ─────────────────────────────────────────────────────────────
    [MenuItem("Tools/Skeleton GO/Fix Dungeon Doors Static (Uncheck Static)")]
    public static void FixDoorsStatic()
    {
        var doors = Object.FindObjectsByType<DoorInteractable>(FindObjectsInactive.Include);
        int fixedCount = 0;

        foreach (var door in doors)
        {
            foreach (var t in door.GetComponentsInChildren<Transform>(true))
            {
                var flags = GameObjectUtility.GetStaticEditorFlags(t.gameObject);
                if ((flags & StaticEditorFlags.BatchingStatic) != 0)
                {
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags & ~StaticEditorFlags.BatchingStatic);
                    EditorUtility.SetDirty(t.gameObject);
                    fixedCount++;
                }
            }
        }

        if (fixedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log($"<color=green>[SkeletonGoSetup] Batching Static dinonaktifkan pada {fixedCount} objek pintu/anaknya.</color>");
            EditorUtility.DisplayDialog("Berhasil", $"Batching Static dinonaktifkan pada {fixedCount} objek pintu.", "OK");
        }
        else
        {
            Debug.Log("[SkeletonGoSetup] Semua pintu sudah tidak menggunakan Batching Static.");
            EditorUtility.DisplayDialog("Aman", "Semua pintu sudah siap bergerak (tidak Static).", "OK");
        }
    }
}
