# Panduan Permulaan Pantas JaleOS - Super Admin

Selamat datang ke JaleOS! Panduan ini merangkumi peranan Super Admin (Pentadbir Platform).

![Penyamaran Pengguna](images/01-impersonate.webp)

## Peranan Anda

Sebagai Super Admin, anda menguruskan keseluruhan platform JaleOS:
- Mengurus Organisasi (tenants)
- Mengurus Pengguna Sistem
- Menyamar sebagai pengguna (impersonate) untuk sokongan
- Mengendalikan Jemputan Pendaftaran (Registration Invites)
- Melihat Log Sistem
- Mengkonfigurasi Tetapan Global

> **Nota:** Super Admin adalah peranan peringkat platform. Tanpa ciri penyamaran, anda tidak akan melihat menu penyewa (Sewaan, Fleet, dll.). Gunakan ciri penyamaran untuk mengakses ciri khusus penyewa.

## Mengakses Ciri Super Admin

### Navigasi

Halaman Super Admin diakses melalui menu lungsur (dropdown) pengguna:
1. Klik ikon profil anda di sudut kanan atas
2. Pilih "Super Admin" atau navigasi terus ke halaman `/super-admin/*`

### Halaman Super Admin

| Halaman | URL | Tujuan |
|------|-----|---------|
| Organizations | `/super-admin/organizations` | Mengurus penyewa |
| Users | `/super-admin/users` | Mengurus pengguna sistem |
| Impersonate | `/super-admin/impersonate` | Sokongan penyamaran pengguna |
| Invites | `/super-admin/invites` | Kod jemputan pendaftaran |
| Logs | `/super-admin/logs` | Log ralat sistem |
| Settings | `/super-admin/settings` | Konfigurasi global |

## Mengurus Organisasi

### Melihat Organisasi

Navigasi ke **Super Admin > Organizations** untuk melihat semua penyewa:
- Nama Organisasi
- Nombor Akaun (pengenal unik)
- Maklumat perhubungan
- Status (Aktif/Tidak Aktif)
- Tarikh dicipta

### Mencipta Organisasi

1. Klik "Add Organization"
2. Isi butiran yang diperlukan:
   - **Nama Organisasi** - Nama perniagaan
   - **Nombor Akaun** - Pengenal unik (dijana secara automatik atau tersuai)
   - **E-mel Perhubungan** - Hubungan utama
   - **Telefon Perhubungan** - Telefon perniagaan
3. Konfigurasikan tetapan awal
4. Klik "Create"

### Menyunting Organisasi

1. Cari organisasi dalam senarai
2. Klik "Edit" atau nama organisasi
3. Kemas kini butiran mengikut keperluan
4. Simpan perubahan

### Status Organisasi

- **Aktif** - Operasi normal
- **Tidak Aktif** - Digantung (pengguna tidak boleh log masuk)

## Mengurus Pengguna

### Melihat Pengguna

Navigasi ke **Super Admin > Users** untuk melihat semua pengguna sistem:
- Nama pengguna dan e-mel
- Penyedia pengesahan (Google, Microsoft)
- Organisasi yang berkaitan
- Peranan dalam setiap organisasi
- Tarikh log masuk terakhir

### Akaun Pengguna

Setiap pengguna boleh menjadi ahli kepada pelbagai organisasi dengan peranan yang berbeza:

| Peranan | Penerangan |
|------|-------------|
| OrgAdmin | Akses penuh kepada organisasi |
| ShopManager | Mengurus operasi cawangan |
| Staff | Mengendalikan sewaan harian |
| Mechanic | Penyelenggaraan fleet |

### Menambah Pengguna ke Organisasi

1. Cari pengguna
2. Klik "Manage Access"
3. Pilih organisasi
4. Tetapkan peranan
5. Simpan perubahan

## Menyamar Sebagai Pengguna (Impersonation)

Ciri penyamaran membolehkan anda log masuk sebagai pengguna lain untuk tujuan sokongan.

### Cara Menyamar

1. Navigasi ke **Super Admin > Impersonate**
2. Cari pengguna mengikut:
   - Nama pengguna
   - E-mel
   - Nama
3. Klik lencana organisasi untuk mula menyamar
4. Anda kini log masuk sebagai pengguna tersebut

### Semasa Penyamaran

- Anda melihat sistem tepat seperti pengguna tersebut
- Tindakan anda direkodkan sebagai pengguna yang disamar
- Sepanduk menunjukkan anda sedang menyamar
- Klik "Stop Impersonating" untuk kembali ke akaun anda

### Bila Perlu Menggunakan Penyamaran

- **Penyelesaian Masalah** - Lihat apa yang pengguna lihat
- **Sokongan** - Bantu pengguna dengan isu tertentu
- **Ujian** - Sahkan ciri berfungsi dengan betul
- **Latihan** - Menunjukkan ciri-ciri sistem

### Amalan Terbaik

1. Hanya menyamar apabila perlu
2. Jangan buat perubahan melainkan diminta
3. Tamatkan penyamaran setelah selesai
4. Dokumentasikan interaksi sokongan

