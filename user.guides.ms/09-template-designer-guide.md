# Panduan Pereka Templat Dokumen

Pereka Templat Dokumen (Document Template Designer) adalah alat visual seret-dan-lepas (drag-and-drop) yang membolehkan Pentadbir Organisasi menyesuaikan dokumen yang dijana oleh JaleOS, seperti Perjanjian Sewaan, Pengesahan Tempahan, dan Resit.

## Gambaran Keseluruhan

JaleOS menggunakan sistem susun atur berasaskan bahagian (section-based layout). Setiap templat terdiri daripada beberapa bahagian (Pengepala, Badan, Kaki) yang mengandungi "blok" boleh seret seperti teks, jadual, dan imej. Blok-blok ini boleh dipautkan kepada data dinamik daripada sewaan dan tempahan anda.

## Bermula

1. Navigasi ke **Settings > Document Templates**.
2. Klik **Create New Template** atau klik pada templat sedia ada untuk menyuntingnya.
3. Pilih **Jenis Templat (Template Type)**:
   - **Booking Confirmation**: Dihantar kepada pelanggan apabila mereka menempah kenderaan.
   - **Rental Agreement**: Kontrak undang-undang yang ditandatangani semasa 'check-in'.
   - **Receipt**: Bukti kewangan bagi pembayaran.

## Menggunakan Pereka (Designer)

Antara muka pereka dibahagikan kepada tiga kawasan utama:
1. **Kotak Alat (Kiri)**: Mengandungi blok-blok yang boleh diseret.
2. **Kanvas (Tengah)**: Tempat anda menyusun blok anda.
3. **Editor Sifat (Kanan)**: Konfigurasikan tetapan untuk blok yang dipilih.

### Blok Boleh Seret

| Jenis Blok | Penerangan |
|------------|-------------|
| **Text** | Teks statik atau token data dinamik (cth., `{{CustomerName}}`). |
| **Image** | Logo anda atau imej penjenamaan lain. |
| **Table** | Digunakan untuk senarai terperinci seperti butiran kenderaan atau pecahan pembayaran. |
| **Separator** | Garisan mendatar untuk membahagikan bahagian. |
| **Header/Footer** | Blok yang telah dikonfigurasikan untuk pengepala dan kaki dokumen standard. |

### Pautan Data (Token)

Anda boleh memasukkan data dinamik ke dalam blok Teks menggunakan kurungan kerinting berganda.

**Token Biasa:**
- `{{OrganizationName}}` - Nama perniagaan anda.
- `{{ShopName}}` - Cawangan khusus.
- `{{CustomerName}}` - Nama penuh penyewa.
- `{{VehicleName}}` - Jenama dan model motosikal.
- `{{StartDate}}` / `{{EndDate}}` - Tempoh sewaan.
- `{{TotalAmount}}` - Jumlah kos sewaan.

## Amalan Terbaik

1. **Uji dengan Pratonton**: Sentiasa gunakan butang "Pratonton" (Preview) untuk melihat rupa templat anda dengan data contoh sebelum menggunakannya secara rasmi.
2. **Tetapkan sebagai Lalai (Default)**: Setelah anda berpuas hati dengan templat tersebut, klik **Set as Default** supaya ia digunakan secara automatik untuk semua sewaan/tempahan baru.
3. **Penjenamaan**: Muat naik logo beresolusi tinggi (PNG atau JPG) untuk kualiti cetakan terbaik.
4. **Pematuhan Undang-undang**: Pastikan Perjanjian Sewaan anda merangkumi semua terma dan syarat yang diperlukan oleh undang-undang tempatan.

## Status Templat

- **Draf (Draft)**: Kerja sedang dijalankan, tidak kelihatan kepada kakitangan.
- **Diluluskan (Approved)**: Sedia untuk digunakan, boleh dipilih secara manual semasa mencetak.
- **Lalai (Default)**: Templat utama yang digunakan secara automatik oleh sistem.

## Penyelesaian Masalah

### Token tidak menunjukkan data
Pastikan anda menggunakan nama token yang betul (sensitif huruf besar/kecil) dan token tersebut sah untuk jenis templat (cth., `{{BookingID}}` hanya berfungsi pada templat Tempahan).

### Susun atur kelihatan rosak pada peranti mudah alih
Pereka ini dioptimumkan untuk penggunaan desktop. Untuk pengalaman terbaik, reka templat anda pada skrin besar.

---

*JaleOS - Pengurusan Sewaan Profesional*
