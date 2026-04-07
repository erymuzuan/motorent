# Panduan Laci Wang Tunai (Cashier Till)

Panduan ini merangkumi sistem laci wang (till) untuk menguruskan operasi laci tunai, pembayaran, pembayaran keluar (payouts), dan penyelarasan syif.

## Gambaran Keseluruhan

Sistem laci wang (till) adalah pusat bagi semua transaksi kewangan semasa syif anda. Sebelum memproses sebarang sewaan, anda mesti membuka sesi laci wang. Semua pembayaran, bayaran balik, dan pembayaran keluar dijejaki melalui laci wang anda.

**Konsep Utama:**
- **Sesi Laci Wang (Till Session)** - Sesi laci tunai peribadi anda untuk hari tersebut.
- **Float Pembukaan (Opening Float)** - Jumlah tunai permulaan dalam laci anda.
- **Tunai Masuk (Cash In)** - Wang yang diterima (pembayaran sewa, deposit).
- **Tunai Keluar (Cash Out)** - Wang yang dibayar keluar (bayaran balik, bahan api, komisen).
- **Penyelarasan (Reconciliation)** - Mengimbangi laci anda pada akhir syif.

## Bermula

### Membuka Laci Wang Anda

Anda mesti membuka laci wang sebelum memproses sebarang 'check-in' atau 'check-out'.

1. Klik butang **Buka Laci Wang (Open Till)** di bahagian atas (atau navigasi ke `/staff/till`).
2. Pilih **Cawangan (Shop)** tempat anda bekerja (jika anda mempunyai akses ke pelbagai cawangan).
3. Masukkan jumlah **Float Pembukaan** (tunai permulaan anda).
4. Tambah sebarang **Nota** (pilihan).
5. Klik **Buka Sesi (Open Session)**.

**Peraturan Penting:**
- Anda hanya boleh mempunyai satu laci wang yang aktif bagi setiap cawangan setiap hari.
- Anda mesti menutup sesi semasa sebelum membuka sesi baru.
- Cawangan dikunci pada laci wang anda - semua transaksi melalui laci tersebut.

### Papan Pemuka Laci Wang (Till Dashboard)

Setelah laci wang anda dibuka, papan pemuka akan menunjukkan:

| Bahagian | Penerangan |
|---------|-------------|
| **Info Sesi** | Nama anda, cawangan, masa mula |
| **Tunai Masuk** | Jumlah wang yang diterima dalam sesi ini |
| **Tunai Keluar** | Jumlah wang yang dibayar keluar dalam sesi ini |
| **Baki Jangkaan** | Float pembukaan + Tunai Masuk - Tunai Keluar |
| **Tindakan Pantas** | Payout, Drop, Top Up, Close |
| **Transaksi Terkini** | 10 transaksi terakhir |

## Merekod Transaksi

### Transaksi Automatik

Ini direkodkan secara automatik apabila anda memproses sewaan:

| Jenis Transaksi | Bila Ia Berlaku |
|-----------------|-----------------|
| **Pembayaran Sewa** | Pelanggan membayar sewa semasa 'check-in' |
| **Deposit Keselamatan** | Pelanggan membayar deposit tunai |
| **Pembayaran Kad** | Pelanggan membayar dengan kad (dijejaki, tidak menjejaskan tunai) |
| **Touch 'n Go / FPX** | Pelanggan membayar melalui e-dompet atau perbankan dalam talian (dijejaki, tidak menjejaskan tunai) |
| **Bayaran Balik Deposit** | Bayaran balik tunai semasa 'check-out' |
| **Caj Kerosakan** | Pelanggan membayar untuk kerosakan |

### Pembayaran Keluar Manual (Manual Payouts)

Gunakan butang **Rekod Payout (Record Payout)** untuk wang yang keluar dari laci anda.

#### Tuntutan Bahan Api (Fuel Reimbursement)
Apabila anda membayar balik pelanggan untuk bahan api:
1. Klik **Record Payout**
2. Pilih **Fuel Reimbursement**
3. Masukkan jumlah
4. Masukkan nama pelanggan/penerima
5. Masukkan nombor resit (jika ada)
6. Tambah nota (cth., "Isi minyak penuh untuk sewaan #123")
7. Klik **Save**

