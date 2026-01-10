<a href="https://buymeacoffee.com/abdullaherturk" target="_blank"><img src="https://cdn.buymeacoffee.com/buttons/v2/default-yellow.png" alt="Buy Me A Coffee" style="height: 60px !important;width: 217px !important;" ></a>

[![made-for-windows](https://img.shields.io/badge/Made%20for-Windows-00A4E3.svg?style=flat&logo=microsoft)](https://www.microsoft.com/)
[![Open Source?](https://img.shields.io/badge/Open%20source%3F-Of%20course%21%20%E2%9D%A4-009e0a.svg?style=flat)](https://github.com/abdullah-erturk/Windows-Backup-Restore-Tool)
[![Stable?](https://img.shields.io/badge/Release_Download_Link-v1%2E0%2E0%20%7C%20Stable-009e0a.svg?style=flat)](https://github.com/abdullah-erturk/Windows-Backup-Restore-Tool/releases)

# Windows Backup & Restore Tool 🛡️

## 📸 Önizleme / Screenshot
![sample](https://github.com/abdullah-erturk/Windows-Backup-Restore-Tool/blob/main/preview.jpg)


### Genel Bakış
**Windows Backup & Restore Tool**, Windows işletim sistemleri için geliştirilmiş profesyonel bir yedekleme ve geri yükleme aracıdır. Hem normal Windows ortamında hem de WinPE (Windows Preinstallation Environment) ortamında sorunsuz çalışabilir. Öncelikli olarak yedekleme ve kurtarma işlemleri için tasarlanmış olsa da, gerektiğinde sıfırdan Windows kurulumunda da kullanılabilir.

---

### Overview
**Windows Backup & Restore Tool** is a professional backup and restoration utility designed for Windows operating systems. It works seamlessly in both normal Windows environments and WinPE (Windows Preinstallation Environment). While primarily designed for backup and recovery operations, it can also be used for fresh Windows installations when needed.

---

<details>
<summary><b>📝Türkçe Açıklama</b></summary>

### ✨ Temel Özellikler

#### 🔄 Yedekleme (Backup)
- **Tam Sistem Yedekleme**: Seçili bölümün veya sürücünün tam yedeğini alır
- **VSS Desteği**: Windows ortamında Volume Shadow Copy Service ile çalışan sistemin yedeğini alabilir
- **Akıllı Sıkıştırma**: 
  - Sıkıştırmasız (None)
  - Hızlı sıkıştırma (Fast)
  - Maksimum sıkıştırma (Max)
- **Otomatik Hariç Tutma**: Cloud klasörleri (OneDrive, Google Drive, Dropbox) ve gereksiz dosyalar otomatik olarak yedekleme dışı bırakılır
- **WIM/ESD Format Desteği**: Standart Windows görüntü formatlarında yedekleme

#### 🔧 Geri Yükleme (Restore)
- **İki Mod**:
  - **Tam Disk Geri Yükleme**: Tüm diski biçimlendirir ve sistemi kurar
  - **Sadece Bölüm Geri Yükleme**: Seçili bölüme geri yükleme yapar
- **Otomatik Boot Yapılandırması**: 
  - GPT/UEFI desteği
  - MBR/BIOS desteği
  - Otomatik boot kaydı oluşturma
- **Çoklu WIM İndeks Desteği**: WIM dosyasındaki farklı Windows sürümlerini seçebilme
- **Akıllı Disk Yönetimi**: Sürücü harfi çakışmalarını otomatik çözer

#### 🌍 Çok Dilli Destek
- Türkçe
- İngilizce
- Genişletilebilir dil sistemi (INI dosyaları ile)

#### 🎯 Ek Özellikler
- **İşlem Sonrası Eylemler**: Yedekleme/geri yükleme sonrası otomatik kapatma veya yeniden başlatma
- **Gerçek Zamanlı İlerleme**: Detaylı log kaydı ve ilerleme göstergesi
- **Firmware Algılama**: UEFI/BIOS otomatik algılama
- **Güvenlik Kontrolleri**: Sistem diskine kazara yazma önleme
- **Gizli Bölüm Desteği**: GUID yollu bölümlere geçici sürücü harfi atama

### 💻 Sistem Gereksinimleri
- **İşletim Sistemi**: Windows 7 ve üzeri (Windows 10/11 önerilir)
- **.NET Framework**: 4.5 veya üzeri (4.6+ önerilir, WinPE sistemlerde yüklü olması gerekir)
- **Yönetici Hakları**: Gerekli
- **DISM Desteği**: Windows yerleşik DISM aracı (tüm modern Windows'larda mevcut)

### 🚀 Kullanım Senaryoları

#### 1️⃣ Sistem Yedeği Alma
```
1. Kaynak bölümü seçin (ör. C:\)
2. Yedek dosyasının kaydedileceği konumu seçin
3. Sıkıştırma seviyesini belirleyin
4. "Yedekle" butonuna tıklayın
```

#### 2️⃣ Sistem Geri Yükleme
```
1. WIM/ESD dosyasını seçin
2. Hedef diski veya bölümü seçin
3. GPT veya MBR boot modunu seçin
4. "Boot Kaydı Oluştur" seçeneğini işaretleyin
5. "Geri Yükle" butonuna tıklayın
```

#### 3️⃣ Sıfırdan Windows Kurulumu
```
1. Resmi Windows ISO'dan WIM dosyasını çıkarın
2. Geri Yükleme modunda "Tüm Disk" seçeneğini işaretleyin
3. Hedef diski seçin
4. Boot modunu (GPT/MBR) yapılandırın
5. Geri yükleme işlemini başlatın
```

### ⚙️ Teknik Detaylar
- **VSS Entegrasyonu**: PowerShell üzerinden Shadow Copy oluşturma
- **Robocopy Fallback**: VSS başarısız olursa dosya bazlı kopyalama
- **DiskPart Otomasyonu**: Disk yapılandırması için template sistemli yaklaşım
- **WMI Sorguları**: Disk ve bölüm bilgilerini almak için
- **IOCTL Çağrıları**: Düşük seviye disk bilgilerine erişim

### 📋 Yedekleme Hariç Tutma Sistemi
Araç, `bin\wim_exclusions.ini` dosyasında tanımlanan kuralları kullanır:
- Sistem dosyaları (hiberfil.sys, pagefile.sys)
- Geçici dosyalar
- Geri dönüşüm kutusu
- Cloud senkronizasyon klasörleri (otomatik algılama)
- Zaten sıkıştırılmış dosyalar (zip, jpg, mp4, vb.)

### 🔒 Güvenlik ve İstikrar
- Sistem diskine yazma koruması
- İşlem öncesi onay diyalogları
- Otomatik backup dosyası temizleme (iptal durumunda)
- Sürücü harfi çakışma yönetimi
- Detaylı hata raporlama

### 🛠️ Geliştirici Notları
- **Mimari**: Windows Forms (C#)
- **Temel Araçlar**: DISM, DiskPart, BCDBoot, Robocopy
- **Platform**: x86/x64 uyumlu

🎯 Kimler için?
- ✅ Sistem yöneticileri
- ✅ BT uzmanları
- ✅ İleri düzey kullanıcılar
- ✅ Bilgisayar tamir teknisyenleri
- ✅ Güvenilir bir Windows yedekleme/geri yükleme çözümüne ihtiyaç duyan herkes

## 🤝 Katkıda Bulunma

Katkılar, sorunlar ve özellik istekleri memnuniyetle karşılanır! Issues sayfasını kontrol etmekten çekinmeyin.

## ⭐ Yıldız

Bu aracı faydalı bulduysanız, lütfen bir yıldız vererek değerlendirin!

---

**Not**: Yedeklerinizi her zaman test edin! Bir yedek, ancak ondan geri yükleme yapabildiğiniz kadar iyidir. Yedekleme stratejinizin etkili olduğundan emin olmak için periyodik olarak test geri yüklemeleri gerçekleştirin.
</details>

<details>
<summary><b>📝 English Description</b></summary>

### ✨ Key Features

#### 🔄 Backup Capabilities
- **Full System Backup**: Complete backup of selected partitions or drives
- **VSS Support**: Volume Shadow Copy Service integration for live system backup
- **Intelligent Compression**: 
  - None (fastest)
  - Fast (balanced)
  - Maximum (smallest size)
- **Smart Exclusions**: Automatically excludes cloud folders (OneDrive, Google Drive, Dropbox) and unnecessary files
- **WIM/ESD Format Support**: Standard Windows imaging formats

#### 🔧 Restoration Features
- **Dual Mode Operation**:
  - **Whole Disk Restore**: Formats entire disk and deploys system
  - **Partition Only Restore**: Restores to selected partition
- **Automatic Boot Configuration**: 
  - GPT/UEFI support
  - MBR/BIOS support
  - Automated boot record creation
- **Multi-Index WIM Support**: Select different Windows editions from WIM file
- **Intelligent Disk Management**: Automatic resolution of drive letter conflicts

#### 🌍 Multi-Language Support
- Turkish
- English
- Extensible language system (via INI files)

#### 🎯 Additional Features
- **Post-Operation Actions**: Automatic shutdown or restart after backup/restore
- **Real-Time Progress**: Detailed logging and progress indicators
- **Firmware Detection**: Automatic UEFI/BIOS detection
- **Safety Checks**: Prevents accidental system disk overwrites
- **Hidden Partition Support**: Temporary drive letter assignment for GUID paths

### 💻 System Requirements
- **Operating System**: Windows 7 and above (Windows 10/11 recommended)
- **.NET Framework**: 4.5 or higher (4.6+ recommended. WinPE systems also require the .NET Framework to be installed.)
- **Administrator Rights**: Required
- **DISM Support**: Windows built-in DISM tool (available in all modern Windows)

### 🚀 Usage Scenarios

#### 1️⃣ Creating System Backup
```
1. Select source partition (e.g., C:\)
2. Choose backup file destination
3. Set compression level
4. Click "Backup" button
```

#### 2️⃣ Restoring System
```
1. Browse and select WIM/ESD file
2. Choose target disk or partition
3. Select GPT or MBR boot mode
4. Enable "Create Boot Record" option
5. Click "Restore" button
```

#### 3️⃣ Fresh Windows Installation
```
1. Extract WIM file from official Windows ISO
2. Select "Whole Disk" in restore mode
3. Choose target disk
4. Configure boot mode (GPT/MBR)
5. Start restoration process
```

### ⚙️ Technical Details
- **VSS Integration**: Shadow Copy creation via PowerShell
- **Robocopy Fallback**: File-level copying when VSS fails
- **DiskPart Automation**: Template-based disk configuration approach
- **WMI Queries**: Disk and partition information retrieval
- **IOCTL Calls**: Low-level disk information access

### 📋 Backup Exclusion System
The tool uses rules defined in `bin\wim_exclusions.ini`:
- System files (hiberfil.sys, pagefile.sys)
- Temporary files
- Recycle bin
- Cloud sync folders (automatic detection)
- Already compressed files (zip, jpg, mp4, etc.)

### 🔒 Security and Stability
- System disk write protection
- Pre-operation confirmation dialogs
- Automatic backup file cleanup (on abort)
- Drive letter conflict management
- Detailed error reporting

### 🛠️ Developer Notes
- **Architecture**: Windows Forms (C#)
- **Core Tools**: DISM, DiskPart, BCDBoot, Robocopy
- **Platform**: x86/x64 compatible

## 🎯 For whom?
- ✅ System administrators
- ✅ IT professionals
- ✅ Power users
- ✅ PC repair technicians
- ✅ Anyone needing reliable Windows backup/restore solution

## 🤝 Contributing
Contributions, issues, and feature requests are welcome!

## ⭐ Show Your Support
Give a ⭐️ if this project helped you!

---

**Note**: This tool requires administrative privileges and uses Windows native tools (DISM, DiskPart, BCDBoot). Always test in a safe environment before using on production systems.
</details>
