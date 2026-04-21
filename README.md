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

<details>
<summary><b>📝 한국어 설명</b></summary>

### 🚀 프로젝트별 기능

#### 🔄 고급 이미지 관리
- **네트워크 지원**: UNC 공유로 직접 백업하고 네트워크 공유 폴더에서 이미지를 신속하게 복원합니다.
- **중앙 집중식 네트워크 배포 (PXE)**: WinPE와 네트워크 연결을 사용하여 네트워크 전체의 여러 PC에 이미지를 신속하게 배포합니다.
- **WIMLib 계층**: 고성능 'wimlib' 통합을 사용하여 작업을 초고속으로 캡처하고 적용합니다.
- **VHD/VHDX 지원**: 네이티브 부팅 테스트를 위해 이미지를 가상 디스크에 직접 배포합니다.

#### 🔧 부팅 및 시스템 복원
- **UEFI 부트 재구성기**: EFI 파티션이 없는 디스크에서 부트로더 구조를 처음부터 구축합니다.
- **자동 부팅 순서**: WinPE 내에서 `bcdedit`을 통해 UEFI 부트 순서 (표시 순서)를 자동으로 조정합니다.
- **DISM 상태 엔진**: 오프라인 설치 시 DISM 및 SFC 도구를 사용한 정밀하세 파일 시스템을 복구합니다.

#### 📦 드라이버 및 현지화
- **ㄷ라이버 관리**: 라이브 또는 오프라인 시스템에서 드라이버를 추출하여 클릭 한 번으로 다른 설치에 주입합니다.
- **유연한 언어 시스템**: 외부 `.ini` 파일 (TR/EN 내장)을 통해 쉽게 확장할 수 있는 로컬라이제이션 프레임워크입니다.

---

### 📚 사용 시나리오
1. **시스템 마이그레이션**: 현재 Windows를 백업하고 몇 분 안에 새 NVMe 디스크로 마이그레이션합니다.
2. **대량 배포**: WinPE를 통해 여러 네트워크에 연결된 PC에 마스터 이미지를 신속하게 배포합니다.
3. **전문적인 파티셔닝**: 몇 초 만에 GPT/MBR 구조 (마지막에 복구 기능 포함)를 생성합니다.
4. **재해 복구**: HealthCheck을 사용하여 부팅되지 않는 시스템을 검사하고 원본 소스를 사용하여 시스템 파일을 복구합니다.
5. **VHD/VHDX 배포**: 물리적 파티션을 변경하지 않고 가상 드라이브에서 시스템 설치를 테스트합니다.

---

### 📋 기술 사양
- **프레임워크**: .NET 8
- **지원 OS**: Windows 10, 11, Server 2016/2019/2022 (x64)
- **핵심 기술**: WIMLib Core, DiskPart API, BCD 엔지니어링
- **플랫폼**: x64 / WinPE 최적화
- **라이선스**: 오픈 소스 개발 프로젝트

---

### 🤝 기여하기
기여, 이슈 제기, 기능 요청을 환영합니다!

### ⭐ 후원
이 프로젝트가 도움이 되었다면 ⭐️를 눌러주세요!

</details>

---

<p align="center">
  <b>Developed by Abdullah ERTÜRK</b><br>
  <a href="https://erturk.netlify.app" target="_blank">erturk.netlify.app</a> | <a href="https://github.com/abdullah-erturk" target="_blank">github.com/abdullah-erturk</a>
</p>
