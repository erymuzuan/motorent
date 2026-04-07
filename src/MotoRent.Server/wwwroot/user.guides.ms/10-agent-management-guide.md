# Panduan Pengurusan Ejen: Rakan Kongsi Keuntungan

Dalam pasaran kereta sewa Malaysia, pelanggan 'walk-in' memang bagus, tetapi jumlah tempahan yang konsisten sering datang dari rakan kongsi tempatan: hotel, pemandu pelancong, dan ejen jalanan (ulat tiket).

Walau bagaimanapun, menjejaki siapa yang membawa pelanggan mana dan mengira komisen secara manual pada hujung bulan sering menyebabkan tempahan "hilang", pertikaian, dan kelewatan pembayaran.

**Pengurusan Ejen JaleOS** memformalkan perkongsian ini. Ia menjejaki setiap tempahan yang dirujuk dari awal hingga akhir dan memastikan ejen anda dibayar dengan tepat dan tepat pada masanya melalui sistem Laci Tunai (Till), membina kepercayaan dan menggalakkan mereka membawa lebih banyak perniagaan kepada anda.

![Gambaran Keseluruhan Pengurusan Ejen](images/10-agent-hero.webp)

## Cara Ia Berfungsi dalam 30 Saat

1.  **Daftar Rakan Kongsi**: Tambah hotel atau individu sebagai "Ejen" dan tetapkan kadar komisen lalai (default) mereka.
2.  **Pautkan Tempahan**: Semasa 'check-in' pelanggan yang dirujuk, pilih Ejen daripada menu lungsur (dropdown).
3.  **Jejak Automatik**: JaleOS mengira komisen dan menahannya dalam status "Pending" sehingga sewaan selesai.
4.  **Pembayaran (Payout)**: Setelah sewaan selesai, komisen menjadi "Eligible" dan boleh dibayar terus dari Laci Juruwang (Cashier Till).

---

## Kisah: Ahmad dan Rangkaian Hotel Pantai Cenang

Ahmad menguruskan kedai sewa di Pantai Cenang. Beliau bekerjasama dengan 5 buah hotel tempatan yang mengesyorkan keretanya kepada tetamu mereka.

*   **Masalah**: Sebelum ini, penyambut tetamu hotel akan WhatsApp Ahmad untuk menempah kereta. Ahmad merekodkan ini dalam buku nota. Pada hujung bulan, pihak hotel mendakwa mereka menghantar 20 pelanggan, tetapi rekod Ahmad hanya menunjukkan 15. Pertengkaran mengenai komisen yang tidak dibayar menjejaskan hubungannya.
*   **Penyelesaian**: Ahmad mendaftarkan setiap hotel sebagai Ejen dalam JaleOS dengan komisen tetap RM50 untuk setiap tempahan. Setiap kali tetamu hotel menyewa kereta, stafnya akan memilih hotel tersebut dalam proses Check-In.
*   **Hasil**: Pihak hotel dapat melihat mereka dibayar dengan tepat dan segera. Kepercayaan meningkat, dan dalam masa tiga bulan, tempahan rujukan Ahmad meningkat dua kali ganda kerana penyambut tetamu lebih suka bekerja dengan sistemnya yang telus.

---

## Persediaan Pantas: Mendaftar Ejen

1. Navigasi ke **Agents** di bar sisi (sidebar).
2. Klik **Add Agent**.
3. Masukkan butiran ejen:
   - **Name**: Nama individu atau perniagaan (cth., "Pelangi Beach Resort").
   - **Contact Info**: Nombor telefon dan e-mel.
   - **Commission Type**: Peratusan (cth., 10%) atau jumlah tetap (cth., RM50) setiap tempahan.
   - **Default Rate**: Komisen standard yang anda bayar kepada ejen ini.
4. Klik **Save**.

![Senarai Ejen](images/10-agents-list.webp)

## Operasi Harian: Menjejaki Komisen

Apabila pelanggan tiba dari ejen, hanya pilih **Ejen** dari menu lungsur (dropdown) semasa proses **Check-In**. JaleOS akan menguruskan selebihnya melalui aliran kerja yang teratur:

### Kitaran Hayat Komisen
- **Pending**: Tempahan sedang aktif. Komisen telah dikira tetapi belum wajib dibayar.
- **Eligible**: Sewaan telah selesai (check-out). Komisen kini wajib dibayar kepada ejen.
- **Paid**: Komisen telah direkodkan sebagai telah dibayar.

![Komisen Ejen](images/10-agent-commissions.webp)

### Memproses Pembayaran (Payouts)
Untuk membayar ejen, anda menggunakan Till harian:
1. Pergi ke halaman **Till** (pastikan sesi anda dibuka).
2. Klik **Record Payout**.
3. Pilih **Agent Commission**.
4. Pilih ejen tertentu; sistem akan menunjukkan semua komisen mereka yang berstatus "Eligible".
5. Masukkan jumlah yang dibayar dan sahkan.

Status komisen akan serta-merta dikemas kini kepada **Paid**, dan pembayaran (payout) akan direkodkan dalam penyesuaian (reconciliation) Till harian anda.

---

## Laporan Prestasi Ejen

Navigasi ke **Finance > Reports > Agent Performance** untuk melihat:
- Jumlah tempahan yang dijana oleh setiap ejen.
- Jumlah pendapatan yang dibawa masuk.
- Jumlah komisen yang telah dibayar vs. baki tertunggak.

Gunakan data ini untuk mengenal pasti rakan kongsi terbaik anda dan mungkin menawarkan mereka kadar komisen yang lebih tinggi untuk menggalakkan lebih banyak rujukan!

---

## Panduan Berkaitan
*   [01-orgadmin-quickstart.md](01-orgadmin-quickstart.md)
*   [04-shopmanager-quickstart.md](04-shopmanager-quickstart.md)
*   [08-cashier-till-guide.md](08-cashier-till-guide.md)
