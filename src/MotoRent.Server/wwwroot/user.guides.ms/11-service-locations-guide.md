# Panduan Lokasi Servis: Mengurus 'Drop-off' dan Pemindahan

Di pusat pelancongan seperti Sabah dan Langkawi, fleksibiliti adalah kelebihan daya saing yang besar. Pelancong sering mahu mengambil kereta di pejabat bandar dan memulangkannya di lapangan terbang (seperti KKIA atau LGK) sebelum penerbangan mereka.

Jika anda tidak menawarkan perkhidmatan ini, anda mungkin kehilangan pelanggan. Tetapi jika anda menawarkannya tanpa sistem yang teratur, staf anda akan membazirkan masa menjejak di mana kereta diletakkan, dan anda terlepas peluang untuk mengenakan **Caj Pemulangan (Drop-off Fees)** yang sepatutnya.

**Lokasi Servis JaleOS** mengubah cabaran logistik ini menjadi sumber pendapatan yang lancar. Ia menjejaki lokasi tepat setiap kenderaan dalam fleet anda dan secara automatik mengenakan caj 'sehhala' (one-way) apabila pelanggan memulangkan kenderaan di lokasi yang berbeza.

![Gambaran Keseluruhan Lokasi Servis](images/11-service-locations-hero.webp)

## Cara Ia Berfungsi dalam 30 Saat

1.  **Tetapkan Lokasi**: Tambahkan kedai anda, hotel rakan kongsi, lapangan terbang, atau jeti ke dalam sistem.
2.  **Tetapkan Caj Pemulangan**: Konfigurasi caj tambahan jika kereta dipulangkan ke lokasi tertentu (cth., RM50 untuk pemulangan di Lapangan Terbang).
3.  **Pilih semasa Check-in**: Semasa menyewa kenderaan, pilih Lokasi Pemulangan yang dijangka; caj akan ditambah pada bil secara automatik.
4.  **Jejak Inventori**: Papan Pemuka Fleet sentiasa menunjukkan lokasi fizikal semasa setiap kereta.

---

## Kisah: Pemulangan di Lapangan Terbang Kota Kinabalu

Ahmad menguruskan fleet sewa di Kota Kinabalu. Sebuah keluarga pelancong menyewa Perodua Alza dari pejabat bandarnya untuk percutian 5 hari ke Kundasang.

*   **Senario**: Keluarga tersebut bertanya jika mereka boleh memulangkan kereta terus di Lapangan Terbang Antarabangsa Kota Kinabalu (BKI) pada pukul 6:00 pagi pada hari terakhir mereka untuk mengejar penerbangan awal.
*   **Tindakan**: Semasa proses Check-In, staf Ahmad memilih "Pejabat Bandar KK" sebagai Lokasi Pengambilan dan "Lapangan Terbang BKI" sebagai Lokasi Pemulangan.
*   **Hasil**: JaleOS serta-merta menambah **Caj Pemulangan RM50** yang telah dikonfigurasi ke dalam jumlah bil. Apabila keluarga tersebut meninggalkan kereta di lapangan terbang, pemandu Ahmad pergi mengambilnya, dan sistem secara automatik mengemas kini lokasi Alza kembali ke pejabat bandar setelah ia diambil. Ahmad mendapat tempahan tersebut, memberikan perkhidmatan terbaik, dan membuat keuntungan tambahan RM50 untuk menampung kos pengambilan.

---

## Persediaan Pantas: Mengkonfigurasi Lokasi

1. Navigasi ke **Settings > Service Locations**.
2. Klik **Add Location**.
3. Masukkan butiran:
   - **Name**: cth., "Lapangan Terbang Langkawi", "Pejabat Utama Pulau Pinang".
   - **Type**: Pilih Shop (Kedai), Hotel, Airport (Lapangan Terbang), atau Other (Lain-lain).
   - **Drop-off Fee**: Tetapkan caj tambahan (cth., 50) jika pelanggan memulangkan kenderaan di sini berbanding lokasi pengambilan.
4. Klik **Save**.

![Senarai Lokasi Servis](images/11-service-locations.webp)

## Operasi Harian

### Semasa Check-In
Apabila memproses sewaan baharu melalui proses Check-In, anda boleh menetapkan kedua-dua tempat pengambilan dan pemulangan yang dijangka.

Jika Lokasi Pemulangan berbeza daripada Lokasi Pengambilan, **Caj Pemulangan** akan ditambah secara automatik pada jumlah sewaan di bawah "Caj Tambahan".

![Pemilihan Lokasi](images/11-location-selection.webp)

### Menguruskan Kerosakan (Pertukaran Kenderaan)
Lokasi Servis bukan hanya untuk lapangan terbang. Bayangkan kereta pelanggan rosak di hotel rakan kongsi di Batu Ferringhi:
1. Anda menghantar kereta ganti ke hotel tersebut.
2. Anda boleh mengemas kini lokasi kereta yang rosak dalam JaleOS kepada "Hotel X".
3. Sistem menjejaki bahawa kereta yang rosak itu diletakkan di hotel sehingga mekanik anda pergi mengambilnya, memastikan anda tidak akan kehilangan jejak kenderaan.

### Penjejakan Papan Pemuka Fleet
**Papan Pemuka Fleet** menunjukkan tag `Location` semasa untuk setiap kenderaan, memudahkan anda melihat di mana inventori anda diedarkan secara fizikal di seluruh rangkaian kedai, hotel, dan lapangan terbang anda.

---

## Panduan Berkaitan
*   [01-orgadmin-quickstart.md](01-orgadmin-quickstart.md)
*   [04-shopmanager-quickstart.md](04-shopmanager-quickstart.md)