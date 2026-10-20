using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using LappaEscape.Core;
using LappaEscape.Player;
using LappaEscape.CameraControl;
using LappaEscape.Environment;
using LappaEscape.Enemy;
using LappaEscape.UI;

namespace LappaEscape.EditorTools
{
    /// <summary>
    /// Generator otomatis prefab dan level Stage 1 Lappa Escape.
    /// Membangun seluruh prefab di Assets/Prefabs/LappaEscape/ dan merangkai scene LappaEscape.
    /// Menu: Tools > Lappa Escape > Build Complete Stage 1 (1-Click)
    /// </summary>
    public static class LappaEscapeLevelBuilder
    {
        private const string PrefabDir = "Assets/Prefabs/LappaEscape";
        private const float TileSize = 2.0f;

        [MenuItem("Tools/Lappa Escape/Build Complete Stage 1 (1-Click)")]
        public static void BuildCompleteStage1()
        {
            EnsurePrefabDirectory();

            // 1. Buat Seluruh Prefab Placeholder di Assets/Prefabs/LappaEscape
            BuildAllPrefabs(out var prefabDict);

            // 2. Bersihkan isi lama jika sudah ada level di scene aktif
            var oldRoot = GameObject.Find("--- LEVEL 1 STAGE ---");
            if (oldRoot != null) Undo.DestroyObjectImmediate(oldRoot);

            var oldManagers = GameObject.Find("--- MANAGERS ---");
            if (oldManagers != null) Undo.DestroyObjectImmediate(oldManagers);

            var oldLava = GameObject.Find("LavaSea");
            if (oldLava != null) Undo.DestroyObjectImmediate(oldLava);

            // 3. Setup Suasana Pencahayaan Inferno
            SetupLightingAndEnvironment();

            // 4. Setup Managers
            SetupManagers(out var gridManager, out var levelManager);

            // 5. Rangkai Seluruh Level Design di Scene
            Transform levelRoot = new GameObject("--- LEVEL 1 STAGE ---").transform;
            Undo.RegisterCreatedObjectUndo(levelRoot.gameObject, "Create Level Root");

            BuildLevelGrid(levelRoot, prefabDict, gridManager, levelManager, out var startNode, out var finishNode);

            // 6. Spawn Player di Start Node
            SpawnPlayer(levelRoot, prefabDict, startNode);

            // 7. Setup Main Camera
            SetupCamera();

            // Tandai scene kotor dan simpan
            var activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            if (!string.IsNullOrEmpty(activeScene.path))
            {
                EditorSceneManager.SaveScene(activeScene);
            }
            AssetDatabase.SaveAssets();

            Debug.Log("<color=green>[LappaEscape] BERHASIL! Seluruh Prefab dan Level Stage 1 selesai dirangkai 100% presisi!</color>");
            EditorUtility.DisplayDialog("Stage 1 Siap!",
                "Seluruh 8 Issue telah diimplementasikan!\n\n" +
                "1. Prefab tersimpan di: Assets/Prefabs/LappaEscape/\n" +
                "2. Level Design Stage 1 tersusun di scene: " + activeScene.name + "\n" +
                "3. TurnManager, GridManager, Demon AI, dan Player Controller siap!\n\n" +
                "Tekan tombol Play untuk mulai menguji permainan!",
                "Gas Main!");
        }

