using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor Window untuk memperluas dunia game menggunakan prefab milik sendiri.
/// Buka via: Tools > World Builder
/// </summary>
public class WorldBuilderTool : EditorWindow
{
    // ── TAB ──────────────────────────────────────────────────────────────
    private int activeTab = 0;
    private readonly string[] tabLabels = { "🏠 Room", "🧱 Tembok", "🪨 Pilar", "🚪 Pintu", "💡 Lampu" };
    private Vector2 scroll;

    // ════════════════════════════════════════════════════════════════════
    // PREFAB FIELDS (ditampilkan di bagian atas selalu)
    // ════════════════════════════════════════════════════════════════════
    private GameObject floorPrefab;
    private GameObject wallPrefab;
    private GameObject pillarPrefab;
    private GameObject doorPrefab;
    private GameObject lampPrefab;   // opsional: prop lampu (mesh); Point Light tetap ditambah

    // ── ROOM GENERATOR ───────────────────────────────────────────────────
    private int   roomCountX      = 2;
    private int   roomCountZ      = 2;
    private float roomWidth       = 10f;
    private float roomDepth       = 10f;
    private float floorY          = 0f;
    private float floorThickness  = 0.3f;
    private bool  addWalls        = true;
    private float wallHeight      = 3f;
    private float wallThickness   = 0.3f;
    private bool  addPillars      = true;
    private float pillarSize      = 0.5f;
    private float pillarHeight    = 3f;
    private bool  addDoors        = false;
    private float doorWidth       = 1.2f;
    private float doorHeight      = 2.2f;
    private float doorOpenAngle   = 90f;
    private bool  addLights       = true;
    private float lightIntensity  = 3f;
    private Color lightColor      = new Color(1f, 0.9f, 0.7f);
    private float lightRange      = 8f;
    private string roomParentName = "GeneratedWorld";

    // ── TEMBOK MANUAL ────────────────────────────────────────────────────
    private Vector3 wallPosition  = Vector3.zero;
    private Vector3 wallScale     = new Vector3(10f, 3f, 0.3f);
    private int     wallCount     = 1;
    private float   wallSpacing   = 10f;
    private enum    WallAxis { X, Z }
    private WallAxis wallAxis     = WallAxis.X;

    // ── PILAR MANUAL ─────────────────────────────────────────────────────
    private Vector3 pillarStart   = Vector3.zero;
    private float   pillarW       = 0.6f;
    private float   pillarH       = 3f;
    private float   pillarD       = 0.6f;
    private int     pillarRowX    = 3;
    private int     pillarRowZ    = 3;
    private float   pillarSpacX   = 5f;
    private float   pillarSpacZ   = 5f;

    // ── PINTU MANUAL ──────────────────────────────────────────────────────
    private Vector3 doorPosition  = Vector3.zero;
    private float   doorW         = 1.2f;
    private float   doorH         = 2.2f;
    private float   doorD         = 0.15f;
    private float   doorAngle     = 90f;

    // ── LAMPU MANUAL ──────────────────────────────────────────────────────
    private Vector3 lampPos       = new Vector3(0f, 2.5f, 0f);
    private float   lampIntensity = 3f;
    private Color   lampColor     = new Color(1f, 0.9f, 0.7f);
    private float   lampRange     = 8f;
    private int     lampCountX    = 2;
    private int     lampCountZ    = 2;
    private float   lampSpacX     = 5f;
    private float   lampSpacZ     = 5f;

    // ─────────────────────────────────────────────────────────────────────

    [MenuItem("Tools/World Builder")]
    public static void ShowWindow()
    {
        var win = GetWindow<WorldBuilderTool>("World Builder");
        win.minSize = new Vector2(340f, 520f);
    }

    void OnGUI()
    {
        EditorGUILayout.Space(6);
        var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            { fontSize = 14, alignment = TextAnchor.MiddleCenter };
        EditorGUILayout.LabelField("🌍  World Builder  —  Prefab Edition", headerStyle, GUILayout.Height(24));
        EditorGUILayout.Space(4);
        DrawSeparator();

        // ── PREFAB PANEL ────────────────────────────────────────────────
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Prefab Asset Milikmu  (kosong = gunakan Cube primitif)", EditorStyles.miniLabel);

        EditorGUILayout.BeginVertical(GUI.skin.box);
        floorPrefab  = PrefabField("Floor",  floorPrefab);
        wallPrefab   = PrefabField("Tembok", wallPrefab);
        pillarPrefab = PrefabField("Pilar",  pillarPrefab);
        doorPrefab   = PrefabField("Pintu",  doorPrefab);
        lampPrefab   = PrefabField("Lampu (mesh prop, opsional)", lampPrefab);
        EditorGUILayout.EndVertical();

        DrawSeparator();

        // ── TAB BAR ─────────────────────────────────────────────────────
        EditorGUILayout.Space(4);
        activeTab = GUILayout.Toolbar(activeTab, tabLabels, GUILayout.Height(28));
        EditorGUILayout.Space(6);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        switch (activeTab)
        {
            case 0: DrawRoomTab();   break;
            case 1: DrawWallTab();   break;
            case 2: DrawPillarTab(); break;
            case 3: DrawDoorTab();   break;
            case 4: DrawLampTab();   break;
        }
        EditorGUILayout.EndScrollView();
    }

