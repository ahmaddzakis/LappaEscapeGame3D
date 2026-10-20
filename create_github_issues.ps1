# Script otomatis untuk membuat seluruh GitHub Issues Lappa Escape
# Pastikan sudah menjalankan 'gh auth login' di PowerShell sebelum menjalankan script ini.

$repo = "ahmaddzakis/LappaEscapeGame3D"
$gh = "C:\Program Files\GitHub CLI\gh.exe"

Write-Host "Mulai membuat 8 GitHub Issues untuk repository: $repo..." -ForegroundColor Cyan

# -------------------------------------------------------------
# Issue 1
# -------------------------------------------------------------
& $gh issue create --repo $repo `
  --title "[Core] Turn-Based Manager & Game Loop Lifecycle" `
  --body @"
## Deskripsi
Implementasi sistem siklus giliran terpusat (*Turn-Based Game Loop*) yang mengatur jalannya permainan secara deterministik sesuai prinsip GDD Lappa Escape.

## Siklus Giliran (Turn Sequence)
1. **Player Turn**: Menerima input diskrit (WASD / Panah) -> mengeksekusi langkah pemain.
2. **Environment Turn**: Objek dinamis lingkungan bereaksi (misal: pengurangan durabilitas ubin retak lava).
3. **Enemy Turn**: Seluruh AI Demon di panggung mengevaluasi posisi dan melakukan aksi (serang atau patroli).
4. **Turn Reset**: Membuka kunci input pemain untuk giliran berikutnya.

## Kriteria Penerimaan (Acceptance Criteria)
- [ ] Skrip `TurnManager.cs` mengontrol state sekuens giliran dengan Enum (`PlayerInput`, `PlayerMoving`, `EnvironmentTurn`, `EnemyTurn`).
- [ ] Event C# `OnStepFinished`, `OnEnvironmentUpdated`, dan `OnEnemyTurnFinished` terpasang rapi.
- [ ] Kontrol input pemain terkunci selama giliran environment dan enemy sedang berproses.
"@

# -------------------------------------------------------------
# Issue 2
# -------------------------------------------------------------
& $gh issue create --repo $repo `
  --title "[Player] Karakter Petualang & Grid Movement 2.0 (Discrete Step)" `
  --body @"
## Deskripsi
Integrasi model karakter petualang (*Low Poly Female Free*) dengan sistem pergerakan petak 2 meter, perputaran arah hadap halus, snapping posisi, dan validasi lantai jurang.

## Detail Fitur
- **Skala Petak:** 1 langkah = tepat 2.0 unit dunia.
- **Deteksi Lantai:** Raycast ke bawah; karakter menolak melangkah jika petak target berupa void/jurang lava tanpa lantai.
- **Deteksi Rintangan:** Raycast setinggi pinggang (0.8m) untuk mendeteksi dinding dan rintangan fisik.
- **Animasi:** Integrasi Animator Controller (Idle, Walk, Attack, Death).

## Kriteria Penerimaan (Acceptance Criteria)
- [ ] Karakter melangkah presisi 2 meter per input WASD/Arrow.
- [ ] Karakter berputar menghadap arah langkah (*Slerp* rotasi).
- [ ] Karakter tidak bisa menembus rintangan atau melayang di atas void lava.
- [ ] Event `OnStepFinished` memicu perpindahan fase giliran ke `TurnManager`.
"@

# -------------------------------------------------------------
# Issue 3
# -------------------------------------------------------------
& $gh issue create --repo $repo `
  --title "[Camera] Diorama Isometric Camera & Subtle Mouse Parallax" `
  --body @"
## Deskripsi
Pemasangan dan tuning sistem kamera isometrik diorama (`DioramaCamera.cs`) pada Main Camera untuk memberikan visual khas *Lara Croft GO* / board game miniatur 3D.

## Detail Fitur
- **Sudut Kamera:** Pitch ~46°, Yaw ~45°.
- **Follow Target:** Kamera mengikuti pergerakan karakter pemain secara mulus (*SmoothDamp*).
- **Parallax:** Sedikit pergeseran sudut berdasarkan kursor mouse untuk sensasi kedalaman 3D tanpa membuat pusing.

## Kriteria Penerimaan (Acceptance Criteria)
- [ ] Sudut kamera konsisten dan tidak dikunci paksa oleh Cinemachine bawaan.
- [ ] Posisi pemain selalu berada di framing tengah yang nyaman dipandang.
- [ ] Seluruh jalur puzzle Stage 1 terlihat jelas dari sudut elevasi kamera.
"@

# -------------------------------------------------------------
# Issue 4
# -------------------------------------------------------------
& $gh issue create --repo $repo `
  --title "[Environment] Sistem Petak Khusus: Cracked Lava Tile & Checkpoint Node" `
  --body @"
## Deskripsi
Pembuatan komponen interaktif lingkungan untuk ubin retak lava (*Cracked Node*) dan titik simpan sementara (*Checkpoint Node*).

## Detail Mekanik
1. **Cracked Lava Node:**
   - Injak ke-1: Durabilitas berkurang, ubin menyala merah/retak parah.
   - Injak ke-2: Ubin runtuh permanen menjadi lubang lava mendidih. Karakter mati jika menginjak ubin yang sudah runtuh.
2. **Checkpoint Node:**
   - Terletak di tengah Segmen 1 (sebelum ubin retak).
   - Menyimpan koordinat aman respawn pemain saat dilewati.

