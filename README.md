<a href="https://buymeacoffee.com/abdullaherturk" target="_blank"><img src="https://cdn.buymeacoffee.com/buttons/v2/default-yellow.png" alt="Buy Me A Coffee" style="height: 60px !important;width: 217px !important;" ></a>

[![made-for-windows](https://img.shields.io/badge/Made%20for-Windows-00A4E3.svg?style=flat&logo=microsoft)](https://www.microsoft.com/)
[![Open Source?](https://img.shields.io/badge/Open%20source%3F-Of%20course%21%20%E2%9D%A4-009e0a.svg?style=flat)](https://github.com/abdullah-erturk/Windows-Backup-Restore-Tool)
[![Stable?](https://img.shields.io/badge/Release_Download_Link-v3%20%7C%20Stable-009e0a.svg?style=flat)](https://github.com/abdullah-erturk/Windows-Backup-Restore-Tool/releases)

# Windows Backup / Restore Tool v3 🛡️

### Profesyonel Sistem Yedekleme, Geri Yükleme ve Önyükleme Onarım Altyapısı
> **Windows Backup Restore Tool**, .NET 8 tabanlı, yüksek performanslı ve teknisyen odaklı bir sistem yönetim aracıdır. Hem WinPE (Windows Preinstallation Environment) hem de canlı sistemlerde çalışarak; yedekleme, geri yükleme, boot onarımı ve sürücü yönetimini tek merkezden yönetmenizi sağlar.

### Professional System Backup, Restore and Boot Repair Infrastructure
> **Windows Backup Restore Tool** is a high-performance, technician-focused system management tool based on .NET 8. It works on both WinPE (Windows Preinstallation Environment) and live systems, allowing you to manage backup, restore, boot repair, and driver management from a single location.
---

<details>
<summary><b>📸 Önizleme/Preview</b></summary>
  
![sample](https://github.com/abdullah-erturk/Windows-Backup-Restore-Tool/blob/main/1.jpeg)
![sample](https://github.com/abdullah-erturk/Windows-Backup-Restore-Tool/blob/main/2.jpeg)
![sample](https://github.com/abdullah-erturk/Windows-Backup-Restore-Tool/blob/main/3.jpeg)

</details>

<details>
<summary><b>📝 Türkçe Açıklama</b></summary>

### 🚀 Bu Projeye Özel Yetenekler

#### 🔄 Gelişmiş İmaj Yönetimi
- **Ağ (Network) Desteği**: Yerel ağdaki paylaşımlı klasörlere (UNC yolları) doğrudan yedekleme ve ağ üzerindeki imajları kısa süre içerisinde geri yükleme.
- **Merkezi Ağ Dağıtımı (PXE)**: WinPE üzerinden ağ bağlantısı kullanarak imajları ağdaki tüm bilgisayarlara hızlıca dağıtabilme (Mass Deployment).
- **WIMLib Katmanı**: Geleneksel DISM yerine yüksek performanslı `wimlib` entegrasyonu ile ultra hızlı capture/apply işlemleri.
- **VHD/VHDX Desteği**: İmajları doğrudan sanal disklere yükleyerek Native Boot kurulumları yapabilme.

#### 🔧 Önyükleme (Boot) ve Sistem Onarımı
- **UEFI Boot Reconstructor**: EFI bölümü olmayan disklere boot yapılarını sıfırdan inşa etme.
- **Otomatik Boot Sırası**: WinPE'de `bcdedit` ile UEFI önyükleme sırasını (DisplayOrder) otomatik düzenleme.
- **DISM Sağlık Motoru**: Çevrimdışı sistemlerde DISM ve SFC ile derinlemesine dosya onarımı.

#### 📦 Sürücü ve Yerelleştirme
- **Sürücü (Driver) Yönetimi**: Çalışan veya offline sistemlerden sürücü çekme (Export) ve diğer sistemlere tek tıkla enjekte etme (Import).
- **Esnek Dil Sistemi**: Harici `.ini` dosyaları üzerinden kolayca yeni dillere çevrilebilir ve özelleştirilebilir yerelleştirme yapısı (TR/EN yerleşik).

---

### 📚 Kullanım Senaryoları

1. **Sistem Göçü**: Mevcut Windows'u yedekleyip yeni bir NVMe diske dakikalar içinde taşıyın.
2. **Toplu Dağıtım (Deployment)**: WinPE üzerinden ağdaki tüm PC'lere merkezi imajı hızla kurun.
3. **Temiz Kurulum**: GPT/MBR disk yapılarını profesyonel standartlarda (Recovery bölümü sonda olacak şekilde) saniyeler içinde oluşturun.
4. **Erişilemeyen Sistem Onarımı**: Açılmayan sistemleri HealthCheck ile tarayın ve sistem dosyalarını onarın.
5. **VHD/VHDX Native Boot**: Fiziksel diski bölümlendirmeden sanal diskler üzerine kurulan sistemleri test edin.

---

### 📋 Teknik Özellikler
- **Framework**: .NET 8
- **İşletim Sistemi**: Windows 10, 11, Server 2016/2019/2022 (x64)
- **Altyapı**: WIMLib Core, DiskPart API, BCD Engineering
- **Platform**: x64 / WinPE Optimize
- **Lisans**: Açık Kaynak Geliştirme Projesi

---

### 🤝 Katkıda Bulunma
Bu proje bir **Açık Kaynak (Open Source)** geliştirme projesidir. Katkılar, sorun bildirimleri ve özellik istekleri memnuniyetle karşılanır!

### ⭐ Destek
Bu aracı faydalı bulduysanız, lütfen bir yıldız vererek değerlendirin!

</details>

<details>
<summary><b>📝 English Description</b></summary>

### 🚀 Project-Specific Capabilities

#### 🔄 Advanced Image Management
- **Network Support**: Direct backup to UNC shares and rapid image restoration from network shared folders.
- **Centralized Network Deployment (PXE)**: Use WinPE and network connectivity to rapidly deploy images to multiple PCs across the network.
- **WIMLib Layer**: Ultra-fast capture and apply operations using high-performance `wimlib` integration.
- **VHD/VHDX Support**: Deploy images directly to virtual disks for Native Boot testing.

#### 🔧 Boot & System Repair
- **UEFI Boot Reconstructor**: Build bootloader structures from scratch on disks missing EFI partitions.
- **Automatic Boot Order**: Automatically refine UEFI Boot Order (DisplayOrder) via `bcdedit` within WinPE.
- **DISM Health Engine**: Deep file system repair using DISM and SFC tools on offline installations.

#### 📦 Drivers & Localization
- **Driver Management**: Extract drivers from live or offline systems and inject them into other installations with a single click.
- **Flexible Language System**: Easily extendable localization framework via external `.ini` files (TR/EN built-in).

---

### 📚 Usage Scenarios
1. **System Migration**: Backup your current Windows and migrate it to a new NVMe disk in minutes.
2. **Mass Deployment**: Rapidly deploy a master image to multiple network-connected PCs via WinPE.
3. **Professional Partitioning**: Create GPT/MBR structures (with Recovery at the end) in seconds.
4. **Disaster Recovery**: Scan non-booting systems using HealthCheck and repair system files using original sources.
5. **VHD/VHDX Deployment**: Test system installations on virtual drives without altering your physical partition.

---

### 📋 Technical Specifications
- **Framework**: .NET 8
- **OS Support**: Windows 10, 11, Server 2016/2019/2022 (x64)
- **Core Technology**: WIMLib Core, DiskPart API, BCD Engineering
- **Platform**: x64 / WinPE Optimized
- **License**: Open Source Development Project

---

### 🤝 Contributing
Contributions, issues, and feature requests are welcome!

### ⭐ Support
Give a ⭐️ if this project helped you!

</details>

---

<p align="center">
  <b>Developed by Abdullah ERTÜRK</b><br>
  <a href="https://erturk.netlify.app" target="_blank">erturk.netlify.app</a> | <a href="https://github.com/abdullah-erturk" target="_blank">github.com/abdullah-erturk</a>
</p>