    // ════════════════════════════════════════════════════════════════════
    // TABS
    // ════════════════════════════════════════════════════════════════════

    void DrawRoomTab()
    {
        EditorGUILayout.HelpBox(
            "Generate grid ruangan lengkap sekaligus.\n" +
            "Prefab di atas dipakai untuk tiap elemen — isi dulu agar hasilnya pakai asset kamu.",
            MessageType.Info);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Jumlah Ruangan", EditorStyles.boldLabel);
        roomCountX = EditorGUILayout.IntSlider("Kolom (X)", roomCountX, 1, 8);
        roomCountZ = EditorGUILayout.IntSlider("Baris (Z)",  roomCountZ, 1, 8);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Ukuran per Ruangan", EditorStyles.boldLabel);
        roomWidth      = EditorGUILayout.FloatField("Lebar (X)",         roomWidth);
        roomDepth      = EditorGUILayout.FloatField("Kedalaman (Z)",     roomDepth);
        floorY         = EditorGUILayout.FloatField("Posisi Y Lantai",   floorY);
        if (floorPrefab == null)
            floorThickness = EditorGUILayout.FloatField("Tebal Lantai (Cube)", floorThickness);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Elemen", EditorStyles.boldLabel);

        addWalls = EditorGUILayout.Toggle("Tambah Tembok", addWalls);
        if (addWalls)
        {
            EditorGUI.indentLevel++;
            wallHeight    = EditorGUILayout.FloatField("Tinggi", wallHeight);
            if (wallPrefab == null)
                wallThickness = EditorGUILayout.FloatField("Tebal (Cube)", wallThickness);
            EditorGUI.indentLevel--;
        }

        addPillars = EditorGUILayout.Toggle("Tambah Pilar", addPillars);
        if (addPillars)
        {
            EditorGUI.indentLevel++;
            pillarHeight = EditorGUILayout.FloatField("Tinggi", pillarHeight);
            if (pillarPrefab == null)
                pillarSize = EditorGUILayout.FloatField("Lebar/Dalam (Cube)", pillarSize);
            EditorGUI.indentLevel--;
        }

        addDoors = EditorGUILayout.Toggle("Tambah Pintu", addDoors);
        if (addDoors)
        {
            EditorGUI.indentLevel++;
            doorWidth    = EditorGUILayout.FloatField("Lebar",    doorWidth);
            doorHeight   = EditorGUILayout.FloatField("Tinggi",   doorHeight);
            doorOpenAngle = EditorGUILayout.FloatField("Sudut Buka", doorOpenAngle);
            EditorGUI.indentLevel--;
        }

        addLights = EditorGUILayout.Toggle("Tambah Lampu", addLights);
        if (addLights)
        {
            EditorGUI.indentLevel++;
            lightIntensity = EditorGUILayout.FloatField("Intensitas", lightIntensity);
            lightColor     = EditorGUILayout.ColorField("Warna",      lightColor);
            lightRange     = EditorGUILayout.FloatField("Jangkauan",  lightRange);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(4);
        roomParentName = EditorGUILayout.TextField("Nama Parent Object", roomParentName);

        EditorGUILayout.Space(8);
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button($"⚙  Generate {roomCountX * roomCountZ} Ruangan", GUILayout.Height(36)))
            GenerateRooms();
        GUI.backgroundColor = Color.white;
    }

