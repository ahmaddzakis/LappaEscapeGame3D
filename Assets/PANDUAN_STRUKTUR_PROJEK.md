# 📁 Panduan Struktur Folder Proyek (Pembangunan Game Cerdas)

Dokumen ini menjelaskan struktur folder proyek agar mudah dikelola dan tidak membingungkan.

---

## 📂 Struktur Utama `Assets/`

```text
Assets/
├── 📁 Animation/                     # Semua aset animasi (Clip & Controller)
│   ├── Idle.anim                     # Animasi Idle (Tugas 2)
│   ├── Walk.anim                     # Animasi Walk (Tugas 2)
│   ├── NewIdle.anim                  # Variasi animasi Idle
│   ├── PlayerController.controller   # Animator Controller untuk Tugas 2 (Capsule/Player)
│   ├── UD_demo_character.controller  # Animator Controller untuk Tugas 3 (Undead Character)
│   └── 📁 Unused/                    # Aset animasi uji coba lama yang tidak dipakai
│       ├── Capsule.controller
│       └── Idle.anim
│
├── 📁 Scenes/                        # Semua Scene Unity (Hanya berisi file .unity)
│   ├── Tugas1.unity                  # Scene untuk Tugas 1 (First Person Movement)
│   ├── Tugas2.unity                  # Scene untuk Tugas 2 (FSM Walk/Idle)
│   └── SampleScene.unity             # Scene default bawaan Unity template
│
├── 📁 Scripts/                       # Semua skrip C# (Dikelompokkan rapi per tugas)
│   ├── 📁 Tugas1/                    # Skrip khusus Tugas 1
│   │   ├── PlayerMovement.cs         # Pergerakan dasar WASD
│   │   └── MouseLook.cs              # Rotasi kamera First Person mengikuti mouse
│   ├── 📁 Tugas2/                    # Skrip khusus Tugas 2
│   │   └── PlayerFSM.cs              # State Machine (isWalking flag & pergerakan)
│   ├── 📁 Tugas3/                    # Skrip khusus Tugas 3 (Dungeon, Karakter, & Skeleton GO Puzzle)
│   │   ├── GridPlayerMovement.cs     # Pergerakan petak (Grid step-by-step) untuk mode Skeleton GO
│   │   ├── DioramaCamera.cs          # Kamera isometrik sudut tinggi (3D diorama view)
│   │   ├── DeadZone.cs               # Deteksi jatuh, layar YOU DIED, dan respawn
│   │   ├── ToonCharacterController.cs# (Alternatif) Kontrol karakter bebas 3rd person & kamera orbit
│   │   ├── DoorInteractable.cs       # Interaksi buka/tutup pintu tombol 'E'
│   │   └── InteractionPrompt.cs      # UI popup teks otomatis ("Press 'E' to open")
│   └── 📁 Editor/                    # Alat bantu Unity Editor (Menu Tools)
│       ├── SkeletonGoSetup.cs        # Tools -> Skeleton GO -> Setup Diorama Mode (1-Click)
│       ├── ToonCharacterSetup.cs     # Tools -> Setup Toon Character & Cinemachine
│       └── FixPinkMaterials.cs       # Tools -> Fix Pink Materials (URP Converter)
│
├── 📁 Garshasp/                      # Asset Pack Butcher Demon Model (Musuh AI)
│   └── 📁 Butcher Demon Model/
│       ├── 📁 Model/Demon_Lord_Model.fbx
│       └── 📁 Perfabs/Demon_Lord_Model.prefab
│
├── 📁 StylizedHandPaintedDungeon(Free)/ # Asset Pack Dungeon (Tugas 3)
│   └── 📁 Demo/Demo_Scene/
│       └── Demo_Scene.unity          # Scene Dungeon untuk Tugas 3
│
├── 📁 Toon_RTS/                      # Asset Pack Model & Animasi Karakter Undead (Tugas 3)
│
├── 📁 Settings/                      # Konfigurasi URP (Universal Render Pipeline)
├── 📁 TutorialInfo/                  # File tutorial bawaan Unity template
└── InputSystem_Actions.inputactions  # Konfigurasi Unity Input System
```

---

## 🎯 Panduan Cepat Per Tugas

| Tugas | Scene Unity | Lokasi Skrip Terkait | Catatan Utama |
|---|---|---|---|
| **Tugas 1** | `Assets/Scenes/Tugas1.unity` | `Assets/Scripts/Tugas1/` | Menggunakan `PlayerMovement.cs` & `MouseLook.cs` |
| **Tugas 2** | `Assets/Scenes/Tugas2.unity` | `Assets/Scripts/Tugas2/` | Menggunakan `PlayerFSM.cs` & Animator `PlayerController.controller` |
| **Tugas 3** | `Assets/StylizedHandPaintedDungeon(Free)/Demo/Demo_Scene/Demo_Scene.unity` | `Assets/Scripts/Tugas3/` | Mode **Skeleton GO** (`GridPlayerMovement.cs` & `DioramaCamera.cs`) atau Free-Roam 3rd Person |
