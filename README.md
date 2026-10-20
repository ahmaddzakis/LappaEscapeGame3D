# GAME DESIGN DOCUMENT (GDD) & AI AGENT DEVELOPMENT SPECIFICATION
# LAPPA ESCAPE
> **Mata Kuliah:** Pembangunan Game Cerdas  
> **Platform & Engine:** PC / Unity 6 (6000.4.1f1 LTS)  
> **Render Pipeline:** Universal Render Pipeline (URP)  
> **Input System:** Unity New Input System  
> **Genre:** 3D Turn-Based Isometric Puzzle Adventure  
> **Metode Pengembangan:** ATM (Amati, Tiru, Modifikasi) terinspirasi dari *Lara Croft GO*  

---

## 1. Executive Summary & Visi Game

### 1.1 Ringkasan Konsep
**Lappa Escape** adalah game puzzle petualangan 3D berbasis petak (*grid & turn-based*) dengan sudut pandang isometrik diorama. Pemain mengendalikan seorang petualang wanita yang terjebak di dalam dunia vulkanik purba (**Inferno World**). Untuk keluar hidup-hidup, pemain harus menghitung setiap langkah dengan cermat, memecahkan teka-teki jembatan dan ubin retak lava, serta mengakali monster neraka (**Demon**) menggunakan aturan logika giliran.

### 1.2 Filosofi Desain (Core Pillars)
1. **Deterministik & Murni Logika (*Zero Twitch Reflex*)**:
   Tidak ada batasan waktu real-time. Keberhasilan pemain 100% ditentukan oleh ketepatan kalkulasi giliran langkah. Dunia dan musuh hanya bereaksi ketika pemain mengambil langkah.
2. **Kamera Isometrik Diorama (*Living Miniature*)**:
   Kamera melayang pada sudut pandang tetap (Pitch ~45°, Yaw ~45°) dengan efek *subtle parallax* mouse, memberikan sensasi memecahkan miniatur teka-teki hidup.
3. **Keterbacaan Visual Tinggi (*High Readability*)**:
   Setiap petak (*node*), tuas (*lever*), dan jembatan memiliki kode visual dan warna yang tegas sehingga pemain dapat membaca alur konsekuensi puzzle sebelum melangkah.

---

## 2. Aset Projek (Asset Manifest)

| Kategori | Aset yang Digunakan | Lokasi Direktori | Peran dalam Game |
| :--- | :--- | :--- | :--- |
| **Player Character** | *Low Poly Female Free* by Shokubutsu Studio | `Assets/Shokubutsu Studio/Low Poly Female Free` | Karakter utama (Protagonis / Lappa). Humanoid rig dengan state: Idle, Walk, Attack, Die. |
| **Environment & Props** | *Inferno World Free* by ithappy | `Assets/ithappy/Inferno_World_Free` | Prefab modular lantai batu magma, kolam lava, tebing obsidian, dan dekorasi lingkungan. |
| **Enemy Character** | Demon / Imp Character Model | `Assets/` (Prefab modular demon) | Musuh turn-based dengan AI directional attack & patrol. |
| **Gameplay Scene** | `InfernoGo.unity` | `Assets/Scenes/InfernoGo.unity` | Scene utama panggung level Stage 1. |
| **Core Scripts** | Custom C# Scripts | `Assets/Scripts/` | Logika grid, turn manager, interaksi node, dan AI musuh. |

---

## 3. Sistem Mekanik & Aturan Permainan (Game Rules)

### 3.1 Siklus Giliran (Turn-Based Cycle)
Permainan berjalan dalam loop sekuensial 4 fase:
```
[1. Player Input] ──> [2. Player Step] ──> [3. Environment Update] ──> [4. Enemy Turn] ──> [Kembali ke 1]
```
1. **Player Input**: Pemain menekan salah satu tombol arah (W/A/S/D atau Panah).
2. **Player Step**: Karakter meluncur halus (*Lerp/Slerp*) sejauh 1 petak grid (2 meter). Kontrol input dikunci selama animasi berlangsung.
3. **Environment Step**: Objek dinamis lingkungan bereaksi (lantai retak lava runtuh jika injakan ke-2).
4. **Enemy Step**: Seluruh Demon di level mengevaluasi kondisi: menyerang jika pemain berada di jangkauan depan, atau melangkah sesuai pola patroli.

---

### 3.2 Spesifikasi Node & Petak Level

Berdasarkan dokumen denah level resmi:

