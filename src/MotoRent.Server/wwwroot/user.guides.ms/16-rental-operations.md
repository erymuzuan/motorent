# Panduan Operasi Sewaan: Pengalaman yang Lancar

Bagi kebanyakan perniagaan sewa kereta di Malaysia, kaunter hadapan (front desk) adalah cabaran terbesar. Kemasukan data secara manual, perjanjian kertas, dan pertikaian kerosakan "dia kata, saya kata" menyebabkan barisan gilir yang panjang, pelancong yang kecewa, dan kerugian wang untuk pembaikan.

**JaleOS** mendigitalkan keseluruhan proses sewaan. Dengan ciri-ciri seperti eKYC (Pengecaman Aksara Optik), tandatangan digital, dan penjejakan kerosakan bergambar yang ketat, apa yang dulunya memeningkan kepala selama 15 minit kini menjadi pengalaman profesional selama 3 minit sahaja.

![Gambaran Keseluruhan Operasi Sewaan](images/16-rental-operations-hero.webp)

## Cara Ia Berfungsi dalam 30 Saat

1.  **Tempahan Dalam Talian (Online Booking)**: Pelancong melayari PWA anda, memilih tarikh/kenderaan, menambah insurans, dan membayar deposit tempahan secara terus.
2.  **Daftar Masuk Pantas (Check-In)**: Gunakan kamera untuk mengimbas pasport atau IC. Sistem mengekstrak butiran secara automatik.
3.  **Perjanjian Digital**: Pelanggan menandatangani perjanjian sewa terus pada tablet atau telefon pintar anda.
4.  **Penjejakan Kerosakan**: Staf mengambil gambar "Sebelum" dan "Selepas" kenderaan, menghapuskan pertikaian mengenai calar atau kemek.
5.  **Pengebilan Bersepadu**: Kira caj lewat, hari tambahan, atau caj pemulangan (drop-off) secara automatik semasa Daftar Keluar (Check-Out).

---

## 1. Tempahan Dalam Talian (Online Booking)

Sebelum pelanggan tiba di kedai anda, JaleOS sudah bekerja untuk anda. **Portal Pelancong (PWA)** terbina dalam bertindak sebagai enjin tempahan dalam talian 24/7 anda.

*   **Carian (Browsing)**: Pelanggan melihat ketersediaan sebenar (real-time) fleet anda (motosikal, kereta, van).
*   **Deposit Tempahan**: Daripada hanya menempah kereta dan mengharapkan mereka muncul, anda boleh mewajibkan **Deposit Tempahan** (cth., RM50 atau 20% daripada jumlah keseluruhan) yang dibayar dengan selamat melalui FPX, DuitNow QR, atau Kad Kredit.
*   **Penyerahan Lancar**: Memandangkan pelanggan telah mengisi butiran mereka dan membayar deposit secara dalam talian, staf anda hanya perlu mengesahkan identiti mereka dan menyerahkan kunci.

![Borang Tempahan Dalam Talian](images/16-online-booking.webp)

---

## 2. Kisah: Check-In Petang Jumaat yang Sibuk

Rizal bekerja di kaunter hadapan di KK Car Rentals. Pada petang Jumaat yang sibuk, tiga keluarga tiba pada masa yang sama untuk mengambil kenderaan yang telah ditempah.

*   **Masalah**: Pada masa lalu, Rizal terpaksa menaip butiran pasport semua orang secara manual, membuat salinan lesen mereka, dan mencetak tiga salinan kontrak kertas untuk mereka tandatangani. Pelanggan akan menunggu selama 20 minit hanya untuk mendapatkan kunci mereka.
*   **Penyelesaian**: Rizal menggunakan Wizard Check-In JaleOS pada tabletnya. Dia mengambil gambar pasport pelanggan pertama, dan sistem eKYC serta-merta mengisi butirannya. Dia memilih kereta yang ditempah, menambah jualan (up-sell) untuk tempat duduk kanak-kanak, dan menyerahkan tablet kepada pelanggan untuk ditandatangani secara digital.
*   **Hasil**: Pelanggan membayar baki deposit, mengambil kunci, dan selesai dalam masa kurang dari 3 minit. Barisan gilir hilang, dan pengurus Rizal gembira dengan operasi yang lancar.

---

## 3. Wizard Daftar Masuk (Check-In)

Proses Check-In ialah wizard 5 langkah berpandu yang direka untuk mengelakkan staf daripada terlepas langkah-langkah penting.

### Langkah 1: Penyewa (eKYC)
Cari penyewa sedia ada atau cipta penyewa baharu. Semasa mencipta penyewa baharu, gunakan **Ciri OCR** (dikuasakan oleh Gemini AI). Hanya ambil gambar Pasport atau MyKad, dan sistem akan mengekstrak Nama, Nombor ID, dan Kewarganegaraan secara automatik.

![Pilihan Penyewa eKYC](images/16-checkin-ekyc.webp)

### Langkah 2 & 3: Kenderaan dan Konfigurasi
Pilih kenderaan yang tersedia. Dalam langkah Konfigurasi, anda boleh:
- Melaraskan tarikh dan masa sewaan.
- Memilih **Pakej Insurans** (cth., Asas, Perlindungan Penuh).
- Menambah **Aksesori** (cth., Topi Keledar, Pemegang Telefon) yang akan mengemas kini kadar harian secara automatik.

![Add-ons Check-In](images/16-checkin-addons.webp)

### Langkah 4 & 5: Deposit dan Perjanjian
Kutip deposit keselamatan (Tunai atau Pra-kebenaran Kad) dan rekodkannya dalam Till. Akhir sekali, pelanggan menyemak perjanjian sewa yang dijana dan menandatanganinya terus pada skrin.

---

## 4. Proses Daftar Keluar (Check-Out): Pengebilan dan Kerosakan

Apabila kenderaan dipulangkan, proses Check-Out memastikan semuanya dikira sebelum deposit dilepaskan.

### Penjejakan Kerosakan dan Kebersihan
Langkah paling kritikal ialah **Pemeriksaan (Inspection)**. Staf mesti menyemak keadaan kenderaan berbanding gambar "Sebelum" yang diambil semasa Check-In.
- Jika kemek baharu ditemui, staf membuat **Laporan Kerosakan** beserta gambar.
- Kos pembaikan boleh terus ditolak daripada deposit keselamatan.
- Masalah kebersihan dan tahap bahan api juga direkodkan di sini.

### Pengebilan Automatik
Dialog Check-Out secara automatik mengira penyelesaian akhir:
- **Caj Lewat (Late Fees)**: Ditambah secara automatik jika kenderaan dipulangkan melepasi tempoh tangguh.
- **Kekurangan Bahan Api**: Caj pelanggan jika paras bahan api lebih rendah daripada semasa ia diambil.
- **Caj Pemulangan (Drop-off Fees)**: Jika kenderaan dipulangkan ke Lokasi Servis yang berbeza.

![Pengebilan Check-Out](images/16-checkout-billing.webp)

Setelah diselesaikan, sistem memproses pemulangan deposit dan menjana Resit Penyelesaian (Settlement Receipt) akhir.

---

## Panduan Berkaitan
*   [02-staff-quickstart.md](02-staff-quickstart.md)
*   [08-cashier-till-guide.md](08-cashier-till-guide.md)
*   [15-accidents-and-fines.md](15-accidents-and-fines.md)