    void DrawWallTab()
    {
        EditorGUILayout.HelpBox("Tambah satu atau deretan tembok.", MessageType.Info);
        wallPosition = EditorGUILayout.Vector3Field("Posisi Awal", wallPosition);
        if (wallPrefab == null)
            wallScale = EditorGUILayout.Vector3Field("Ukuran Cube (X,Y,Z)", wallScale);
        wallCount   = EditorGUILayout.IntSlider("Jumlah", wallCount, 1, 20);
        if (wallCount > 1)
        {
            EditorGUI.indentLevel++;
            wallSpacing = EditorGUILayout.FloatField("Jarak Antar", wallSpacing);
            wallAxis    = (WallAxis)EditorGUILayout.EnumPopup("Arah Deretan", wallAxis);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space(8);
        GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
        if (GUILayout.Button($"➕  Tambah {wallCount} Tembok", GUILayout.Height(34))) SpawnWalls();
        GUI.backgroundColor = Color.white;
    }

    void DrawPillarTab()
    {
        EditorGUILayout.HelpBox("Tambah grid pilar.", MessageType.Info);
        pillarStart = EditorGUILayout.Vector3Field("Posisi Awal (sudut kiri-bawah)", pillarStart);
        if (pillarPrefab == null)
        {
            pillarW = EditorGUILayout.FloatField("Lebar",     pillarW);
            pillarH = EditorGUILayout.FloatField("Tinggi",    pillarH);
            pillarD = EditorGUILayout.FloatField("Kedalaman", pillarD);
        }
        EditorGUILayout.Space(4);
        pillarRowX = EditorGUILayout.IntSlider("Kolom (X)", pillarRowX, 1, 10);
        pillarRowZ = EditorGUILayout.IntSlider("Baris (Z)",  pillarRowZ, 1, 10);
        pillarSpacX = EditorGUILayout.FloatField("Jarak X", pillarSpacX);
        pillarSpacZ = EditorGUILayout.FloatField("Jarak Z", pillarSpacZ);
        EditorGUILayout.Space(8);
        GUI.backgroundColor = new Color(0.9f, 0.7f, 0.3f);
        if (GUILayout.Button($"➕  Tambah {pillarRowX * pillarRowZ} Pilar", GUILayout.Height(34))) SpawnPillars();
        GUI.backgroundColor = Color.white;
    }

    void DrawDoorTab()
    {
        EditorGUILayout.HelpBox(
            "Tambah pintu interaktif.\nScript DoorInteractable dipasang otomatis.",
            MessageType.Info);
        doorPosition = EditorGUILayout.Vector3Field("Posisi", doorPosition);
        if (doorPrefab == null)
        {
            doorW = EditorGUILayout.FloatField("Lebar",     doorW);
            doorH = EditorGUILayout.FloatField("Tinggi",    doorH);
            doorD = EditorGUILayout.FloatField("Ketebalan", doorD);
        }
        doorAngle = EditorGUILayout.FloatField("Sudut Buka (°)", doorAngle);
        EditorGUILayout.Space(8);
        GUI.backgroundColor = new Color(0.8f, 0.5f, 0.9f);
        if (GUILayout.Button("➕  Tambah Pintu Interaktif", GUILayout.Height(34))) SpawnDoor();
        GUI.backgroundColor = Color.white;
    }

    void DrawLampTab()
    {
        EditorGUILayout.HelpBox("Tambah grid Point Light (+ opsional mesh prop lampu).", MessageType.Info);
        lampPos       = EditorGUILayout.Vector3Field("Posisi Awal", lampPos);
        lampIntensity = EditorGUILayout.FloatField("Intensitas",    lampIntensity);
        lampColor     = EditorGUILayout.ColorField("Warna",         lampColor);
        lampRange     = EditorGUILayout.FloatField("Jangkauan",     lampRange);
        EditorGUILayout.Space(4);
        lampCountX = EditorGUILayout.IntSlider("Kolom (X)", lampCountX, 1, 10);
        lampCountZ = EditorGUILayout.IntSlider("Baris (Z)",  lampCountZ, 1, 10);
        lampSpacX  = EditorGUILayout.FloatField("Jarak X",  lampSpacX);
        lampSpacZ  = EditorGUILayout.FloatField("Jarak Z",  lampSpacZ);
        EditorGUILayout.Space(8);
        GUI.backgroundColor = new Color(1f, 0.9f, 0.3f);
        if (GUILayout.Button($"➕  Tambah {lampCountX * lampCountZ} Lampu", GUILayout.Height(34))) SpawnLights();
        GUI.backgroundColor = Color.white;
    }

    // ════════════════════════════════════════════════════════════════════
    // GENERATOR: ROOM GRID
    // ════════════════════════════════════════════════════════════════════
    void GenerateRooms()
    {
        var existing = GameObject.Find(roomParentName);
        var parent   = existing != null ? existing.transform : new GameObject(roomParentName).transform;
        Undo.RegisterCreatedObjectUndo(parent.gameObject, "Generate Rooms");

        for (int z = 0; z < roomCountZ; z++)
        {
            for (int x = 0; x < roomCountX; x++)
            {
                float ox = x * roomWidth;
                float oz = z * roomDepth;

                var roomGO = new GameObject($"Room_{x}_{z}");
                roomGO.transform.SetParent(parent, false);
                Undo.RegisterCreatedObjectUndo(roomGO, "Generate Room");

                // Floor
                var floorGO = Spawn("Floor", floorPrefab, new Vector3(ox, floorY, oz), roomGO.transform);
                if (floorPrefab == null)
                    floorGO.transform.localScale = new Vector3(roomWidth, floorThickness, roomDepth);

                if (addWalls)   RoomWalls(roomGO.transform,   ox, oz);
                if (addPillars) RoomPillars(roomGO.transform, ox, oz);
                if (addDoors)   RoomDoor(roomGO.transform,    ox, oz);
                if (addLights)  RoomLight(roomGO.transform,   ox, oz);
            }
        }

        Debug.Log($"[WorldBuilder] {roomCountX * roomCountZ} ruangan di-generate di '{roomParentName}'.");
        Selection.activeGameObject = parent.gameObject;
    }

    void RoomWalls(Transform parent, float ox, float oz)
    {
        float wy = floorY + wallHeight * 0.5f + (floorPrefab == null ? floorThickness * 0.5f : 0f);

        // Depan
        var wf = Spawn("Wall_Front", wallPrefab, new Vector3(ox, wy, oz - roomDepth * 0.5f), parent);
        if (wallPrefab == null) wf.transform.localScale = new Vector3(roomWidth, wallHeight, wallThickness);

        // Belakang
        var wb = Spawn("Wall_Back", wallPrefab, new Vector3(ox, wy, oz + roomDepth * 0.5f), parent);
        if (wallPrefab == null) wb.transform.localScale = new Vector3(roomWidth, wallHeight, wallThickness);

        // Kiri
        var wl = Spawn("Wall_Left", wallPrefab, new Vector3(ox - roomWidth * 0.5f, wy, oz), parent);
        if (wallPrefab == null) wl.transform.localScale = new Vector3(wallThickness, wallHeight, roomDepth);

        // Kanan
        var wr = Spawn("Wall_Right", wallPrefab, new Vector3(ox + roomWidth * 0.5f, wy, oz), parent);
        if (wallPrefab == null) wr.transform.localScale = new Vector3(wallThickness, wallHeight, roomDepth);
    }

    void RoomPillars(Transform parent, float ox, float oz)
    {
        float py = floorY + pillarHeight * 0.5f + (floorPrefab == null ? floorThickness * 0.5f : 0f);
        float hw = roomWidth * 0.5f, hd = roomDepth * 0.5f;

        Vector3[] corners = {
            new Vector3(ox - hw, py, oz - hd),
            new Vector3(ox + hw, py, oz - hd),
            new Vector3(ox - hw, py, oz + hd),
            new Vector3(ox + hw, py, oz + hd),
        };
        foreach (var c in corners)
        {
            var p = Spawn("Pillar", pillarPrefab, c, parent);
            if (pillarPrefab == null)
                p.transform.localScale = new Vector3(pillarSize, pillarHeight, pillarSize);
        }
    }

    void RoomDoor(Transform parent, float ox, float oz)
    {
        float dy  = floorY + doorHeight * 0.5f + (floorPrefab == null ? floorThickness * 0.5f : 0f);
        var   pos = new Vector3(ox, dy, oz - roomDepth * 0.5f);
        var   go  = Spawn("Door", doorPrefab, pos, parent);

        if (doorPrefab == null)
            go.transform.localScale = new Vector3(doorWidth, doorHeight, 0.15f);

        GameObjectUtility.SetStaticEditorFlags(go, 0);
        if (go.GetComponent<DoorInteractable>() == null)
        {
            var d = go.AddComponent<DoorInteractable>();
            d.openAngle = doorOpenAngle;
            d.debugLog  = false;
        }
        EditorUtility.SetDirty(go);
    }

    void RoomLight(Transform parent, float ox, float oz)
    {
        float ly = floorY + wallHeight - 0.3f;
        var   go = new GameObject("PointLight");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(ox, ly, oz);

        // Opsional: tempelkan mesh prop lampu
        if (lampPrefab != null)
        {
            var prop = (GameObject)PrefabUtility.InstantiatePrefab(lampPrefab, go.transform);
            prop.name = "LampProp";
        }

        var l = go.AddComponent<Light>();
        l.type      = LightType.Point;
        l.intensity = lightIntensity;
        l.color     = lightColor;
        l.range     = lightRange;
        l.shadows   = LightShadows.Soft;
    }

    // ════════════════════════════════════════════════════════════════════
    // SPAWN MANUAL
    // ════════════════════════════════════════════════════════════════════
    void SpawnWalls()
    {
        var parent = GetOrCreateParent("Walls");
        for (int i = 0; i < wallCount; i++)
        {
            Vector3 offset = wallAxis == WallAxis.X
                ? new Vector3(i * wallSpacing, 0, 0)
                : new Vector3(0, 0, i * wallSpacing);

            var go = Spawn($"Wall_{i}", wallPrefab, wallPosition + offset, parent);
            if (wallPrefab == null) go.transform.localScale = wallScale;
            Undo.RegisterCreatedObjectUndo(go, "Add Wall");
        }
        Debug.Log($"[WorldBuilder] {wallCount} tembok ditambahkan.");
    }

    void SpawnPillars()
    {
        var parent = GetOrCreateParent("Pillars");
        int total  = 0;
        for (int z = 0; z < pillarRowZ; z++)
        for (int x = 0; x < pillarRowX; x++)
        {
            var pos = pillarStart + new Vector3(x * pillarSpacX, 0, z * pillarSpacZ);
            var go  = Spawn($"Pillar_{x}_{z}", pillarPrefab, pos, parent);
            if (pillarPrefab == null) go.transform.localScale = new Vector3(pillarW, pillarH, pillarD);
            Undo.RegisterCreatedObjectUndo(go, "Add Pillar");
            total++;
        }
        Debug.Log($"[WorldBuilder] {total} pilar ditambahkan.");
    }

    void SpawnDoor()
    {
        var parent = GetOrCreateParent("Doors");
        var go     = Spawn("Door", doorPrefab, doorPosition, parent);

        if (doorPrefab == null) go.transform.localScale = new Vector3(doorW, doorH, doorD);

        GameObjectUtility.SetStaticEditorFlags(go, 0);
        if (go.GetComponent<DoorInteractable>() == null)
        {
            var d = go.AddComponent<DoorInteractable>();
            d.openAngle = doorAngle;
        }

        Undo.RegisterCreatedObjectUndo(go, "Add Door");
        Selection.activeGameObject = go;
        EditorUtility.SetDirty(go);
        Debug.Log($"[WorldBuilder] Pintu interaktif di {doorPosition}.");
    }

    void SpawnLights()
    {
        var parent = GetOrCreateParent("Lights");
        int total  = 0;
        for (int z = 0; z < lampCountZ; z++)
        for (int x = 0; x < lampCountX; x++)
        {
            var pos = lampPos + new Vector3(x * lampSpacX, 0, z * lampSpacZ);
            var go  = new GameObject($"Light_{x}_{z}");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            if (lampPrefab != null)
            {
                var prop = (GameObject)PrefabUtility.InstantiatePrefab(lampPrefab, go.transform);
                prop.name = "LampProp";
            }

            var l = go.AddComponent<Light>();
            l.type      = LightType.Point;
            l.intensity = lampIntensity;
            l.color     = lampColor;
            l.range     = lampRange;
            l.shadows   = LightShadows.Soft;

            Undo.RegisterCreatedObjectUndo(go, "Add Light");
            total++;
        }
        Debug.Log($"[WorldBuilder] {total} lampu ditambahkan.");
    }

    // ════════════════════════════════════════════════════════════════════
    // HELPER
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Jika prefab disediakan → InstantiatePrefab (tetap terhubung ke prefab).
    /// Jika kosong           → buat Cube primitif sebagai fallback.
    /// </summary>
    private GameObject Spawn(string objName, GameObject prefab, Vector3 position, Transform parent)
    {
        GameObject go;
        if (prefab != null)
        {
            go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(parent, false);
        }

        go.name                  = objName;
        go.transform.position    = position;
        return go;
    }

    private Transform GetOrCreateParent(string name)
    {
        var existing = GameObject.Find(name);
        if (existing != null) return existing.transform;
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name} Parent");
        return go.transform;
    }

    private GameObject PrefabField(string label, GameObject current)
        => (GameObject)EditorGUILayout.ObjectField(label, current, typeof(GameObject), false);

    private void DrawSeparator()
    {
        var rect = EditorGUILayout.GetControlRect(false, 1f);
        EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 0.5f));
        EditorGUILayout.Space(2);
    }
}