#### Komisen Ejen (Agent Commission)
Apabila membayar komisen kepada ejen yang membawa pelanggan:
1. Klik **Record Payout**
2. Pilih **Agent Commission**
3. Masukkan jumlah
4. Masukkan nama ejen
5. Rujuk tempahan atau sewaan
6. Klik **Save**

#### Wang Runcit (Petty Cash)
Untuk perbelanjaan pelbagai:
1. Klik **Record Payout**
2. Pilih **Petty Cash**
3. Masukkan jumlah
4. Nyatakan tujuannya (cth., "Bekalan pejabat")
5. Masukkan nombor resit
6. Klik **Save**

**Sentiasa simpan resit untuk setiap pembayaran keluar!**

## Menerima Pembayaran dari Laci Wang

Halaman Laci Wang mempunyai bahagian **Pembayaran Pantas (Quick Payments)** untuk menerima pembayaran secara terus tanpa melalui aliran 'check-in' yang penuh.

### Butang Pembayaran Pantas

| Butang | Kegunaan |
|--------|---------|
| **Pembayaran Sewa** | Pembayaran tambahan untuk sewaan sedia ada |
| **Deposit** | Deposit keselamatan untuk sewaan aktif |
| **Deposit Tempahan** | Deposit untuk tempahan awal (advance bookings) |

### Merekod Pembayaran Sewa

Untuk situasi di mana pelanggan perlu membayar lebih pada sewaan sedia ada:

