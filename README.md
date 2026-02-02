# Site Yönetim Sistemi
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

---

## Genel Bakış

Site Yönetim Sistemi, apartman ve site yöneticilerinin günlük işlemlerini dijital ortamda yönetmelerini sağlayan bir web uygulamasıdır. Site tanımlama, daire yönetimi, gider takibi, aidat tahsilatı, borç takibi ve raporlama gibi temel yönetim işlevlerini tek bir platformda sunar.

**Hedef Kullanıcılar:** Site yöneticileri, apartman yönetim şirketleri, kat malikleri dernekleri

---

## Özellikler

### Site ve Bina Yönetimi
- Site tanımlama (ad, adres, şehir, ilçe)
- Vergi bilgileri (vergi dairesi, vergi numarası)
- Gecikme zammı ayarları (oran, uygulama günü)
- Çok bloklu site desteği

### Daire Yönetimi
- Daire ekleme/düzenleme
- Blok ve kat bilgisi
- Pay oranı (aidat dağılımı için)
- Malik bilgileri (ad, telefon, e-posta)
- Daire bazlı aidat ödeme dönemi (başlangıç-bitiş günü)

### Gider Yönetimi
- Gider türleri (Elektrik, Su, Aidat vb.)
- Gider kaydı oluşturma
- Giderleri dairelere dağıtma (eşit pay veya pay oranına göre)
- Gider düzenleme ve silme
- Fatura numarası ve vade tarihi takibi

### Gider Paylaşımı (Borç)
- Daire bazlı borç listesi
- Gecikme zammı uygulama
- Borç durumu takibi (Bekleyen, Kısmen Ödendi, Ödendi)

### Gelir (Aidat)
- Site/daire bazlı aylık aidat tanımlama
- Her ayın 1'inde otomatik aidat oluşturma (HostedService)
- Aidat tahsilat kaydı
- Gelir tablosu ve raporlama

### Sayaç Yönetimi
- Su, elektrik, doğalgaz sayaçları
- Sayaç okuma girişi
- Sayaç geçmişi takibi

### Tahsilat ve Ödeme
- Ödeme kaydı (nakit, havale, kredi kartı)
- Makbuz oluşturma
- Banka hesabına bağlama

### Banka Hesapları
- Site banka hesapları
- Güncel bakiye takibi
- Tahsilatlar bakiyeyi artırır, gider ödemeleri azaltır
- Başlangıç bakiyesi (OpeningBalance) desteği

### Raporlar
- Aylık rapor (gelir, gider, bakiye)
- Yıllık rapor
- Aidat türlerini rapordan hariç tutma seçeneği

### Borçlular
- Tahsil edilmemiş gider ve aidat borcu olan daireler
- Borç tutarı özeti

### Destek Talepleri
- Daire sakininden yöneticiye destek talebi
- Öneri/şikayet formu
- Blok, kat, iletişim bilgileri
- Dosya eki desteği

### Öneri / Şikayet Formu (Public)
- Giriş gerektirmeyen URL: `/Feedback/Create?siteId=...` veya `/OneriSikayet?siteId=...`
- Blok No, Daire No, İsim Soyisim, Telefon, Konu, Açıklama alanları

### Kimlik Doğrulama
- JWT Bearer token
- Kayıt (Register)
- Giriş (Login)
- Token yenileme (Refresh Token)
- Cookie tabanlı web oturumu

### Diğer
- Anketler (tablolar hazır, geliştirme aşamasında)
- Üye yönetimi
- Çoklu site desteği (kullanıcı-site ilişkisi)

---

## Gereksinimler