| Simbol / Jenis Node | Representasi Visual | Perilaku & Logika Mekanik |
| :--- | :--- | :--- |
| **Start Node** | Petak Kuning (Ikon Orang) | Titik awal karakter muncul saat level dimulai pertama kali. |
| **Finish Node** | Petak Cyan (Ikon Centang) | Titik tujuan akhir. Begitu pemain menginjak petak ini, memicu kondisi **Stage Clear / Level Win**. |
| **Node Path** | Petak Abu-abu Netral | Jalur jalan normal berukuran $2\text{m} \times 2\text{m}$. Aman dipijak berkali-kali. |
| **Bridge Node (Jembatan Dinamis)** | Petak Berwarna (Merah / Cokelat / Ungu) | Jalur jembatan yang status aktif/nonaktifnya dikendalikan oleh tuas warna bersesuaian. Saat nonaktif, petak dianggap jurang/lava tak bisa dilewati. |
| **Sliding Bridge (Jembatan Geser)** | Petak Jembatan dengan Panah Arah | Jembatan yang bergeser posisi fisik dari Koordinat A ke Koordinat B ketika tuas geser ditarik. |
| **Cracked Node (Ubin Retak Lava)** | Petak Berpola Retakan Ungu/Lava | **Aturan 2x Injak:**<br>• *Injakan 1:* Lantai mulai retak parah dan menyala merah.<br>• *Injakan 2 (atau saat ditinggalkan):* Lantai runtuh permanen menjadi lava mendidih. Karakter mati jika tetap berdiri di atasnya. |
| **Checkpoint Node** | Petak Putih (Ikon Bendera) | Saat karakter melewati petak ini, titik respawn pemain otomatis diperbarui ke koordinat checkpoint ini. |
| **Blocked Path** | Garis Pembatas Biru | Koneksi antar-petak terhalang tembok/jurang. Mencegah karakter melangkah langsung meskipun petak tampak bersebelahan. |

---

### 3.3 Sistem Tuas Interaktif (Levers)

1. **Tuas Toggle Warna (*Color-Coded Lever* - Hitam/Merah/Ungu)**:
   * Terletak di petak cabang buntu.
   * Ketika pemain melangkah ke petak tuas, tombol aksi / otomatis memicu tuas berganti status (*Toggled*).
   * Membuka atau menutup jembatan warna pasangannya dengan animasi transisi naik/turun yang mulus.
2. **Tuas Jembatan Geser (*Sliding Bridge Lever* - Putih)**:
   * Menggeser segmen jembatan secara linier searah panah desain untuk menyambungkan dua tebing terpisah.

---

### 3.4 Mekanik Pertarungan & AI Musuh (*Demon*)

Demon memiliki sifat penglihatan dan serangan berbasis arah hadap (*Directional Combat*):

```
                       [Belakang Demon]
                              │
    [Samping Demon] ───> [ DEMON ] <─── [Samping Demon]
                              │
                      [ DEPAN DEMON ]  <─── ZONA BAHAYA (Face-to-Face)
```

* **Aturan Kalah (Player Dies)**:
  * Terjadi jika pemain melangkah masuk ke petak tepat di **depan wajah Demon (Face-to-Face)**.
  * Demon langsung memicu animasi tebasan/serangan, layar menampilkan efek kekalahan, dan pemain di-respawn di Checkpoint terakhir.
* **Aturan Menang (Demon Defeated)**:
  * Terjadi jika pemain melangkah masuk ke petak Demon dari arah **Samping (Flank)** atau arah **Belakang (Backstab)**.
  * Karakter pemain melakukan animasi serangan, Demon hancur/hilang dari grid, dan pemain selamat menempati petak tersebut.
* **Pola Gerak Demon**:
  * **Sentry (Statis)**: Diam di tempat dan selalu mengawasi satu arah node di depannya.
  * **Patroller (Dinamis)**: Melangkah 1 petak bolak-balik setiap giliran pemain selesai melangkah.

---

## 4. Rincian Desain Level (Stage 1 Breakdown)

Level 1 didesain memiliki 2 segmen terintegrasi sesuai diagram arsitektur level:

```
[START] 
   └──> Jalur Lurus
           └──> Percabangan Tuas Merah 1
                   └──> Jembatan Merah Aktif
                           └──> Demon #1 (Pancingan Samping)
                                   └──> [CHECKPOINT NODE]
                                           └──> Cracked Tile (Lava)
                                                   └──> Tuas Putih (Jembatan Geser)
                                                           └──> [TRANSISI KE SEGMEN 2]
                                                                   └──> Koridor Tangga (5 Node)
                                                                           └──> Puzzle Multi-Tuas & Multi-Demon
                                                                                   └──> [FINISH NODE (WIN)]
```