1. Di halaman Laci Wang anda, klik **Rental Payment**
2. Cari sewaan mengikut ID atau nama pelanggan
3. Pilih sewaan daripada keputusan carian
4. Masukkan jumlah pembayaran
5. Pilih kaedah pembayaran (Tunai, Kad, Touch 'n Go, FPX, Pindahan Bank)
6. Klik **Record Payment**
7. Pembayaran akan muncul dalam transaksi anda

### Merekod Deposit Keselamatan

Untuk mengutip deposit pada sewaan aktif:

1. Klik **Deposit**
2. Cari sewaan mengikut ID
3. Sahkan butiran sewaan
4. Masukkan jumlah deposit dan jenis (Tunai atau Kad Pre-auth)
5. Klik **Record Deposit**

### Merekod Deposit Tempahan

Apabila pelanggan membayar deposit untuk tempahan awal:

1. Klik **Booking Deposit**
2. Masukkan rujukan tempahan (kod 6 aksara seperti "ABC123")
3. Atau cari mengikut nama pelanggan
4. Sahkan butiran tempahan (tarikh, deposit yang diperlukan)
5. Masukkan jumlah deposit (tidak boleh melebihi baki yang perlu dibayar)
6. Pilih kaedah pembayaran
7. Klik **Record Payment**
8. Resit akan dijana secara automatik untuk pelanggan

**Nota**: Status pembayaran tempahan dikemas kini secara automatik:
- **Belum Bayar (Unpaid)** → **Dibayar Sebahagian (Partially Paid)** → **Dibayar Sepenuhnya (Fully Paid)**

### Cash Drop

Apabila anda mempunyai terlalu banyak tunai dalam laci dan perlu memindahkannya ke dalam peti besi (safe):

1. Klik **Cash Drop**
2. Masukkan jumlah yang dikeluarkan
3. Tambah nota (cth., "Simpanan petang ke peti besi")
4. Klik **Confirm**

Ini mengurangkan baki jangkaan anda tetapi dijejaki secara berasingan.

### Top Up

Jika anda memerlukan lebih banyak tunai dalam laci (cth., untuk wang kecil/tukar):

1. Klik **Top Up**
2. Masukkan jumlah yang ditambah
3. Tambah nota (cth., "Wang tukar dari peti besi")
4. Klik **Confirm**

Ini meningkatkan baki jangkaan anda.

## Memproses Pembayaran semasa Check-In

Apabila memproses sewaan baru:

1. **Buka laci wang anda terlebih dahulu** - 'Check-in' tidak akan berfungsi tanpa laci wang yang aktif.
2. Lengkapkan langkah-langkah 'check-in' (pelanggan, kenderaan, tarikh, dll.)
3. Pada langkah pembayaran:
   - **Tunai**: Dimasukkan ke dalam laci wang anda secara automatik.
   - **Kad**: Dijejaki tetapi tidak menjejaskan baki tunai anda.
   - **Touch 'n Go/FPX/Pindahan Bank**: Dijejaki sebagai pembayaran elektronik.
4. Selepas 'check-in', resit akan dijana dan boleh dicetak.

### Pembayaran Berasingan (Split Payments)

Pelanggan boleh membayar dengan pelbagai kaedah:
- Sebahagian tunai, sebahagian kad.
- Mata wang yang berbeza untuk tunai (dengan kadar pertukaran).

Sistem menjejaki setiap pembayaran secara berasingan.

### Tunai Pelbagai Mata Wang

Untuk pembayaran tunai mata wang asing:
1. Pilih mata wang (USD, EUR, GBP, dll.).
2. Masukkan jumlah dalam mata wang tersebut.
3. Kadar pertukaran menukarkannya kepada MYR.
4. Laci wang anda menjejaki nilai setara dalam MYR.

Mata wang yang disokong: MYR, USD, EUR, GBP, SGD, CNY, JPY, AUD.

## Memproses Bayaran Balik semasa Check-Out

Apabila pelanggan memulangkan kenderaan:

1. Sistem mengira:
   - Deposit yang dipegang.
   - Sebarang caj tambahan (hari tambahan, kerosakan, dll.).
   - Jumlah bayaran balik (deposit tolak caj).
2. Jika bayaran balik tunai:
   - Direkodkan secara automatik sebagai **Deposit Refund** dalam laci wang anda.
   - Mengurangkan baki jangkaan anda.
3. Cetak resit penyelesaian (settlement receipt).

## Resit

Resit dijana secara automatik untuk:

| Jenis Resit | Bila Ia Dijana |
|--------------|----------------|
| **Resit Check-In** | Selepas 'check-in' berjaya |
| **Resit Penyelesaian** | Selepas 'check-out' |
| **Resit Deposit Tempahan** | Apabila deposit tempahan dibayar |

### Ciri-ciri Resit
- **Cetak**: Klik butang Cetak untuk membuka dialog cetakan.
- **Cetak Semula**: Cari resit dalam `/finance/receipts` untuk mencetak semula.
- **Batal (Void)**: Pengurus boleh membatalkan resit dengan menyatakan alasan.

### Format Nombor Resit
`RCP-YYMMDD-XXXXX` (cth., RCP-260117-00042)

## Menutup Laci Wang Anda

Pada akhir syif anda:

1. Klik **Tutup Syif (Close Shift)** pada papan pemuka laci wang anda.
2. Kira tunai fizikal anda.
3. Masukkan jumlah **Tunai Sebenar (Actual Cash)**.
4. Sistem menunjukkan:
   - **Jangkaan (Expected)**: Apa yang sepatutnya ada dalam laci.
   - **Sebenar (Actual)**: Apa yang anda kira.
   - **Varian (Variance)**: Perbezaan (kurang atau lebih).
5. Jika ada varian:
   - Tambah nota menjelaskan perbezaan tersebut.
   - Akui (acknowledge) varian tersebut.
6. Klik **Tutup Sesi (Close Session)**.

### Pengendalian Varian

| Varian | Status | Tindakan |
|----------|--------|--------|
| Tiada (0) | Ditutup | Sesi ditutup secara normal |
| Kurang (-) | Ditutup dengan Varian | Pengurus akan menyemak |
| Lebih (+) | Ditutup dengan Varian | Pengurus akan menyemak |

**Tips untuk Penyelarasan yang Tepat:**
- Kira tunai dengan teliti, dua kali jika perlu.
- Periksa jika ada nota atau syiling yang terselip.
- Semak semula transaksi jika varian tidak dijangka.
- Laporkan sebarang percanggahan dengan segera.

## Melihat Sejarah

### Sejarah Transaksi
Pada halaman laci wang anda, klik **Lihat Sejarah (View History)** untuk melihat:
- Semua transaksi bagi sesi semasa.
- Tapis mengikut jenis (Tunai Masuk, Tunai Keluar).
- Cari mengikut huraian.

### Sesi Terdahulu
Navigasi ke `/staff/till` dan klik **Session History** untuk melihat:
- Sesi sebelumnya dengan statusnya.
- Jumlah tunai masuk/keluar bagi setiap sesi.
- Jumlah varian.
- Status pengesahan.

## Aliran Kerja Harian

### Permulaan Syif
1. [ ] Log masuk ke sistem.
2. [ ] Klik **Open Till** di bahagian atas.
3. [ ] Pilih cawangan anda.
4. [ ] Kira dan masukkan float pembukaan anda.
5. [ ] Sahkan float sepadan dengan apa yang ada dalam laci.

### Semasa Syif Anda
1. [ ] Proses 'check-in' (pembayaran direkod secara automatik).
2. [ ] Proses 'check-out' (bayaran balik direkod secara automatik).
3. [ ] Rekodkan sebarang pembayaran keluar dengan resit.
4. [ ] Lakukan 'cash drop' jika laci terlalu penuh.
5. [ ] Pastikan resit transaksi tersusun.

### Akhir Syif
1. [ ] Semak senarai transaksi anda.
2. [ ] Kira tunai fizikal anda.
3. [ ] Klik **Close Shift**.
4. [ ] Masukkan jumlah tunai sebenar.
5. [ ] Jelaskan sebarang varian.
6. [ ] Tutup sesi.
7. [ ] Simpan tunai dengan selamat.

## Penyelesaian Masalah

### Ralat "No Active Till"
Anda cuba memproses sewaan tanpa laci wang yang dibuka.
- **Penyelesaian**: Buka laci wang anda terlebih dahulu, kemudian cuba lagi.

### Tidak Boleh Membuka Laci Wang
Anda mungkin sudah mempunyai sesi aktif untuk cawangan tersebut.
- **Penyelesaian**: Tutup sesi sedia ada terlebih dahulu.

### Varian Semasa Penutupan
Tunai yang dikira tidak sepadan dengan jangkaan.
- **Penyelesaian**:
  1. Kira semula wang tunai.
  2. Semak semula transaksi untuk kesilapan.
  3. Periksa jika ada pembayaran keluar yang terlepas.
  4. Nota varian tersebut dan tutup sesi.

### Resit Tidak Boleh Dicetak
- **Penyelesaian**:
  1. Periksa sambungan pencetak.
  2. Cuba pilihan cetak semula dari `/finance/receipts`.
  3. Hubungi pengurus jika isu berterusan.

## Amalan Terbaik

1. **Sentiasa buka laci wang sebelum memulakan kerja** - Jangan proses sewaan tanpa laci wang.
2. **Dapatkan resit untuk semua pembayaran keluar** - Pastikan ia tersusun.
3. **Lakukan 'cash drop' secara berkala** - Jangan simpan terlalu banyak tunai dalam laci.
4. **Kira dengan teliti semasa penutupan** - Jangan terburu-buru.
5. **Laporkan isu dengan segera** - Jangan tunggu sehingga akhir hari.
6. **Simpan nota** - Dokumentasikan apa-apa yang luar biasa.

## Rujukan Pantas

| Tindakan | Lokasi |
|--------|----------|
| Buka Laci Wang | Butang atas atau `/staff/till` |
| Lihat Laci Wang | `/staff/till` |
| Rekod Pembayaran | Halaman Till > Rental Payment |
| Rekod Deposit | Halaman Till > Deposit |
| Deposit Tempahan | Halaman Till > Booking Deposit |
| Rekod Payout | Halaman Till > Fuel/Agent/Petty Cash |
| Cash Drop | Halaman Till > Cash Drop |
| Tutup Laci Wang | Halaman Till > Close Shift |
| Lihat Resit | `/finance/receipts` |
| Sejarah Sesi | Halaman Till > View History |

## Perlukan Bantuan?

- **Isu Laci Wang**: Hubungi Pengurus Cawangan anda.
- **Ralat Sistem**: Hubungi Sokongan IT.
- **Soalan Mengenai Varian**: Berbincang dengan Pengurus sebelum menutup sesi.

---

*JaleOS - Sistem Pengurusan Sewaan Kenderaan*
