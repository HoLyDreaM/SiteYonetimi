# Site Yönetim Sistemi

Apartman ve site yöneticilerinin günlük işlemlerini dijital ortamda yönetmelerini sağlayan kapsamlı bir web uygulamasıdır. Site tanımlama, daire yönetimi, gider takibi, aidat tahsilatı, borç takibi, raporlama ve destek talepleri gibi tüm yönetim işlevlerini tek bir platformda sunar.

**Hedef Kullanıcılar:** Site yöneticileri, apartman yönetim şirketleri, kat malikleri dernekleri

---

## İçindekiler

1. [Genel Bakış](#genel-bakış)
2. [Özellikler](#özellikler)
3. [Gereksinimler](#gereksinimler)
4. [Proje Yapısı](#proje-yapısı)
5. [Kurulum](#kurulum)
6. [Veritabanı Kurulumu](#veritabanı-kurulumu)
7. [Uygulama Ayarları](#uygulama-ayarları)
8. [Çalıştırma](#çalıştırma)
9. [Kullanım Kılavuzu](#kullanım-kılavuzu)
10. [API Referansı](#api-referansı)
11. [Teknolojiler](#teknolojiler)
12. [Sorun Giderme](#sorun-giderme)
13. [Yapılan Güncellemeler](#yapılan-güncellemeler)

---

## Genel Bakış

Site Yönetim Sistemi, .NET 8 ve ASP.NET Core MVC ile geliştirilmiş, SQL Server veritabanı kullanan tam özellikli bir yönetim panelidir. JWT Bearer ve Cookie tabanlı kimlik doğrulama destekler. Çoklu site yapısı sayesinde tek bir hesaptan birden fazla site yönetilebilir.

---

## Özellikler

### Site ve Bina Yönetimi
- Site tanımlama (ad, adres, şehir, ilçe)
- Vergi bilgileri (vergi dairesi, vergi numarası)
- Gecikme zammı ayarları (oran, uygulama günü)
- Çok bloklu site desteği
- Varsayılan aylık aidat tutarı
- Aidat ödeme dönemi (başlangıç-bitiş günü, 1–28 arası)

### Daire Yönetimi
- Daire ekleme, düzenleme ve **silme**
- Blok/bina ve kat bilgisi
- Pay oranı (aidat dağılımı için)
- **Kim oturuyor?** seçeneği:
  - **Ev Sahibi Oturuyor:** Malik adı, telefon
  - **Kiracı Oturuyor:** Ev sahibi adı, ev sahibi telefonu, kiracı adı, kiracı telefonu (zorunlu)
- Daire bazlı aylık aidat tutarı (boş bırakılırsa Site varsayılanı × pay oranı kullanılır)
- Daire bazlı aidat ödeme dönemi (başlangıç-bitiş günü)

### Gelirler (Aidat)
- Site/daire bazlı aylık aidat tanımlama
- Her ayın 1'inde otomatik aidat oluşturma (HostedService)
- **Tahsil edilen** ve **bekleyen** gelirler ayrı kartlarda gösterilir
- **Kısmi tahsilat** desteği (bir aidatın bir kısmını tahsil edip kalanını sonraya bırakma)
- Aidat tahsilat kaydı
- **Tahsilat yapılmamış aidatları tek tek veya toplu silme**
- Ek para toplama (Özel Toplama) kaydı

### Gider Yönetimi
- Gider türleri (Elektrik, Su, Aidat vb.)
- Gider kaydı oluşturma, düzenleme, silme
- Fatura numarası, fatura tarihi ve vade tarihi takibi
- Giderleri rapordan hariç tutma seçeneği (ExcludeFromReport)
- **Fatura ek dosyası:** JPG veya PDF formatında fatura ekleme (oluşturma ve düzenleme sırasında)

### Tahsilatlar ve Ödemeler
- Ödeme kaydı (nakit, havale, kredi kartı vb.)
- Makbuz oluşturma
- Banka hesabına bağlama
- **Tahsilat İptal:** Yanlış tahsil edilen aidat iptal edilebilir; aidat tekrar tahsil edilebilir hale gelir, banka bakiyesi ve gelir durumu güncellenir

### Banka Hesapları
- Site banka hesapları (**sadece bir banka hesabı** eklenebilir)
- Güncel bakiye takibi
- Tahsilatlar bakiyeyi artırır, gider ödemeleri azaltır
- Başlangıç bakiyesi (OpeningBalance) desteği
- **Detay sayfası:** Banka, şube, hesap no, IBAN, güncel bakiye bilgilerinin altında tüm gelen ve giden ödemeler/tahsilatlar sayfalanmış listelenir

### Bildirimler (Çan İkonu)
- **Bildirim simgesi** (çan ikonu) ile site anasayfasından itibaren tüm sayfalarda yer alır
- Tıklanınca **dropdown** açılır:
  - **Ödemesi Gelen Giderler:** Son 30 gün içinde bankadan ödenen (düşülen) giderler listelenir
  - **Süresi Geçen Aidatlar:** Ödeme süresi geçmiş ve tahsil edilmemiş/kısmi tahsil edilmiş aidatlar listelenir
- Bildirim varsa simgede kırmızı badge ile toplam sayı gösterilir
- Her gider için detay linki ile Giderler sayfasına, her aidat için tahsilat linki ile Gelirler sayfasına hızlı erişim

### Raporlar

#### Hazırün Cetveli
- Toplantı katılım listesi sütunları: **Blok**, **No**, **Kat Maliki**, **İmza**, **Varsa Vekili**, **İmza**
- **Çıktı Al / Yazdır** butonu ile toplantılardan hemen önce çıktı alınıp imzalatılabilir
- Toplantı tarihi seçilebilir

#### Aylık Rapor
- **Başlık formatı:** `Yıl Ay - Site İsmi` (örn: `2025 Şubat - Site Yönetimi`)
- Tahsil edilen, bekleyen gelir, toplam gider, bakiye özet kartları
- **Gelirler kalem kalem** (Blok, Daire, Ev Sahibi, Tür, Tutar, Tahsil Edilen, Kalan, Vade)
- **Giderler kalem kalem** (Gider Türü, Açıklama, Tarih, Fatura No, Tutar)
- **Excel İndir:** Tüm detaylar ₺ simgesiyle Excel dosyasına aktarılır
- **Yazdır:** Sol menü gizlenir, rapor başlığı ve özet kartlar yan yana yazdırılır

#### Yıllık Rapor
- **Başlık formatı:** `Yıl - Site İsmi` (örn: `2025 - Site Yönetimi`)
- Yıllık özet kartları (Tahsil Edilen, Bekleyen, Toplam Gider, Bakiye)
- Aylık özet tablosu (her ay için tahsil, bekleyen, gider, bakiye)
- **Detayları göster** checkbox’ı: Ekranda her ayın gelir ve giderlerini kalem kalem açıp kapatma
- **Yazdırma:** Her zaman detaylı (checkbox’tan bağımsız)
- **Excel İndir:** Her ay için gelir ve gider detayları dahil tam rapor

### Borçlular
- **Sadece aidat ve ek para toplama borçları** gösterilir (gider dağıtımı borçları dahil değildir)
- Borçlu daire listesi, tutar, en eski vade, gecikme günü

### Site Sakinleri
- Dairelerde oturan kişilerin (ev sahibi veya kiracı) listesi
- İsim, kat, daire, telefon, tip (Ev Sahibi / Kiracı) bilgileri
- Sadece görüntüleme amaçlı

### Destek Talepleri
- Site sakinlerinden yöneticiye destek talebi
- **Giriş gerektirmeyen** form: `/Destek?siteId=xxx` veya `/DestekKaydi?siteId=xxx`
- Blok, daire, iletişim bilgileri, konu, açıklama
- Dosya eki desteği
- **Site bazlı SMTP ayarları:** Her site kendi bildirim e-postası ve SMTP ayarlarını tanımlayabilir
- Destek kaydı oluşturulunca yapılandırılmış e-posta adresine bildirim gönderilir

### Öneri / Şikayet Formu (Public)
- Giriş gerektirmeyen URL: `/Feedback/Create?siteId=xxx` veya `/OneriSikayet?siteId=xxx`
- Blok No, Daire No, İsim Soyisim, Telefon, Konu, Açıklama alanları

### Üst Bar (Giriş Sonrası)
- **Bildirim simgesi:** Ödemesi gelen giderler dropdown’u
- **Profil:** Hesap bilgileri ve ayarlar sayfasına gider (Hesap Ayarları menüden kaldırılmıştır)

### Panel (Dashboard)
- Sitelerim özeti
- **Destek URL’si:** Her site için `https://.../Destek?siteId=xxx` adresi ve **Kopyala** butonu
- Son siteler listesi, her sitenin Destek URL’si ve kopyalama butonu

### Sayaç Yönetimi
- Su, elektrik, doğalgaz sayaçları
- Sayaç okuma girişi
- Sayaç geçmişi takibi

### Teklifler
- Site teklif kayıtları (şirket adı, tarih, aylık/yıllık ücret, açıklama)
- **Otomatik klasör oluşturma:** Teklifler, Gider faturaları ve Destek kayıtları için gerekli upload klasörleri (`uploads/teklifler`, `uploads/giderler`, `uploads/destek`) uygulama başlangıcında otomatik oluşturulur

### Evrak Arşivi
- Site bazlı evrak arşivi (sözleşmeler, belgeler vb.)
- **Evrak Adı**, **Açıklama** ve **Dosya** alanları
- PDF, Word (.doc, .docx), Excel (.xls, .xlsx) dosya desteği
- **Site bazlı klasör yapısı:** `uploads/evraklar/{siteId}/{documentId}.{uzantı}` — her site kendi klasöründe, üye siteleri birbirinden ayrılır
- Ekler, düzenleme ve silme

### Önemli Telefonlar
- Site bazlı acil ve önemli telefon listesi

### Kimlik Doğrulama
- JWT Bearer token (API)
- Cookie tabanlı web oturumu (panel)
- Kayıt (Register), Giriş (Login)
- Token yenileme (Refresh Token)
- Rol tabanlı yetkilendirme (Admin, Manager, Resident)

### URL Yapısı
- **Ana panel URL’leri:** `/Dashboard`, `/Sites`, `/Apartments`, `/Incomes` vb. (`/App/` öneki yok)
- **Geriye dönük uyumluluk:** `/App/Dashboard`, `/App/Sites` vb. eski URL’ler hâlâ çalışır

### Diğer
- Anketler (tablolar hazır, geliştirme aşamasında)
- Üye yönetimi
- Çoklu site desteği (kullanıcı-site ilişkisi)

---

## Gereksinimler

| Gereksinim | Sürüm / Not |
|------------|-------------|
| .NET SDK | 8.0 — [İndir](https://dotnet.microsoft.com/download/dotnet/8.0) |
| MS-SQL Server | 2019 veya üzeri (LocalDB, Express veya tam sürüm) |
| SQL Server Management Studio | Opsiyonel (manuel veritabanı kurulumu için) |
| PowerShell | Veritabanı otomatik kurulum scripti için |

---

## Proje Yapısı

```
SiteYonetim/
├── src/
│   ├── SiteYonetim.Domain/           # Entity'ler, interface'ler, domain modelleri
│   │   ├── Entities/                 # Apartment, Site, Income, Expense, vb.
│   │   └── Interfaces/               # IReportService, IIncomeService, vb.
│   ├── SiteYonetim.Infrastructure/   # EF Core DbContext, servis implementasyonları
│   │   ├── Data/                     # SiteYonetimDbContext
│   │   ├── Services/                 # ReportService, AuthService, vb.
│   │   └── HostedServices/          # MonthlyDuesHostedService, vb.
│   ├── SiteYonetim.WebApi/           # ASP.NET Core Web API + MVC
│   │   ├── Areas/App/                # Web panel (MVC)
│   │   │   ├── Controllers/
│   │   │   └── Views/
│   │   ├── Controllers/              # REST API, Account, Destek, Feedback
│   │   └── Views/                   # Login, Register, Destek formu
│   └── SiteYonetim.Tests/            # Birim testleri
├── database/
│   ├── Scripts/
│   │   └── Full-Schema.sql          # Tek dosyada tüm veritabanı şeması
│   ├── Full-Schema.sql              # (Scripts ile aynı içerik)
│   └── Setup-Database.ps1           # Otomatik veritabanı kurulum scripti
├── SiteYonetim.sln
└── README.md
```

### Katmanlar
- **Domain:** İş kuralları, entity'ler, servis arayüzleri
- **Infrastructure:** Veritabanı erişimi, e-posta, JWT, HostedService'ler
- **WebApi:** HTTP API, MVC sayfaları, kimlik doğrulama, filtreler

---

## Kurulum

### 1. Projeyi İndirme / Klonlama

```bash
git clone <repository-url>
cd SiteYonetim
```

### 2. Bağımlılıkları Yükleme

```bash
dotnet restore
```

### 3. Uygulama Ayarlarını Oluşturma

`appsettings.json` dosyaları güvenlik nedeniyle repoda yoktur. Önce örnek dosyaları kopyalayın:

**Windows (PowerShell):**
```powershell
cd src/SiteYonetim.WebApi
Copy-Item appsettings.example.json appsettings.json
Copy-Item appsettings.Development.example.json appsettings.Development.json
```

**Linux / macOS:**
```bash
cd src/SiteYonetim.WebApi
cp appsettings.example.json appsettings.json
cp appsettings.Development.example.json appsettings.Development.json
```

Ardından [Uygulama Ayarları](#uygulama-ayarları) bölümüne göre connection string ve JWT ayarlarını düzenleyin.

### 4. Projeyi Derleme

```bash
dotnet build
```

---

## Veritabanı Kurulumu

Veritabanı tabloları oluşturulmadan uygulama çalışmaz. `Invalid object name 'UserSites'` gibi hatalar veritabanı kurulumunun yapılmadığını gösterir.

### Yöntem 1: Otomatik Kurulum (PowerShell)

```powershell
cd database
.\Setup-Database.ps1
```

Script, `appsettings.json` içindeki connection string'i otomatik okur. Farklı sunucu için:

```powershell
.\Setup-Database.ps1 -Server "BILGISAYAR_ADI" -User "sa" -Password "sifreniz"
```

Windows Authentication (Trusted_Connection) kullanıyorsanız parametre vermeniz gerekmez.

### Yöntem 2: Manuel Kurulum (SQL Server Management Studio)

1. SQL Server Management Studio'yu açın
2. SQL Server örneğinize bağlanın
3. **Dosya → Aç → Dosya** ile `database/Scripts/Full-Schema.sql` dosyasını açın
4. **F5** veya **Yürüt** ile script'i çalıştırın

### Yöntem 3: sqlcmd ile Kurulum

```bash
sqlcmd -S . -d master -i database/Scripts/Full-Schema.sql
```

`-S .` yerel sunucu için. Farklı sunucu: `-S "BILGISAYAR_ADI\SQLEXPRESS"`

### Veritabanı Detayları

| Özellik | Değer |
|---------|-------|
| Veritabanı adı | SiteYonetim |
| Collation | Turkish_CI_AS |
| Script özelliği | `IF NOT EXISTS` kullanıldığı için mevcut veritabanına tekrar çalıştırılabilir (idempotent) |

---

## Uygulama Ayarları

> **İlk kurulum:** `appsettings.json` ve `appsettings.Development.json` dosyaları güvenlik nedeniyle repoda bulunmaz. Projeyi indirdikten sonra aşağıdaki adımları uygulayın:
>
> 1. `src/SiteYonetim.WebApi/appsettings.example.json` dosyasını kopyalayıp `appsettings.json` olarak kaydedin
> 2. `src/SiteYonetim.WebApi/appsettings.Development.example.json` dosyasını kopyalayıp `appsettings.Development.json` olarak kaydedin
> 3. Aşağıdaki ayarları kendi ortamınıza göre düzenleyin

`src/SiteYonetim.WebApi/appsettings.json` dosyasını düzenleyin.

### Connection String

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=SiteYonetim;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

**Örnek bağlantı dizeleri:**

| Ortam | Connection String |
|-------|-------------------|
| Yerel (Windows Auth) | `Server=.;Database=SiteYonetim;Trusted_Connection=True;TrustServerCertificate=True;` |
| Yerel (SQL Auth) | `Server=.;Database=SiteYonetim;User Id=sa;Password=sifre;TrustServerCertificate=True;` |
| SQL Express | `Server=.\SQLEXPRESS;Database=SiteYonetim;Trusted_Connection=True;TrustServerCertificate=True;` |
| Uzak sunucu | `Server=sunucu.adresi;Database=SiteYonetim;User Id=kullanici;Password=sifre;TrustServerCertificate=True;` |

### JWT Ayarları

```json
{
  "Jwt": {
    "Secret": "EnAz32KarakterUzunlugundaGizliAnahtar!",
    "Issuer": "SiteYonetim",
    "Audience": "SiteYonetim",
    "AccessTokenMinutes": 60,
    "RefreshTokenDays": 7
  }
}
```

**Önemli:** Üretim ortamında `Secret` değerini mutlaka güçlü ve benzersiz bir anahtarla değiştirin.

### E-posta (Opsiyonel)

Genel e-posta ayarları `appsettings.json` içinde tanımlanabilir. Destek bildirimleri için **her site kendi SMTP ayarlarını** Destek Kayıt Ayarları sayfasından yapılandırır.

---

## Çalıştırma

```bash
cd SiteYonetim
dotnet run --project src/SiteYonetim.WebApi
```

Veya Visual Studio / Rider ile **F5** ile çalıştırın.

### Erişim Adresleri

| Adres | Açıklama |
|-------|----------|
| `https://localhost:7xxx` | Web paneli (giriş sayfası, ana sayfa) |
| `https://localhost:7xxx/Dashboard` | Panel ana sayfa (giriş sonrası) |
| `https://localhost:7xxx/Sites` | Siteler |
| `https://localhost:7xxx/Destek?siteId=xxx` | Destek kaydı formu (giriş gerekmez) |
| `https://localhost:7xxx/OneriSikayet?siteId=xxx` | Öneri/Şikayet formu (giriş gerekmez) |
| `https://localhost:7xxx/swagger` | Swagger UI (API dokümantasyonu) |

Port numarası (7xxx) `launchSettings.json` veya çalışma ortamına göre değişir.

---

## Kullanım Kılavuzu

### İlk Kurulum Adımları

1. **Kayıt:** Web arayüzünden veya Swagger `POST /api/auth/register` ile kayıt olun
2. **Giriş:** E-posta ve şifre ile giriş yapın
3. **Site Ekle:** Sol menüden **Siteler** → **Yeni Site** ile ilk sitenizi ekleyin
4. **Daire Ekle:** **Daireler** menüsünden daireleri tanımlayın (Ev Sahibi/Kiracı seçeneğini belirleyin)
5. **Gider Türü Ekle:** **Gider Türleri** menüsünden (Aidat, Elektrik, Su vb.) ekleyin
6. **Gider Ekle:** **Giderler** menüsünden gider kaydı oluşturun
7. **Destek Ayarları:** **Destek Kayıt Ayarları** ile site bazlı SMTP ve bildirim e-postası tanımlayın
8. **Tahsilat:** **Gelirler** sayfasından aidat oluşturup **Tahsilatlar** menüsünden ödeme alın

### Menü Yapısı

| Menü | Açıklama |
|------|----------|
| Panel | Dashboard, siteler özeti, Destek URL’leri |
| Siteler | Site listesi, yeni site ekleme, düzenleme |
| Daireler | Daire listesi, ekleme, düzenleme, silme |
| Gelirler | Aylık aidat listesi, tahsilat, kısmi tahsilat, aidat silme |
| Gider Türleri | Gider türü tanımları |
| Giderler | Gider kayıtları, düzenleme, silme |
| Tahsilatlar | Ödeme kayıtları |
| Banka Hesapları | Banka hesapları, bakiye |
| Borçlular | Aidat/ek toplama borçlu daire listesi |
| Teklifler | Site teklif kayıtları |
| Evrak Arşivi | Sözleşme, belge arşivi (PDF, Word, Excel) |
| Site Sakinleri | Dairelerde oturan kişiler (ev sahibi/kiracı) listesi |
| Önemli Telefonlar | Acil ve önemli telefon listesi |
| Destek Kayıtları | Destek talepleri listesi |
| Destek Kayıt Ayarları | Site bazlı SMTP ve bildirim e-postası |
| Sayaçlar | Sayaç tanımları, okuma girişi |
| Raporlar | Aylık rapor, Yıllık rapor, Hazırün Cetveli (Blok, No, Kat Maliki, İmza, Varsa Vekili, İmza - çıktı/yazdırma) |

*Not: Hesap Ayarları sol menüden kaldırılmıştır. Profil ve hesap ayarlarına üst bardaki **Profil** linkinden erişilir.*

### Destek URL’si Kullanımı

1. **Panel** sayfasında **Sitelerim** veya **Son Siteler** bölümünde her site için Destek URL’si görünür
2. `https://siteniz.com/Destek?siteId=xxx` formatındaki linki site sakinlerine paylaşın
3. **Kopyala** butonu ile URL’yi panoya kopyalayın
4. Site sakinleri bu linke tıklayarak giriş yapmadan destek talebi oluşturabilir

### Raporlar Kullanımı

**Aylık Rapor:**
- Yıl ve ay seçip **Görüntüle** ile raporu açın
- **Excel İndir** ile `.xlsx` dosyası indirin
- **Yazdır** ile tarayıcı yazdırma penceresini açın (sol menü gizlenir)

**Hazırün Cetveli:**
- Raporlar sayfasından **Hazırün Cetveli** kartına tıklayın
- Sütunlar: Blok, No, Kat Maliki, İmza, Varsa Vekili, İmza
- Toplantı tarihini seçip **Güncelle** ile listeyi güncelleyin
- **Çıktı Al / Yazdır** ile yeni pencerede yazdırma uyumlu sayfa açılır; toplantı öncesi çıktı alıp imzalatabilirsiniz

**Yıllık Rapor:**
- Yıl seçip **Görüntüle** ile raporu açın
- **Detayları göster** checkbox’ı ile ekranda aylık detayları açıp kapatın
- **Excel İndir** her zaman detaylı çıktı verir
- **Yazdır** her zaman detaylı çıktı verir

### Çoklu Site

Kullanıcılar birden fazla siteye atanabilir. Menüden site seçimi yapılarak ilgili siteye geçilir. Tek site varsa otomatik olarak o site seçilir. URL’lerde `?siteId=xxx` parametresi kullanılır.

---

## API Referansı

Tüm API’ler (auth hariç) `Authorization: Bearer {token}` header’ı gerektirir.

### Kimlik Doğrulama

| Metod | Endpoint | Açıklama |
|-------|----------|----------|
| POST | /api/auth/register | Yeni kullanıcı kaydı |
| POST | /api/auth/login | Giriş (JWT token döner) |
| POST | /api/auth/refresh | Token yenileme |

### Siteler

| Metod | Endpoint | Açıklama |
|-------|----------|----------|
| GET | /api/sites | Kullanıcının siteleri |
| POST | /api/sites | Yeni site oluştur |

### Daireler

| Metod | Endpoint | Açıklama |
|-------|----------|----------|
| GET | /api/apartments/site/{siteId} | Site daireleri |

### Gider Türleri

| Metod | Endpoint | Açıklama |
|-------|----------|----------|
| GET | /api/expensetypes/site/{siteId} | Site gider türleri |
| POST | /api/expensetypes | Yeni gider türü |

### Giderler

| Metod | Endpoint | Açıklama |
|-------|----------|----------|
| GET | /api/expenses/site/{siteId} | Site giderleri |
| GET | /api/expenses/{id} | Gider detayı |
| POST | /api/expenses | Yeni gider |
| PUT | /api/expenses/{id} | Gider güncelle |
| DELETE | /api/expenses/{id} | Gider sil |

### Tahsilatlar

| Metod | Endpoint | Açıklama |
|-------|----------|----------|
| POST | /api/payments | Tahsilat kaydet |
| POST | /api/payments/{id}/receipt | Makbuz oluştur |

### Sayaçlar

| Metod | Endpoint | Açıklama |
|-------|----------|----------|
| GET | /api/meters/site/{siteId} | Site sayaçları |
| POST | /api/meters/readings | Sayaç okuma gir |

---

## Teknolojiler

| Teknoloji | Sürüm | Kullanım |
|-----------|-------|----------|
| .NET | 8.0 | Ana platform |
| ASP.NET Core | 8.0 | Web API, MVC |
| Entity Framework Core | 8.0 | ORM, SQL Server |
| MS-SQL Server | 2019+ | Veritabanı |
| JWT Bearer | - | API kimlik doğrulama |
| System.IdentityModel.Tokens.Jwt | 8.15.0 | JWT işlemleri (güvenlik güncellemesi) |
| Microsoft.IdentityModel.Tokens | 8.15.0 | Token doğrulama |
| BCrypt.Net-Next | 4.0.3 | Şifre hash |
| MailKit | 4.3.0 | E-posta gönderimi |
| ClosedXML | - | Excel export |
| Swagger/OpenAPI | 3.0 | API dokümantasyonu |
| Bootstrap | 5.3 | Web arayüzü |
| Bootstrap Icons | 1.11 | İkonlar |
| jQuery | 3.7 | Form işlemleri |

---

## Sorun Giderme

### "Invalid object name 'UserSites'" hatası
Veritabanı kurulumu yapılmamıştır. [Veritabanı Kurulumu](#veritabanı-kurulumu) bölümünü takip edin.

### Bağlantı hatası
- SQL Server servisinin çalıştığından emin olun
- Connection string’deki Server, Database, User, Password değerlerini kontrol edin
- Firewall ayarlarını kontrol edin (uzak sunucu için)

### Port çakışması
`Properties/launchSettings.json` içinde farklı bir port tanımlayın.

### NU1902 güvenlik uyarısı (JWT paketleri)
`System.IdentityModel.Tokens.Jwt` ve `Microsoft.IdentityModel.Tokens` paketleri 8.15.0 veya üzeri sürüme güncellenmiştir. Eski sürümlerde bilinen güvenlik açıkları bulunmaktadır.

---

## Yapılan Güncellemeler

### Evrak Arşivi Modülü

Site bazlı evrak arşivi eklendi. Teklifler gibi sözleşme, belge ve benzeri evrakları saklamak için kullanılır.

| Özellik | Açıklama |
|---------|----------|
| **Entity** | `SiteDocument` (SiteId, Name, Description, FilePath, FileName) |
| **Veritabanı** | `SiteDocuments` tablosu |
| **Servis** | `ISiteDocumentService`, `SiteDocumentService` |
| **Controller** | `SiteDocumentsController` — Index, Create, Edit, Delete |
| **Dosya türleri** | PDF, Word (.doc, .docx), Excel (.xls, .xlsx) |
| **Dosya yolu** | `uploads/evraklar/{siteId}/{documentId}.{uzantı}` — SiteId ile ayrılmış |
| **Menü** | Teklifler'in yanında "Evrak Arşivi" linki |

**Kullanım:** Site seçin → Evrak Arşivi → Yeni Evrak → Evrak adı, açıklama ve dosya yükleyin.

---

### Tahsilat İptal Özelliği

Yanlış tahsil edilen aidatın iptal edilmesi için "Tahsilat İptal" özelliği eklendi.

| Özellik | Açıklama |
|---------|----------|
| **Servis** | `IPaymentService.DeleteAsync` — Tahsilatı soft delete ile iptal eder |
| **Controller** | `PaymentsController.Delete` (POST) |
| **UI** | Tahsilatlar sayfasında her tahsilat satırında **İptal** butonu |
| **Onay** | İptal öncesi onay penceresi |

**İptal sonrası otomatik güncellemeler:**
- Tahsilat kaydı silinir (soft delete)
- Aidat durumu "Bekliyor" / "Kısmi" olarak güncellenir; tekrar tahsil edilebilir
- Banka hesabı bakiyesi düşürülür
- Banka hareketi (BankTransaction) iptal edilir
- İlişkili makbuz iptal edilir
- Gider payı (ExpenseShare) tahsilatı ise PaidAmount geri alınır

**Kullanım:** Tahsilatlar sayfası → İlgili ay ve siteyi seçin → İptal etmek istediğiniz tahsilatın yanındaki **İptal** butonuna tıklayın → Onaylayın.

---

## Lisans

Bu proje eğitim ve kişisel kullanım amaçlıdır.
