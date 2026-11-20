# AnatoLab - Human Muscular System

Atlas otot manusia 3D interaktif yang dibuat dengan **Unity 6, C#, dan Universal Render Pipeline (URP)** untuk proyek mata kuliah Komputer Grafis.

Pengguna dapat memilih wilayah tubuh pada model, membaca nama Indonesia dan istilah Latin, mempelajari fungsi otot, serta mengamati model dari berbagai sudut pandang.

> Model menggunakan 24 perkiraan wilayah permukaan dari satu mesh gabungan. Batas pilihan belum mengikuti segmentasi anatomis setiap otot secara presisi.

## Fitur

- Eksplorasi 3D dengan orbit kamera, zoom berbatas, dan tampilan depan, belakang, kiri, serta kanan.
- Pemilihan wilayah melalui klik model atau daftar navigasi.
- Highlight saat kursor diarahkan dan saat wilayah dipilih.
- Pencarian langsung berdasarkan nama Indonesia, istilah Latin, atau wilayah tubuh.
- Panel informasi berisi deskripsi dan fungsi utama otot.
- Mode X-ray dengan transparansi pada wilayah lain.
- Perpindahan fokus kamera yang halus, tombol tutup, dan reset.
- Daftar wilayah yang dapat digulir dan pemeriksaan konfigurasi melalui menu editor.

## Teknologi

| Komponen | Teknologi |
| --- | --- |
| Engine | Unity 6000.6.3f1 |
| Bahasa | C# |
| Rendering | Universal Render Pipeline |
| UI | Unity UI dan TextMesh Pro |
| Interaksi | Physics Raycast dan Convex MeshCollider |
| Input | Input System: Both |
| Platform build | Windows 64-bit |

## Menjalankan proyek

1. Clone atau unduh repository ini. Ekstrak terlebih dahulu jika menggunakan ZIP.
2. Buka folder yang berisi `Assets`, `Packages`, dan `ProjectSettings` melalui Unity Hub.
3. Gunakan **Unity 6000.6.3f1** dan tunggu proses impor aset selesai.
4. Buka **`Assets/AnatoLab/MuscularExplorer.unity`**.
5. Tekan **Play** untuk mulai menjelajah model.

`Assets/Scenes/SampleScene.unity` adalah scene awal proyek. Gunakan `MuscularExplorer` untuk aplikasi AnatoLab.

## Aplikasi Windows

Build siap pakai tersedia pada [halaman Releases](https://github.com/mfalviankp/AnatoLab-Unity/releases).

Ekstrak seluruh isi `AnatoLab_Windows.zip`, lalu jalankan **`AnatoLab - Human Muscular System.exe`**. Pertahankan `UnityPlayer.dll`, folder `*_Data`, `MonoBleedingEdge`, dan berkas pendukung lain dalam folder hasil ekstrak.

## Kontrol

| Aksi | Kontrol |
| --- | --- |
| Memilih wilayah | Klik kiri pada model atau daftar navigasi |
| Memutar kamera | Tahan klik kanan, lalu geser mouse |
| Zoom | Scroll mouse atau tombol `+` / `-` |
| Fokus wilayah terpilih | **F** atau **Fokus ke Wilayah** |
| Menutup informasi | **Esc** atau **Tutup Pilihan** |
| Reset tampilan | **Home** atau **Reset View** |
| Tampilan standar | **Depan**, **Belakang**, **Kiri**, **Kanan** |
| Transparansi wilayah lain | Tombol **X-ray** |

## Susunan kode

| Lokasi | Tanggung jawab |
| --- | --- |
| `Assets/AnatoLab/Scripts/OrganData.cs` | Nama, istilah Latin, deskripsi, fungsi, dan arah fokus wilayah |
| `Assets/AnatoLab/Scripts/OrganSelector.cs` | Raycast, penghalang UI, hover, seleksi, dan X-ray |
| `Assets/AnatoLab/Scripts/OrbitCamera.cs` | Orbit, zoom, fokus, dan tampilan standar |
| `Assets/AnatoLab/Scripts/AnatomyApp.cs` | Daftar wilayah, pencarian, panel informasi, dan tombol kontrol |
| `Assets/AnatoLab/Editor/AnatoLabBuilder.cs` | Pembuatan scene dan validasi referensi |
| `Assets/AnatoLab/Shaders/Ghost.shader` | Shader transparan untuk mode X-ray |
| `Assets/AnatoLab/GeneratedFinal` | Mesh dan material yang dipakai scene akhir |
| `Assets/Models` | Model sumber Meshy |

## Pengujian

Validasi melalui **AnatoLab > Periksa Scene** berhasil untuk **24 OrganData**, **24 Convex MeshCollider**, dan referensi utama UI.

Pengujian melalui antarmuka Play Mode meliputi klik model, hover, daftar gulir, pencarian dengan dan tanpa hasil, penghapusan pencarian, panel informasi, fokus, X-ray, penutupan pilihan, tampilan belakang, scroll zoom, dan reset. Klik tombol UI tidak memilih model di belakangnya.

Build Windows berhasil dijalankan dan diuji untuk pemilihan Dada, fokus otomatis, pencarian, penghapusan pencarian, X-ray, reset, tampak belakang, dan klik langsung pada Punggung atas. Pemeriksaan terakhir pada **20 November 2026** menunjukkan **0 error di Console Play Mode**.

Orbit klik kanan sudah diimplementasikan, tetapi gerakan menahan tombol kanan dan menyeret belum diuji secara otomatis. Periksa kontrol tersebut secara manual saat menjalankan aplikasi.

## Batas model

Model `Meshy_AI_Human_ecorche_full_bo_0506161047_texture.glb` merupakan satu mesh gabungan. Pemisahan wilayah menggunakan koordinat permukaan dan dapat mencakup otot, tendon, atau tulang di sekitarnya. Kepala, leher, tangan, kaki, dan panggul tidak diperlakukan sebagai satu otot tertentu. Otot dalam belum tersedia sebagai objek terpisah.

X-ray adalah efek transparansi untuk eksplorasi model. Aplikasi ditujukan untuk pembelajaran visual dasar. Pengembangan selanjutnya dapat menggunakan model berlabel per otot dan pemeriksaan pemetaan anatomi.

## Referensi dan atribusi

- [OpenStax - Muscles of the Pectoral Girdle and Upper Limbs](https://openstax.org/books/anatomy-and-physiology-2e/pages/11-5-muscles-of-the-pectoral-girdle-and-upper-limbs).
- [OpenStax - Appendicular Muscles of the Pelvic Girdle and Lower Limbs](https://openstax.org/books/anatomy-and-physiology-2e/pages/11-6-appendicular-muscles-of-the-pelvic-girdle-and-lower-limbs).

Deskripsi anatomi merupakan ringkasan berbahasa Indonesia untuk pembelajaran dasar. Model dan tekstur berasal dari aset Meshy yang sudah digunakan dalam proyek. Hak penggunaan aset pihak ketiga mengikuti lisensi masing-masing; repository ini tidak memberikan lisensi ulang atas aset tersebut.

## Pengembang

**Muhammad Falvian Kemal Pasha**  
Teknik Informatika, Universitas Brawijaya  
Proyek mata kuliah **Komputer Grafis**  
GitHub: [@mfalviankp](https://github.com/mfalviankp)