## Jemputan Pendaftaran

Kod jemputan membolehkan organisasi baru mendaftar di platform.

### Mencipta Jemputan

1. Navigasi ke **Super Admin > Invites**
2. Klik "Create Invite"
3. Konfigurasikan:
   - **Kod** - Tersuai atau dijana secara automatik
   - **Kegunaan** - Bilangan maksimum kegunaan
   - **Tamat Tempoh** - Tarikh tamat tempoh
   - **Templat Organisasi** - Tetapan yang telah dikonfigurasikan
4. Klik "Create"

### Mengurus Jemputan

- Lihat jemputan aktif dan tamat tempoh
- Lihat statistik penggunaan
- Batalkan jemputan yang tidak digunakan
- Lanjutkan tarikh tamat tempoh

### Aliran Kerja Jemputan

1. Cipta kod jemputan
2. Kongsi dengan pelanggan baru
3. Pelanggan mendaftar menggunakan kod
4. Organisasi dicipta
5. Pelanggan menyediakan akaun mereka

## Log Sistem

Lihat dan analisis peristiwa serta ralat sistem.

### Melihat Log

Navigasi ke **Super Admin > Logs** untuk melihat:
- Mesej ralat
- 'Stack traces'
- Pengguna/organisasi yang terjejas
- Cap masa (Timestamps)

### Menapis Log

Tapis mengikut:
- **Tahap** - Ralat (Error), Amaran (Warning), Info
- **Julat Tarikh** - Tempoh masa tersuai
- **Organisasi** - Penyewa tertentu
- **Pengguna** - Pengguna tertentu

### Jenis Log Biasa

| Jenis | Penerangan |
|------|-------------|
| Error | Ralat sistem yang memerlukan perhatian |
| Warning | Isu-isu berpotensi |
| Info | Peristiwa sistem umum |
| Audit | Tindakan pengguna (log masuk, perubahan) |

## Tetapan Sistem

Konfigurasikan tetapan platform global.

### Tetapan Umum

- Nama platform dan penjenamaan
- Bahasa lalai
- Format tarikh/masa
- Tetapan mata wang

### Tetapan Pengesahan

- Penyedia OAuth (Google, Microsoft)
- Masa tamat sesi
- Polisi kata laluan (jika berkenaan)

### Tetapan E-mel

- Konfigurasi SMTP
- Templat e-mel
- Keutamaan notifikasi

### Feature Flags

Dayakan/matikan ciri platform:
- Portal pelancong
- Sokongan berbilang lokasi
- Pelaporan lanjutan
- Akses API

## Pentadbiran Harian

### Senarai Semak Pagi

- [ ] Semak log sistem untuk ralat
- [ ] Semak jemputan pendaftaran yang tertunggak
- [ ] Sahkan semua organisasi aktif
- [ ] Semak sebarang tiket sokongan

### Tugasan Mingguan

- [ ] Audit akses pengguna
- [ ] Semak penggunaan organisasi
- [ ] Semak prestasi sistem
- [ ] Kemas kini dokumentasi

### Tugasan Bulanan

- [ ] Jana laporan platform
- [ ] Semak tetapan keselamatan
- [ ] Rancang kemas kini sistem
- [ ] Sandarkan (backup) konfigurasi

## Pertimbangan Keselamatan

### Kawalan Akses

1. Hadkan akses Super Admin kepada kakitangan penting sahaja
2. Gunakan pengesahan yang kuat (OAuth)
3. Semak log akses secara berkala
4. Alih keluar akaun yang tidak digunakan

### Perlindungan Data

1. Jangan berkongsi kelayakan pengguna
2. Gunakan penyamaran untuk sokongan (bukan log masuk terus)
3. Rekodkan semua tindakan pentadbiran
4. Ikuti polisi pengekalan data

### Tindak Balas Insiden

Jika anda mengesan aktiviti mencurigakan:
1. Dokumentasikan isu tersebut
2. Matikan akaun yang terjejas buat sementara waktu
3. Semak log untuk skop isu
4. Maklumkan pihak yang terjejas
5. Laksanakan langkah pembetulan

## Penyelesaian Masalah

### Isu Biasa

**Pengguna tidak boleh log masuk:**
1. Semak status pengguna (aktif)
2. Sahkan status organisasi
3. Semak penyedia pengesahan
4. Semak entri log terkini

**Organisasi tidak kelihatan:**
1. Sahkan organisasi wujud
2. Semak status (aktif/tidak aktif)
3. Sahkan pengguna mempunyai akses

**Penyamaran tidak berfungsi:**
1. Sahkan peranan Super Admin
2. Semak pengguna sasaran wujud
3. Pastikan organisasi aktif

## Mendapatkan Bantuan

- **Dokumentasi Teknikal** - Semak dokumentasi dalaman
- **Pasukan Pembangunan** - Hubungi untuk isu sistem
- **Pasukan Keselamatan** - Laporkan kebimbangan keselamatan

---

*JaleOS - Sistem Pengurusan Sewaan Kenderaan*
*Panduan Pentadbiran Platform*