## Kriteria Penerimaan (Acceptance Criteria)
- [ ] Skrip `CrackedTile.cs` mencatat jumlah injakan pemain.
- [ ] Animasi/efek visual ubin runtuh saat injakan kedua.
- [ ] Skrip `CheckpointNode.cs` mengupdate spawnpoint aktif pemain pada `LevelManager`.
- [ ] Pemain yang mati langsung muncul kembali di Checkpoint terakhir.
"@

# -------------------------------------------------------------
# Issue 5
# -------------------------------------------------------------
& $gh issue create --repo $repo `
  --title "[Puzzle] Sistem Tuas Warna (Bridges) & Tuas Jembatan Geser (Sliding Bridge)" `
  --body @"
## Deskripsi
Pembuatan mekanik manipulasi jalan menggunakan tuas toggle warna (jembatan merah/cokelat/ungu) dan tuas geser arah panah untuk jembatan bergerak.

## Detail Fitur
1. **Tuas Toggle Warna (Color-Coded Lever):**
   - Mengaktifkan/menonaktifkan segmen jembatan warna pasangannya (animasi naik dari bawah lava atau turun).
   - Memperbarui status *walkable* node di grid.
2. **Tuas Jembatan Geser (Sliding Bridge Lever):**
   - Menggeser segmen jembatan secara linier searah panah rancangan desain level untuk menyambungkan dua tebing terpisah.

## Kriteria Penerimaan (Acceptance Criteria)
- [ ] Pemain dapat berinteraksi dengan tuas saat berada di petak tuas.
- [ ] Jembatan berpindah/muncul dengan transisi animasi mulus (*Smooth Lerp*).
- [ ] Node yang jembatannya nonaktif/bergeser tidak bisa diseberangi oleh pemain.
"@

# -------------------------------------------------------------
# Issue 6
# -------------------------------------------------------------
& $gh issue create --repo $repo `
  --title "[AI/Combat] Demon AI & Directional Combat System" `
  --body @"
## Deskripsi
Implementasi kecerdasan buatan musuh (*Demon AI*) dengan aturan pertarungan berbasis arah hadap (*Directional Combat*).

## Aturan Pertarungan (Combat Rules)
- **Face-to-Face (Pemain Masuk dari Arah Depan Tatapan Demon):**
  Pemain tewas -> animasi serangan Demon -> layar Game Over -> Respawn di Checkpoint.
- **Flank & Backstab (Pemain Masuk dari Arah Samping atau Belakang Demon):**
  Demon tereliminasi -> animasi serangan pemain -> Demon hancur/hilang dari node -> Pemain selamat menempati petak.

## Varian Perilaku Demon
- **Sentry (Statis):** Diam di tempat dan selalu mengawasi satu arah node di hadapannya.
- **Patroller (Dinamis):** Melangkah 1 petak bolak-balik setiap giliran pemain selesai melangkah.

## Kriteria Penerimaan (Acceptance Criteria)
- [ ] Skrip `DemonAI.cs` memiliki variabel arah hadap (`ForwardDirection`).
- [ ] Deteksi posisi pemain relatif terhadap arah hadap Demon (Depan vs Samping/Belakang).
- [ ] Sinkronisasi langkah patroli dengan fase `EnemyTurn` pada `TurnManager`.
"@

# -------------------------------------------------------------
# Issue 7
# -------------------------------------------------------------
& $gh issue create --repo $repo `
  --title "[Level Design] Konstruksi Dunia Stage 1 Lengkap (Inferno World)" `
  --body @"
## Deskripsi
Penyusunan tata letak dunia 3D Stage 1 di scene `InfernoGo.unity` menggunakan prefab modular *Inferno World Free* sesuai dokumen denah resmi *Level / Stage 1 Design Lappa Escape*.

## Rincian Segmen
1. **Segmen 1 (Onboarding):**
   - Start Node -> Jalur Lurus -> Percabangan Tuas Merah -> Jembatan Merah -> Demon #1 -> Checkpoint Node -> Cracked Lava Node -> Jembatan Geser.
2. **Segmen 2 (Klimaks):**
   - Koridor Tangga (5 Node) -> Area puzzle multi-tuas & multi-demon -> Blocked Path pembatas -> Finish Node.

## Kriteria Penerimaan (Acceptance Criteria)
- [ ] Tata letak node 100% presisi sesuai gambar denah rancangan level.
- [ ] Semua prefab lantai memiliki `BoxCollider` berukuran 2x2 meter.
- [ ] Finish Node (Cyan) memicu kondisi kemenangan panggung (*Stage Clear*).
"@

# -------------------------------------------------------------
# Issue 8
# -------------------------------------------------------------
& $gh issue create --repo $repo `
  --title "[UI/UX & Audio] Game Flow, Win/Lose Screens, & Sound Effects" `
  --body @"
## Deskripsi
Pengembangan antarmuka pengguna (UI), alur game over / win, dan integrasi audio efek tema vulkanik untuk memberikan *feedback* permainan yang memuaskan.

## Fitur UI & Audio
- **UI:**
  - Layar Stage Clear (Menang) saat mencapai Finish Node.
  - Efek layar kematian / transisi respawn ke Checkpoint.
- **Audio (SFX):**
  - Suara langkah kaki pada batu magma.
  - Suara tarikan tuas batu mekanis.
  - Suara gemuruh jembatan bergerak / ubin retak runtuh ke lava.
  - Suara tebasan serangan Demon dan eliminasi musuh.

## Kriteria Penerimaan (Acceptance Criteria)
- [ ] Canvas UI responsif dan mendukung resolusi layar PC.
- [ ] Tombol Restart / Main Lagi berfungsi dengan mulus.
- [ ] Efek suara tersambung ke setiap event aksi dalam game.
"@

Write-Host "Seluruh 8 GitHub Issues berhasil diproses!" -ForegroundColor Green
