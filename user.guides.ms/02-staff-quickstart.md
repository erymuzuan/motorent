# Panduan Permulaan Pantas JaleOS - Kakitangan

Panduan ini merangkumi operasi harian untuk kakitangan kaunter hadapan. Matlamat utama anda adalah untuk menyediakan pengalaman 'check-in' dan 'check-out' yang lancar bagi penyewa.

![Gambaran Keseluruhan Kakitangan](images/02-staff-hero.webp)

## 1. Papan Pemuka Kakitangan

Papan Pemuka Kakitangan (Staff Dashboard) direka untuk tindakan pantas:
- **Check-In Baru**: Mulakan sesi sewaan baru.
- **Proses Check-Out**: Proses kenderaan yang dipulangkan.
- **Sewaan Aktif**: Lihat semua kenderaan yang sedang disewa.
- **Tertunggak (Overdue)**: Lihat dengan cepat sewaan yang terlepas waktu pemulangan.
- **Pemulangan Hari Ini (Due Today)**: Senarai keutamaan pemulangan yang dijangka hari ini.

![Papan Pemuka Kakitangan](images/09-staff-dashboard.webp)

## 2. Proses Check-In (Wizard)

Apabila pelanggan tiba, gunakan butang **Check-In Baru**. Ini mengikuti wizard 5-langkah:
1. **Butiran Sewaan**: Pilih cawangan, jenis tempoh (Harian atau Selang Tetap), dan tarikh.
2. **Pendaftaran Pelanggan**: Cari pelanggan sedia ada atau daftar pelanggan baru. Gunakan **Ciri OCR** (dikuasakan oleh AI Gemini) untuk mengekstrak nama, nombor ID, dan kewarganegaraan secara automatik daripada ID atau Pasport mereka.
3. **Kenderaan & Pilihan**: Pilih kenderaan yang tersedia, pakej insurans, dan aksesori.
4. **Perjanjian & Tandatangan**: Bacakan terma kepada pelanggan. Mereka mesti menandatangani terus pada skrin.
5. **Pembayaran & Semakan**: Rekodkan pembayaran sewa dan deposit keselamatan (Tunai atau Kad).

![Check-In Wizard](images/04-checkin.webp)

## 3. Memproses Pemulangan (Check-Out)

Apabila kenderaan dipulangkan:
1. Cari rekod sewaan dalam senarai **Pemulangan Hari Ini** atau melalui **Proses Check-Out**.
2. **Semakan Standard**: Rekodkan perbatuan (mileage) pemulangan dan tarikh tamat.
3. **Pemeriksaan Kerosakan**: Periksa kenderaan untuk sebarang calar atau kerosakan baru. Jika kerosakan ditemui, buat **Laporan Kerosakan** dengan foto/gambar dan anggaran kos.
4. **Bayaran Balik Deposit**: Sistem akan mengira jika ada sebarang amaun yang perlu ditolak daripada deposit untuk kerosakan atau pemulangan lewat. Proses bayaran balik kepada pelanggan.

## 4. Pengurusan Pelanggan

Gunakan menu **Customers > Renters** untuk mengurus profil penyewa. Anda boleh mengemas kini maklumat hubungan, mengesahkan dokumen, dan melihat sejarah sewaan mereka.

![Penyewa](images/07-renters.webp)

## 5. Melaporkan Kemalangan

Jika pelanggan melaporkan kemalangan semasa tempoh sewaan mereka:
1. Pergi ke **Accidents** dan klik **Report New Accident**.
2. Dokumenkan tarikh, lokasi, pihak yang terlibat, dan anggaran kos dalam tab yang berkaitan.
3. Muat naik sebarang dokumen sokongan atau foto.

---
*Tip Operasi: Sentiasa sahkan dokumen asal (Pasport/Lesen) berbanding data yang diekstrak oleh ciri OCR untuk ketepatan.*