### 4.1 Segmen 1: Pengenalan Dasar (Bottom-Right)
* **Tujuan Edukasi Pemain:**
  1. Memahami grid step movement (WASD).
  2. Memahami tuas untuk membuka jalan yang buntu oleh jembatan merah.
  3. Memahami aturan pertarungan Demon (membunuh dari samping, bukan dari depan).
  4. Merasakan checkpoint dan resiko ubin retak lava.
  5. Mengoperasikan tuas geser untuk membuka jalan ke seberang jurang.

### 4.2 Segmen 2: Evaluasi & Tantangan (Top-Right)
* **Tujuan Edukasi Pemain:**
  1. Menyeberangi koridor diagonal panjang.
  2. Mengatur urutan penarikan tuas (merah, cokelat/ungu, putih) tanpa terperangkap oleh patroli Demon.
  3. Menemukan rute optimal yang tidak terhalang *Blocked Path* untuk menginjak **Finish Node**.

---

## 5. Arsitektur Teknis & Skrip (Untuk AI Agent / Developer)

Seluruh logika game harus dibangun dengan pola kode modular, bersih, dan mematuhi standar Unity 6.

### 5.1 Daftar Modul Skrip Utama
```
Assets/Scripts/
├── Core/
│   ├── TurnManager.cs           # Pusat kendali giliran (Player, Env, Enemy)
│   ├── GridManager.cs           # Peta koordinat node, pencarian tetangga, & validasi jalur
│   └── LevelManager.cs          # Pengatur Checkpoint, Win/Lose state, & Respawn
├── Player/
│   ├── GridPlayerMovement.cs    # Eksekusi gerak petak 2m, input discrete, rotasi, & animasi
│   └── PlayerCombat.cs          # Logika serang dari belakang/samping & trigger kematian
├── Environment/
│   ├── GridNode.cs              # Data komponen tiap petak (posisi, tetangga, walkable flag)
│   ├── LeverInteractable.cs     # Tuas warna & tuas geser jembatan
│   ├── BridgeController.cs      # Animasi naik-turun jembatan & update walkable node
│   ├── SlidingBridge.cs         # Animasi translasi jembatan geser antar-node
│   ├── CrackedTile.cs           # Penghitung pijakan ubin retak lava
│   └── CheckpointTrigger.cs     # Pengubah titik respawn aktif
├── Enemy/
│   ├── DemonAI.cs               # Finite State Machine (Idle, Patrol, Attack, Death)
│   └── EnemyTurnHandler.cs      # Sinkronisasi giliran gerak musuh dengan TurnManager
└── Camera/
    └── DioramaCamera.cs         # Kamera isometrik 45° smooth-follow & mouse parallax
```

### 5.2 Standar Implementasi Grid
* **Skala Petak:** $1 \text{ Node} = 2.0\text{f}$ unit di dunia Unity ($X$ dan $Z$).
* **Orientasi Isometrik:** Sumbu grid dibuat selaras diagonal $45^\circ$ terhadap pandangan kamera, atau pergerakan karakter dinormalisasi terhadap sudut pandang isometrik kamera (`DioramaCamera`).
* **Collider Rule:** Setiap petak lantai wajib memiliki `BoxCollider` agar deteksi fisik, raycast kaki, dan posisi tinggi ($Y$) selalu presisi.

---

## 6. Petunjuk Operasional untuk AI Agent Antigravity

Saat AI Agent bekerja di repo ini, wajib mengikuti panduan kerja berikut:

1. **Jaga Integritas Scene & Skrip**:
   * Scene pengerjaan utama adalah **`Assets/Scenes/InfernoGo.unity`**.
   * Jangan mengubah atau menghapus `Tugas1.unity` dan `Tugas2.unity`.
2. **Komentar & Bahasa**:
   * Semua variabel inspector, tooltip, dan komentar kode ditulis dalam **Bahasa Indonesia** yang informatif.
3. **Pemberian Menu Pintas Editor**:
   * Buat alat bantu otomatis di menu editor Unity (`Tools/Lappa Escape/...`) untuk memudahkan snap grid, verifikasi collider lantai, dan setup otomatis level dalam 1 klik.
4. **Kompatibilitas Unity 6**:
   * Jangan gunakan API yang sudah usang (*deprecated*) seperti `FindObjectsOfType` (gunakan `Object.FindAnyObjectByType` atau `Object.FindObjectsByType` tanpa `FindObjectsSortMode`).
   * Gunakan New Input System (`UnityEngine.InputSystem`).

---
*Dokumen ini adalah acuan resmi desain dan teknis pengembangan game Lappa Escape.*