        private static void EnsurePrefabDirectory()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder(PrefabDir))
                AssetDatabase.CreateFolder("Assets/Prefabs", "LappaEscape");
        }

        private static void BuildAllPrefabs(out Dictionary<string, GameObject> prefabs)
        {
            prefabs = new Dictionary<string, GameObject>();

            // Material Palet URP
            Material matNormal = CreateLitMaterial("Mat_Node_Normal", new Color(0.25f, 0.25f, 0.28f));
            Material matStart = CreateLitMaterial("Mat_Node_Start", Color.yellow, new Color(0.8f, 0.8f, 0.1f));
            Material matFinish = CreateLitMaterial("Mat_Node_Finish", Color.cyan, new Color(0.2f, 0.8f, 1.0f));
            Material matCheckpoint = CreateLitMaterial("Mat_Node_Checkpoint", Color.white, new Color(0.5f, 0.5f, 0.5f));
            Material matCracked = CreateLitMaterial("Mat_Node_Cracked", new Color(0.6f, 0.15f, 0.6f), new Color(1.0f, 0.35f, 0.1f));
            Material matBridgeRed = CreateLitMaterial("Mat_Bridge_Red", new Color(0.9f, 0.15f, 0.15f), new Color(0.7f, 0.1f, 0.1f));
            Material matBridgeDarkRed = CreateLitMaterial("Mat_Bridge_DarkRed", new Color(0.55f, 0.08f, 0.08f));
            Material matBridgePurple = CreateLitMaterial("Mat_Bridge_Purple", new Color(0.35f, 0.12f, 0.35f));

            // 1. Node Normal Prefab
            prefabs["Node_Normal"] = SaveAsPrefab("Node_Normal", CreateBaseNodeMesh(NodeType.Normal, matNormal));

            // 2. Start Node Prefab
            var startObj = CreateBaseNodeMesh(NodeType.Start, matStart);
            var markerStart = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            markerStart.transform.SetParent(startObj.transform, false);
            markerStart.transform.localPosition = new Vector3(0, 0.2f, 0);
            markerStart.transform.localScale = new Vector3(0.6f, 0.05f, 0.6f);
            markerStart.GetComponent<Renderer>().sharedMaterial = matStart;
            prefabs["Node_Start"] = SaveAsPrefab("Node_Start", startObj);

            // 3. Finish Node Prefab (dengan pilar portal kemenangan)
            var finishObj = CreateBaseNodeMesh(NodeType.Finish, matFinish);
            var beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beam.name = "VictoryBeam";
            beam.transform.SetParent(finishObj.transform, false);
            beam.transform.localPosition = new Vector3(0, 2f, 0);
            beam.transform.localScale = new Vector3(0.5f, 2f, 0.5f);
            beam.GetComponent<Renderer>().sharedMaterial = matFinish;
            prefabs["Node_Finish"] = SaveAsPrefab("Node_Finish", finishObj);

            // 4. Checkpoint Node Prefab
            var cpObj = CreateBaseNodeMesh(NodeType.Checkpoint, matCheckpoint);
            var cpComp = cpObj.AddComponent<CheckpointNode>();
            var lightObj = new GameObject("CheckpointLight");
            lightObj.transform.SetParent(cpObj.transform, false);
            lightObj.transform.localPosition = new Vector3(0, 0.8f, 0);
            var lgt = lightObj.AddComponent<Light>();
            lgt.color = Color.white;
            lgt.intensity = 1.5f;
            lgt.range = 3.5f;
            cpComp.checkpointLight = lgt;
            prefabs["Node_Checkpoint"] = SaveAsPrefab("Node_Checkpoint", cpObj);

            // 5. Cracked Lava Node Prefab
            var crackedObj = CreateBaseNodeMesh(NodeType.Cracked, matCracked);
            crackedObj.AddComponent<CrackedLavaTile>();
            prefabs["Node_CrackedLava"] = SaveAsPrefab("Node_CrackedLava", crackedObj);

            // 6. Bridge Prefabs (Red, DarkRed, Purple)
            prefabs["Bridge_Red"] = SaveAsPrefab("Bridge_Red", CreateBridgeObject(BridgeColor.Red, matBridgeRed));
            prefabs["Bridge_DarkRed"] = SaveAsPrefab("Bridge_DarkRed", CreateBridgeObject(BridgeColor.DarkRed, matBridgeDarkRed));
            prefabs["Bridge_Purple"] = SaveAsPrefab("Bridge_Purple", CreateBridgeObject(BridgeColor.Purple, matBridgePurple));

            // 7. Lever Prefabs
            prefabs["Lever_Red"] = SaveAsPrefab("Lever_Red", CreateLeverObject(LeverType.ColorBridgeToggle, BridgeColor.Red, Color.red));
            prefabs["Lever_WhiteSliding"] = SaveAsPrefab("Lever_WhiteSliding", CreateLeverObject(LeverType.SlidingBridgeToggle, BridgeColor.Red, Color.white));
            prefabs["Lever_DarkRed"] = SaveAsPrefab("Lever_DarkRed", CreateLeverObject(LeverType.ColorBridgeToggle, BridgeColor.DarkRed, new Color(0.6f, 0.1f, 0.1f)));
            prefabs["Lever_Purple"] = SaveAsPrefab("Lever_Purple", CreateLeverObject(LeverType.ColorBridgeToggle, BridgeColor.Purple, new Color(0.5f, 0.1f, 0.5f)));
        }

        private static GameObject CreateBaseNodeMesh(NodeType type, Material mat)
        {
            var nodeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nodeObj.name = $"Node_{type}";
            nodeObj.transform.localScale = new Vector3(TileSize * 0.92f, 0.35f, TileSize * 0.92f);

            var r = nodeObj.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;

            var node = nodeObj.AddComponent<GridNode>();
            node.nodeType = type;
            node.isWalkable = true;

            return nodeObj;
        }

        private static GameObject CreateBridgeObject(BridgeColor color, Material mat)
        {
            var bridgeObj = CreateBaseNodeMesh(NodeType.Bridge, mat);
            bridgeObj.name = $"Bridge_{color}";

            var bc = bridgeObj.AddComponent<BridgeController>();
            bc.bridgeColor = color;
            bc.loweredY = -2.5f;
            bc.raisedY = 0f;

            var node = bridgeObj.GetComponent<GridNode>();
            node.isWalkable = false;

            return bridgeObj;
        }

        private static GameObject CreateLeverObject(LeverType type, BridgeColor color, Color handleColor)
        {
            var leverObj = new GameObject($"Lever_{type}_{color}");

            // Pedestal base
            var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.name = "Pedestal";
            baseObj.transform.SetParent(leverObj.transform, false);
            baseObj.transform.localScale = new Vector3(0.4f, 0.1f, 0.4f);
            baseObj.GetComponent<Renderer>().sharedMaterial = CreateLitMaterial("Mat_LeverBase", Color.gray);

            // Handle
            var handleObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handleObj.name = "LeverHandle";
            handleObj.transform.SetParent(leverObj.transform, false);
            handleObj.transform.localPosition = new Vector3(0, 0.35f, 0);
            handleObj.transform.localScale = new Vector3(0.12f, 0.35f, 0.12f);
            handleObj.transform.localRotation = Quaternion.Euler(0, 0, 25f);
            handleObj.GetComponent<Renderer>().sharedMaterial = CreateLitMaterial($"Mat_Handle_{color}", handleColor, handleColor * 0.5f);

            var lever = leverObj.AddComponent<LeverInteractable>();
            lever.leverType = type;
            lever.targetColor = color;
            lever.leverHandle = handleObj.transform;

            return leverObj;
        }

        private static GameObject SaveAsPrefab(string name, GameObject tempObj)
        {
            string path = $"{PrefabDir}/{name}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(tempObj, path);
            Object.DestroyImmediate(tempObj);
            return prefab;
        }

        private static void SetupLightingAndEnvironment()
        {
            var light = Object.FindAnyObjectByType<Light>();
            if (light != null)
            {
                light.color = new Color(1.0f, 0.88f, 0.75f);
                light.intensity = 1.25f;
                light.transform.rotation = Quaternion.Euler(50f, -40f, 0f);
            }

            RenderSettings.ambientLight = new Color(0.28f, 0.14f, 0.1f);

            var lava = GameObject.CreatePrimitive(PrimitiveType.Plane);
            lava.name = "LavaSea";
            lava.transform.position = new Vector3(-10f, -3.5f, 10f);
            lava.transform.localScale = new Vector3(10f, 1f, 10f);

            var mat = CreateLitMaterial("Mat_LavaSea", new Color(0.95f, 0.22f, 0.02f), new Color(1.4f, 0.35f, 0.02f));
            lava.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void SetupManagers(out GridManager gm, out LevelManager lm)
        {
            var managersObj = new GameObject("--- MANAGERS ---");
            managersObj.AddComponent<TurnManager>();
            gm = managersObj.AddComponent<GridManager>();
            lm = managersObj.AddComponent<LevelManager>();
            managersObj.AddComponent<GameUIManager>();
        }

        private static void BuildLevelGrid(Transform root, Dictionary<string, GameObject> prefabs,
                                           GridManager gm, LevelManager lm,
                                           out GridNode startNode, out GridNode finishNode)
        {
            Dictionary<Vector2Int, GridNode> nodes = new Dictionary<Vector2Int, GridNode>();

            // -------------------------------------------------------------
            // SEGMEN 1: Bottom-Right
            // -------------------------------------------------------------
            // Start Node (0, 0)
            startNode = InstantiateNode(prefabs["Node_Start"], root, new Vector2Int(0, 0));
            nodes[new Vector2Int(0, 0)] = startNode;

            nodes[new Vector2Int(0, 1)] = InstantiateNode(prefabs["Node_Normal"], root, new Vector2Int(0, 1));
            nodes[new Vector2Int(0, 2)] = InstantiateNode(prefabs["Node_Normal"], root, new Vector2Int(0, 2));

            // Belok ke kiri-atas (-X): (-1, 2)
            nodes[new Vector2Int(-1, 2)] = InstantiateNode(prefabs["Node_Normal"], root, new Vector2Int(-1, 2));

            // 3 Jembatan Merah: (-2, 2), (-3, 2), (-4, 2)
            List<BridgeController> redBridges = new List<BridgeController>();
            for (int i = -2; i >= -4; i--)
            {
                var bNode = InstantiateNode(prefabs["Bridge_Red"], root, new Vector2Int(i, 2));
                var bc = bNode.GetComponent<BridgeController>();
                bc.isRaisedInitially = false;
                redBridges.Add(bc);
                nodes[new Vector2Int(i, 2)] = bNode;
            }

            // Persimpangan: (-5, 2)
            nodes[new Vector2Int(-5, 2)] = InstantiateNode(prefabs["Node_Normal"], root, new Vector2Int(-5, 2));

            // Cabang ke Tuas Merah: (-5, 1), (-5, 0), (-6, 0)
            nodes[new Vector2Int(-5, 1)] = InstantiateNode(prefabs["Node_Normal"], root, new Vector2Int(-5, 1));
            nodes[new Vector2Int(-5, 0)] = InstantiateNode(prefabs["Node_Normal"], root, new Vector2Int(-5, 0));
            var redLeverNode = InstantiateNode(prefabs["Node_Normal"], root, new Vector2Int(-6, 0));
            nodes[new Vector2Int(-6, 0)] = redLeverNode;

            // Pasang Tuas Merah di (-6, 0)
            var redLeverObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabs["Lever_Red"], redLeverNode.transform);
            redLeverObj.transform.localPosition = new Vector3(0, 0.25f, 0);
            var rLever = redLeverObj.GetComponent<LeverInteractable>();
            rLever.linkedBridges.AddRange(redBridges);

            // Jalur lurus lanjut: (-6, 2)
            nodes[new Vector2Int(-6, 2)] = InstantiateNode(prefabs["Node_Normal"], root, new Vector2Int(-6, 2));

            // Jembatan Merah #2 dengan Demon di atasnya: (-7, 2)
            var demonBridgeNode = InstantiateNode(prefabs["Bridge_Red"], root, new Vector2Int(-7, 2));
            var bcDemon = demonBridgeNode.GetComponent<BridgeController>();
            bcDemon.isRaisedInitially = true; // Terbuka agar demon bisa berdiri
            nodes[new Vector2Int(-7, 2)] = demonBridgeNode;

            // Spawn Demon #1 di (-7, 2) menghadap kanan (+X) (Zone face-to-face ke arah pemain datang)
            SpawnDemon(demonBridgeNode, Vector3.right);

            // Checkpoint Node: (-8, 2)
            var cpNode = InstantiateNode(prefabs["Node_Checkpoint"], root, new Vector2Int(-8, 2));
            nodes[new Vector2Int(-8, 2)] = cpNode;

            // Cracked Lava Node: (-9, 2)
            var crackedNode = InstantiateNode(prefabs["Node_CrackedLava"], root, new Vector2Int(-9, 2));
            nodes[new Vector2Int(-9, 2)] = crackedNode;

            // Cabang Tuas Putih Geser: (-9, 1)
            var whiteLeverNode = InstantiateNode(prefabs["Node_Normal"], root, new Vector2Int(-9, 1));
            nodes[new Vector2Int(-9, 1)] = whiteLeverNode;

            // Node jembatan geser: (-10, 1) dan (-10, 2)
            var slideNodeA = InstantiateNode(prefabs["Bridge_Red"], root, new Vector2Int(-10, 1));
            slideNodeA.nodeType = NodeType.SlidingBridge;
            var slideNodeB = InstantiateNode(prefabs["Bridge_Red"], root, new Vector2Int(-10, 2));
            slideNodeB.nodeType = NodeType.SlidingBridge;
            nodes[new Vector2Int(-10, 1)] = slideNodeA;
            nodes[new Vector2Int(-10, 2)] = slideNodeB;

            // Buat Sliding Bridge Slab
            var slideSlab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slideSlab.name = "SlidingBridge_Slab";
            slideSlab.transform.position = slideNodeA.WorldPosition;
            slideSlab.transform.localScale = new Vector3(TileSize * 0.95f, 0.4f, TileSize * 0.95f);
            slideSlab.GetComponent<Renderer>().sharedMaterial = CreateLitMaterial("Mat_SlideSlab", Color.white, Color.white * 0.3f);

            var sb = slideSlab.AddComponent<SlidingBridge>();
            sb.nodeA = slideNodeA;
            sb.nodeB = slideNodeB;
            sb.positionA = slideNodeA.transform;
            sb.positionB = slideNodeB.transform;

            var whiteLeverObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabs["Lever_WhiteSliding"], whiteLeverNode.transform);
            whiteLeverObj.transform.localPosition = new Vector3(0, 0.25f, 0);
            whiteLeverObj.GetComponent<LeverInteractable>().linkedSlidingBridge = sb;

            // -------------------------------------------------------------
            // SEGMEN 2: Top-Right
            // -------------------------------------------------------------
            // Koridor tangga 5 node: (-10, 3) s/d (-10, 7)
            for (int j = 3; j <= 7; j++)
            {
                var c = new Vector2Int(-10, j);
                nodes[c] = InstantiateNode(prefabs["Node_Normal"], root, c);
            }

            // Junction area
            nodes[new Vector2Int(-10, 8)] = InstantiateNode(prefabs["Node_Normal"], root, new Vector2Int(-10, 8));
            nodes[new Vector2Int(-11, 8)] = InstantiateNode(prefabs["Node_Normal"], root, new Vector2Int(-11, 8));
            nodes[new Vector2Int(-9, 8)] = InstantiateNode(prefabs["Node_Normal"], root, new Vector2Int(-9, 8));

            // Demon #2 di (-11, 8) menghadap +Z
            SpawnDemon(nodes[new Vector2Int(-11, 8)], Vector3.forward);

            // Jembatan DarkRed di (-10, 9)
            var darkRedBridgeNode = InstantiateNode(prefabs["Bridge_DarkRed"], root, new Vector2Int(-10, 9));
            nodes[new Vector2Int(-10, 9)] = darkRedBridgeNode;

            // Tuas DarkRed di (-11, 9)
            var darkRedLeverNode = InstantiateNode(prefabs["Node_Normal"], root, new Vector2Int(-11, 9));
            nodes[new Vector2Int(-11, 9)] = darkRedLeverNode;
            var drLeverObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabs["Lever_DarkRed"], darkRedLeverNode.transform);
            drLeverObj.transform.localPosition = new Vector3(0, 0.25f, 0);
            drLeverObj.GetComponent<LeverInteractable>().linkedBridges.Add(darkRedBridgeNode.GetComponent<BridgeController>());

            // Demon #3 (Patroller) di (-9, 9)
            var demon3Node = InstantiateNode(prefabs["Node_Normal"], root, new Vector2Int(-9, 9));
            nodes[new Vector2Int(-9, 9)] = demon3Node;
            var d3 = SpawnDemon(demon3Node, Vector3.left);
            d3.demonType = DemonType.Patroller;
            d3.patrolNodes.Add(demon3Node);
            d3.patrolNodes.Add(nodes[new Vector2Int(-9, 8)]);

            // Jembatan Ungu di (-10, 10)
            var purpleBridgeNode = InstantiateNode(prefabs["Bridge_Purple"], root, new Vector2Int(-10, 10));
            nodes[new Vector2Int(-10, 10)] = purpleBridgeNode;

            // Tuas Ungu di (-9, 10)
            var purpleLeverNode = InstantiateNode(prefabs["Node_Normal"], root, new Vector2Int(-9, 10));
            nodes[new Vector2Int(-9, 10)] = purpleLeverNode;
            var pLeverObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabs["Lever_Purple"], purpleLeverNode.transform);
            pLeverObj.transform.localPosition = new Vector3(0, 0.25f, 0);
            pLeverObj.GetComponent<LeverInteractable>().linkedBridges.Add(purpleBridgeNode.GetComponent<BridgeController>());

            // Finish Node: (-10, 11) (Cyan)
            finishNode = InstantiateNode(prefabs["Node_Finish"], root, new Vector2Int(-10, 11));
            nodes[new Vector2Int(-10, 11)] = finishNode;

            // Hubungkan seluruh koneksi tetangga
            ConnectAllNeighbors(nodes);

            // Blocked Path (garis biru di denah antara (-10, 8) dan (-10, 9))
            if (nodes.TryGetValue(new Vector2Int(-10, 8), out var nA) && nodes.TryGetValue(new Vector2Int(-10, 9), out var nB))
            {
                nA.blockedNeighbors.Add(nB);
                nB.blockedNeighbors.Add(nA);
            }

            gm.ScanAllNodes();
        }

        private static GridNode InstantiateNode(GameObject prefab, Transform parent, Vector2Int coord)
        {
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            obj.transform.position = new Vector3(coord.x * TileSize, 0f, coord.y * TileSize);
            var node = obj.GetComponent<GridNode>();
            node.gridCoordinate = coord;
            return node;
        }

        private static DemonAI SpawnDemon(GridNode node, Vector3 facingDir)
        {
            GameObject demonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Conquerror/Minimalist_Demon/Demon.prefab");
            GameObject demonObj;

            if (demonPrefab != null)
            {
                demonObj = (GameObject)PrefabUtility.InstantiatePrefab(demonPrefab);
            }
            else
            {
                demonObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                demonObj.name = "Placeholder_Demon";
                demonObj.GetComponent<Renderer>().sharedMaterial = CreateLitMaterial("Mat_Demon", new Color(0.8f, 0.1f, 0.1f), Color.red * 0.4f);
            }

            demonObj.transform.position = node.WorldPosition + Vector3.up * 0.35f;
            demonObj.transform.rotation = Quaternion.LookRotation(facingDir, Vector3.up);

            // Tambahkan Indikator Mata / Arah Serang
            var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = "Eye_FacingIndicator";
            eye.transform.SetParent(demonObj.transform, false);
            eye.transform.localPosition = new Vector3(0, 1.2f, 0.5f);
            eye.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            eye.GetComponent<Renderer>().sharedMaterial = CreateLitMaterial("Mat_Eye", Color.red, Color.red * 2.5f);

            var ai = demonObj.AddComponent<DemonAI>();
            ai.facingDirection = facingDir;
            ai.currentNode = node;
            ai.facingIndicator = eye;
            node.currentOccupant = demonObj;

            return ai;
        }

        private static void SpawnPlayer(Transform root, Dictionary<string, GameObject> prefabs, GridNode startNode)
        {
            GameObject femalePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Shokubutsu Studio/Low Poly Female Free/URP/Prefabs/Flat Shaded/Female 1 Flat.prefab");
            GameObject playerObj;

            if (femalePrefab != null)
            {
                playerObj = (GameObject)PrefabUtility.InstantiatePrefab(femalePrefab);
            }
            else
            {
                playerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                playerObj.name = "Lappa_Player";
                playerObj.GetComponent<Renderer>().sharedMaterial = CreateLitMaterial("Mat_Player", Color.blue, Color.cyan * 0.3f);
            }

            playerObj.name = "Lappa_Protagonist";
            playerObj.tag = "Player";
            playerObj.transform.position = startNode.WorldPosition + Vector3.up * 0.2f;

            var cc = playerObj.GetComponent<CharacterController>();
            if (cc == null) cc = playerObj.AddComponent<CharacterController>();
            cc.height = 1.6f;
            cc.radius = 0.35f;

            var controller = playerObj.AddComponent<LappaPlayerController>();
            controller.tileSize = TileSize;
            controller.currentNode = startNode;
            startNode.currentOccupant = playerObj;
        }

        private static void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }

            var diorama = cam.GetComponent<DioramaCamera>();
            if (diorama == null) diorama = cam.gameObject.AddComponent<DioramaCamera>();

            var player = Object.FindAnyObjectByType<LappaPlayerController>();
            if (player != null) diorama.target = player.transform;

            diorama.distance = 18f;
            diorama.pitchAngle = 46f;
            diorama.yawAngle = 45f;
            diorama.ApplyTransformImmediate();
        }

        private static void ConnectAllNeighbors(Dictionary<Vector2Int, GridNode> nodes)
        {
            Vector2Int[] dirs = new Vector2Int[]
            {
                new Vector2Int(1, 0),
                new Vector2Int(-1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(0, -1)
            };

            foreach (var kvp in nodes)
            {
                var node = kvp.Value;
                var coord = kvp.Key;

                foreach (var dir in dirs)
                {
                    var targetCoord = coord + dir;
                    if (nodes.TryGetValue(targetCoord, out var neighbor))
                    {
                        if (!node.neighbors.Contains(neighbor))
                        {
                            node.neighbors.Add(neighbor);
                        }
                    }
                }
            }
        }

        private static Material CreateLitMaterial(string name, Color baseColor, Color? emission = null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.name = name;
            mat.color = baseColor;
            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
            }
            return mat;
        }
    }
}
