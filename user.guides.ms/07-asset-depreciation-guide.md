# Panduan Susut Nilai Aset: Kos Sebenar Fleet Anda

Ramai pengusaha kereta sewa di Malaysia melihat baki akaun bank mereka pada hujung bulan dan menganggap pendapatan kasar sebagai keuntungan bersih. Tetapi kenderaan seperti Perodua Bezza atau Yamaha NMAX adalah aset yang mengalami susut nilai (depreciation). Setiap kilometer yang dipandu mengurangkan nilainya.

Jika anda tidak menjejaki susut nilai, anda mungkin menghadapi masalah pada masa hadapan. Apabila tiba masanya untuk menggantikan fleet anda yang semakin lama, anda mungkin sedar anda tidak mempunyai modal yang cukup kerana anda telah membelanjakan "keuntungan" anda tanpa disedari.

**Susut Nilai Aset JaleOS** membantu anda menjejaki nilai kenderaan anda yang semakin menurun, menunjukkan Pulangan Pelaburan (ROI) dan Keuntungan Bersih yang *sebenar*. Ia memastikan anda tahu dengan tepat bila sesebuah kenderaan tidak lagi menguntungkan dan perlu dijual.

![Gambaran Keseluruhan Susut Nilai Aset](images/07-asset-depreciation-hero.png)

## Cara Ia Berfungsi dalam 30 Saat

1.  **Tambah Aset**: Pautkan rekod kewangan kepada kenderaan dalam fleet anda dengan kos perolehannya.
2.  **Tetapkan Kaedah**: Pilih bagaimana kenderaan kehilangan nilai (cth., Garis Lurus/Straight Line selama 5 tahun).
3.  **Jalankan Susut Nilai**: JaleOS mengira dan merekodkan nilai yang hilang setiap bulan secara automatik.
4.  **Lihat Keuntungan**: Sistem menolak susut nilai dan perbelanjaan daripada pendapatan sewa untuk menunjukkan Keuntungan Bersih sebenar anda.

---

## Kisah: Kerugian Tersembunyi Khairul

Khairul memiliki fleet 20 buah kereta di Pulau Pinang. Selama dua tahun, perniagaannya sangat rancak. Wang tunai sentiasa masuk.

*   **Masalah**: Khairul tidak menjejaki susut nilai. Apabila lima buah keretanya yang lebih lama mula memerlukan penyelenggaraan yang mahal, dia sedar dia perlu menjualnya. Tetapi dia tidak menyimpan sebarang wang daripada sewaan sebelumnya untuk menampung jurang antara nilai jualan semula (resale value) yang rendah dan kos kereta baru.
*   **Penyelesaian**: Khairul mula menggunakan **Asset Dashboard** dalam JaleOS. Dia menetapkan susut nilai "Straight Line" untuk keseluruhan fleet yang tinggal.
*   **Hasil**: Khairul kini melihat dengan tepat berapa banyak "ekuiti" yang tinggal dalam setiap kereta. Dia tahu keuntungan bulanannya yang *sebenar* selepas menolak kos susut nilai yang tidak kelihatan, membolehkannya mengembangkan fleetnya dengan selamat tanpa kejutan aliran tunai.

---

## Papan Pemuka Aset: Pusat Kawalan Kewangan Anda

Papan Pemuka Aset (Asset Dashboard) (`/finance/asset-dashboard`) memberikan pandangan menyeluruh tentang kesihatan kewangan fleet anda.

- **Metrik KPI**: Jejaki Total Invested (Jumlah Pelaburan), Current Book Value (Nilai Buku Semasa), Accumulated Depreciation (Susut Nilai Terkumpul), dan Fleet ROI (ROI Fleet).
- **Prestasi Terbaik (Top Performers)**: Lihat serta-merta kenderaan mana yang menjana keuntungan sebenar tertinggi.
- **Perhatian Diperlukan (Attention Needed)**: Dapatkan amaran untuk larian susut nilai yang tertunggak atau aset yang kurang berprestasi.

![Papan Pemuka Aset](images/07-asset-dashboard.png)

---

## Persediaan Pantas: Menjejaki Aset

### 1. Mencipta Rekod Aset

Untuk mula menjejaki susut nilai sesebuah kenderaan:

1. Navigasi ke **Finance > Assets** dan klik **+ Add Asset**.
2. Pilih kenderaan dan masukkan **Acquisition Date** (Tarikh Perolehan) dan **Acquisition Cost** (Kos Perolehan) (cth., RM 45,000).
3. Konfigurasikan tetapan:
   - **Method**: Pilih cara mengira susut nilai (lihat di bawah).
   - **Useful Life**: Jangka hayat dalam bulan (cth., 60 bulan).
   - **Residual Value**: Anggaran nilai jualan semula pada akhir hayatnya (cth., RM 15,000).
4. Klik **Save**.

### 2. Penjelasan Kaedah Susut Nilai

| Kaedah (Method) | Cara Ia Berfungsi | Sesuai Untuk |
|--------|--------------|----------|
| **Straight Line** | Jumlah bulanan yang sama sepanjang jangka hayat. | Kereta standard dengan penurunan nilai yang boleh dijangka. |
| **Day Out of Door** | Susut nilai % serta-merta pada sewaan pertama. | Motosikal yang kehilangan nilai ketara sebaik sahaja digunakan. |
| **Declining Balance** | % daripada nilai buku semasa (tinggi pada mulanya, lebih perlahan kemudian). | Kenderaan mewah (Luxury). |
| **Hybrid** | Day Out of Door + Straight Line. | Penjejakan tepat penurunan awal + penurunan berterusan. |

### 3. Merekod Susut Nilai

Untuk memastikan rekod anda tepat, anda harus menjalankan susut nilai setiap bulan:

1. Pergi ke **Finance > Asset Dashboard** dan klik **Run Monthly Depreciation** dalam menu Tindakan Pantas (Quick Actions).
2. Pilih tempoh (bulan/tahun) dan semak jumlah yang dikira.
3. Klik **Confirm**.

---

## Maklumat Lanjut: Butiran Aset & Keuntungan

Anda boleh melihat maklumat kewangan terperinci untuk mana-mana kenderaan tunggal dengan mengklik ikon carta dalam senarai aset.

### Carta Nilai Aset
Halaman Butiran Aset (Asset Details) (`/finance/assets/{id}/details`) memaparkan carta interaktif yang menunjukkan nilai buku kenderaan dari semasa ke semasa, termasuk unjuran masa depan berdasarkan kaedah susut nilai pilihan anda.

![Butiran Aset](images/07-asset-details.png)

### Laporan ROI Kenderaan
Navigasi ke **Finance > Reports > Profitability** untuk melihat:
- **Revenue**: Jumlah pendapatan sewaan.
- **Expenses**: Semua kos (penyelenggaraan, insurans, pembiayaan/loan).
- **Net Profit/Loss**: Pendapatan tolak Perbelanjaan tolak Susut Nilai.
- **ROI %**: Pulangan sebenar ke atas pelaburan asal anda.

Gunakan data ini untuk memutuskan sama ada untuk terus membaiki kenderaan atau menjualnya sebelum ia menjadi beban.

---

## Panduan Berkaitan
*   [01-orgadmin-quickstart.md](01-orgadmin-quickstart.md)
*   [14-asset-financing.md](14-asset-financing.md)
