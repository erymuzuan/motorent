# Pelarasan Harga Dinamik: Maksimumkan Keuntungan Musim Perayaan

Adakah anda masih mengenakan kadar sewa yang sama untuk Perodua Myvi semasa Hari Raya seperti hari biasa? Jika ya, anda mungkin kehilangan ribuan Ringgit keuntungan.

Dalam pasaran sewa kereta di Malaysia, permintaan sentiasa berubah. Antara cuti sekolah, Tahun Baru Cina, dan musim puncak di Langkawi, kenderaan anda bernilai lebih tinggi pada waktu tertentu. **Pelarasan Harga Dinamik (Dynamic Pricing)** mengautomasikan perubahan kadar sewa anda supaya anda tidak terlepas keuntungan musim puncak.

![Hero: Kedai Sewa Kereta di Langkawi dengan Kalendar Perayaan](images/13-dynamic-pricing-hero.webp)

## Cara Ia Berfungsi dalam 30 Saat

Harga Dinamik melaraskan kadar sewa asas anda secara automatik berdasarkan peraturan (rules) yang anda tetapkan.

1.  **Tetapkan Peraturan**: Pilih julat tarikh (contoh: minggu Hari Raya) atau hari berulang (contoh: setiap hari Sabtu).
2.  **Tentukan Pelarasan**: Gunakan pengganda (contoh: 1.5x untuk +50%) atau jumlah tetap (contoh: +RM50).
3.  **Automatik**: JaleOs akan memaparkan kadar baru ini secara automatik di portal tempahan dan sistem check-in staf anda.

---

## Kisah: Armada Kereta Encik Ridzuan di Langkawi

Encik Ridzuan menguruskan 15 buah kereta di Kuah, Langkawi. Sebelum menggunakan JaleOs, beliau cuba menaikkan harga secara manual semasa pameran *LIMA* dan *Hari Raya*.

*   **Masalah**: Staf beliau sering terlupa untuk memberikan harga tinggi, atau memberikan "harga lama" kepada pelanggan tetap, menyebabkan beliau kerugian hampir RM2,000 dalam tempoh seminggu.
*   **Penyelesaian**: Beliau menetapkan peraturan **"Puncak Perayaan"** dalam JaleOs dengan pengganda **1.4x**.
*   **Hasil**: Setiap tempahan pada minggu tersebut dikira secara automatik dengan kadar yang lebih tinggi. Staf beliau tidak perlu menghafal harga baru, dan pendapatannya meningkat sebanyak 40% tanpa perlu menambah kereta baru.

![Senario: Pemilik Bisnes Malaysia melihat Pertumbuhan Hasil](images/13-dynamic-pricing-scenario.webp)

---

## Angka: Mengapa Harga Dinamik Sangat Berbaloi

Pelarasan kecil membawa pulangan besar. Mari lihat contoh untuk 10 buah kereta di Pulau Pinang:

| Senario | Kadar Asas (Harian) | Hasil Puncak (7 Hari) | Perbezaan |
| :--- | :--- | :--- | :--- |
| **Harga Statik** | RM120 | RM8,400 | - |
| **Dinamik (+30%)** | RM156 | RM10,920 | **+RM2,520** |

Dengan hanya kenaikan 30% dalam satu minggu puncak, sistem ini telah membayar kos langganannya sendiri untuk setahun penuh.

---

## Persediaan Pantas

Ikuti langkah ini untuk mula memaksimumkan hasil anda:

1.  **Aktifkan Ciri**: Pergi ke **Settings > Organization Settings** dan pastikan "Dynamic Pricing" telah diaktifkan.
2.  **Tambah Peraturan**: Navigasi ke **Settings > Pricing Rules** dan klik **Add Rule**.
3.  **Konfigurasi**:
    *   **Name**: cth. "Puncak Hari Raya 2026"
    *   **Type**: Pilih "Event" atau "Season".
    *   **Dates**: Tetapkan tarikh mula dan tamat.
    *   **Multiplier**: Masukkan `1.30` untuk kenaikan 30%.
4.  **Priority**: Jika anda mempunyai peraturan yang bertindih, peraturan dengan nombor **Priority** yang lebih tinggi akan diutamakan.

---

## Operasi Harian

*   **Paparan Kalendar**: Gunakan **Pricing Calendar** untuk melihat kadar sewa efektif bagi mana-mana hari pada masa hadapan.
*   **Atasi Secara Manual (Manual Override)**: Anda masih boleh menukar harga secara manual semasa proses check-in jika anda ingin memberikan diskaun khas.
*   **Spesifik Kenderaan**: Anda boleh menetapkan peraturan hanya untuk kereta "Luxury" atau "Van", sementara mengekalkan harga biasa untuk kereta "Economy".

## Optimumkan Harga Anda

*   **Weekend Boosters**: Tetapkan peraturan berulang "Sabtu/Ahad" untuk kenaikan +10% bagi pelancong hujung minggu.
*   **Diskaun Jangka Panjang**: Selain harga puncak, gunakan **Duration Discounts** untuk menggalakkan sewaan 7 hari ke atas semasa musim biasa.
*   **Early Bird Rules**: Cipta peraturan yang hanya terpakai jika tempahan dibuat jauh lebih awal.

## Penyelesaian Masalah

| Masalah | Penyelesaian |
| :--- | :--- |
| **Harga tidak berubah** | Pastikan peraturan ditetapkan kepada "Active" dan tarikh adalah betul. |
| **Peraturan salah digunakan** | Periksa nilai **Priority**. Nombor yang lebih tinggi akan diutamakan. |
| **Harga terlalu mahal?** | Gunakan tetapan **Max Rate** untuk mengehadkan harga tertinggi yang boleh dicaj. |

## Panduan Berkaitan
*   [01-orgadmin-quickstart.md](01-orgadmin-quickstart.md)
*   [08-cashier-till-guide.md](08-cashier-till-guide.md)
*   [10-agent-management-guide.md](10-agent-management-guide.md)