- **.NET 8 SDK** — [İndir](https://dotnet.microsoft.com/download/dotnet/8.0)
- **MS-SQL Server** — 2019 veya üzeri (LocalDB, Express veya tam sürüm)
- **SQL Server Management Studio** (opsiyonel, manuel veritabanı kurulumu için)

---

## Proje Yapısı

```
SiteYonetim/
├── src/
│   ├── SiteYonetim.Domain/           # Entity'ler, interface'ler, domain modelleri
│   ├── SiteYonetim.Infrastructure/   # EF Core DbContext, servis implementasyonları, veri erişimi
│   ├── SiteYonetim.WebApi/           # ASP.NET Core Web API, MVC controller'lar, view'lar
│   │   ├── Areas/App/                 # Web panel (MVC)
│   │   │   ├── Controllers/
│   │   │   ├── Views/
│   │   │   └── Filters/
│   │   └── Controllers/              # REST API controller'lar
│   └── SiteYonetim.Tests/            # Birim testleri
├── database/
│   ├── Scripts/
│   │   └── Full-Schema.sql           # Tek dosyada tüm veritabanı şeması
│   └── Setup-Database.ps1            # Otomatik veritabanı kurulum scripti
├── SiteYonetim.sln
└── README.md
```

### Katmanlar
- **Domain:** İş kuralları, entity'ler, servis arayüzleri
- **Infrastructure:** Veritabanı erişimi, harici servis entegrasyonları
- **WebApi:** HTTP API, MVC sayfaları, kimlik doğrulama

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

### 3. Projeyi Derleme

```bash
dotnet build
```

---

## Veritabanı Kurulumu

Veritabanı tabloları oluşturulmadan uygulama çalışmaz. "Invalid object name 'UserSites'" gibi hatalar veritabanı kurulumunun yapılmadığını gösterir.

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

- **Veritabanı adı:** SiteYonetim
- **Collation:** Turkish_CI_AS
- **Script özelliği:** `IF NOT EXISTS` kullanıldığı için mevcut veritabanına tekrar çalıştırılabilir (idempotent)

---

## Uygulama Ayarları

`src/SiteYonetim.WebApi/appsettings.json` dosyasını düzenleyin:

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

---

## Çalıştırma

```bash
cd SiteYonetim
dotnet run --project src/SiteYonetim.WebApi
```

Veya Visual Studio / Rider ile F5 ile çalıştırın.

### Erişim Adresleri

| Adres | Açıklama |
|-------|----------|
| `https://localhost:7xxx` | Web paneli (giriş sayfası, dashboard) |
| `https://localhost:7xxx/swagger` | Swagger UI (API dokümantasyonu) |
| `https://localhost:7xxx/App/Dashboard` | Panel ana sayfa (giriş sonrası) |

Port numarası (7xxx) `launchSettings.json` veya çalışma ortamına göre değişir.

---

## Kullanım Kılavuzu

### İlk Kurulum Adımları

1. **Kayıt:** Swagger'dan `POST /api/auth/register` veya web arayüzünden kayıt olun
2. **Giriş:** E-posta ve şifre ile giriş yapın
3. **Site Ekle:** Sol menüden **Siteler** → **Yeni Site** ile ilk sitenizi ekleyin
4. **Daire Ekle:** **Daireler** menüsünden daireleri tanımlayın
5. **Gider Türü Ekle:** **Gider Türleri** menüsünden (Aidat, Elektrik, Su vb.) ekleyin
6. **Gider Ekle:** **Giderler** menüsünden gider kaydı oluşturun
7. **Dağıt:** Gideri dairelere dağıtın (Borçlar oluşur)
8. **Tahsilat:** **Tahsilatlar** menüsünden ödeme alın

### Menü Yapısı

| Menü | Açıklama |
|------|----------|
| Panel | Dashboard, site özeti |
| Siteler | Site listesi, yeni site ekleme |
| Daireler | Daire listesi, ekleme/düzenleme |
| Gelirler (Aidat) | Aylık aidat listesi, tahsilat |
| Gider Türleri | Gider türü tanımları |
| Giderler | Gider kayıtları, düzenleme, silme, dağıtım |
| Borçlar | Daire borçları, gecikme zammı |
| Tahsilatlar | Ödeme kayıtları |
| Banka Hesapları | Banka hesapları, bakiye |
| Sayaçlar | Sayaç tanımları, okuma girişi |
| Raporlar | Aylık/yıllık raporlar |
| Borçlular | Borçlu daire listesi |
| Destek Kayıtları | Destek talepleri |
| Üye Yönetimi | Kullanıcı yönetimi |

### Çoklu Site

Kullanıcılar birden fazla siteye atanabilir. Menüden site seçimi yapılarak ilgili siteye geçilir. Tek site varsa otomatik olarak o site seçilir.

---

## API Referansı

Tüm API'ler (auth hariç) `Authorization: Bearer {token}` header'ı gerektirir.

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
| POST | /api/expenses/{id}/distribute | Gideri dairelere dağıt |

### Borçlar (ExpenseShares)

| Metod | Endpoint | Açıklama |
|-------|----------|----------|
| GET | /api/expenseshares/site/{siteId} | Site borç listesi |
| POST | /api/expenseshares/site/{siteId}/apply-late-fees | Gecikme zammı uygula |

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
| BCrypt | - | Şifre hash |
| Swagger/OpenAPI | 3.0 | API dokümantasyonu |
| Bootstrap | 5.3 | Web arayüzü |
| Bootstrap Icons | 1.11 | İkonlar |

---

## Sorun Giderme

### "Invalid object name 'UserSites'" hatası
Veritabanı kurulumu yapılmamıştır. [Veritabanı Kurulumu](#veritabanı-kurulumu) bölümünü takip edin.

### Bağlantı hatası
- SQL Server servisinin çalıştığından emin olun
- Connection string'deki Server, Database, User, Password değerlerini kontrol edin
- Firewall ayarlarını kontrol edin (uzak sunucu için)

### Port çakışması
`Properties/launchSettings.json` içinde farklı bir port tanımlayın.

---

## Lisans

Bu proje eğitim ve kişisel kullanım amaçlıdır.
