# 🚀 Sigorta Yoxla - Hangfire Background Job Sistemi

## 📋 Layihə Haqqında

Bu layihə **Hangfire** əsaslı background job sistemidir. Sistem avtomatik olaraq:
- 🚗 Avtomobil sığorta məlumatlarını yoxlayır
- 📱 WhatsApp vasitəsilə müştərilərə məlumat göndərir
- 📊 Queue əsaslı task idarəetməsi həyata keçirir

## 🏗️ Arxitektura Sxemi

```
┌─────────────────────────────────────────────────────────────┐
│                     SİGORTA YOXLA SİSTEMİ                   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐     │
│  │   Program   │    │   Models    │    │  Services   │     │
│  │             │    │             │    │             │     │
│  │ - Main()    │    │ QueueItem   │    │QueueRepo    │     │
│  │ - Hangfire  │    │             │    │Insurance    │     │
│  │   Config    │    │             │    │WhatsApp     │     │
│  └─────────────┘    └─────────────┘    └─────────────┘     │
│         │                   │                   │          │
│         └───────────────────┼───────────────────┘          │
│                             │                              │
│  ┌─────────────────────────────────────────────────────┐   │
│  │                HANGFIRE JOBS                        │   │
│  │                                                     │   │
│  │  ┌─────────────┐              ┌─────────────┐      │   │
│  │  │InsuranceJob │              │WhatsAppJob  │      │   │
│  │  │             │              │             │      │   │
│  │  │Hər dəqiqə   │              │Hər 2 dəqiqə │      │   │
│  │  │işləyir      │              │işləyir      │      │   │
│  │  └─────────────┘              └─────────────┘      │   │
│  └─────────────────────────────────────────────────────┘   │
│                             │                              │
│  ┌─────────────────────────────────────────────────────┐   │
│  │                 XARICI SİSTEMLƏR                    │   │
│  │                                                     │   │
│  │  ┌─────────────┐              ┌─────────────┐      │   │
│  │  │  Selenium   │              │ WhatsApp    │      │   │
│  │  │ (Sığorta    │              │ Web.js      │      │   │
│  │  │  Saytları)  │              │ (Node.js)   │      │   │
│  │  └─────────────┘              └─────────────┘      │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

## 📁 Layihə Strukturu

```
sigortaYoxla/
├── 📂 Models/
│   ├── Queue.cs                      # Queue məlumat modeli (yeni sahələr: Priority, RetryCount, ProcessAfter ...)
│   ├── InsuranceRenewalTracking.cs   # Yenilənmə izləmə prosesi
│   └── User.cs                       # Avtomobil/istifadəçi məlumatları
├── 📂 Services/
│   ├── QueueRepository.cs            # Queue idarəetməsi
│   ├── InsuranceService.cs           # Sığorta yoxlama xidməti
│   ├── WhatsAppService.cs            # WhatsApp mesaj göndərmə
│   └── RenewalTrackingService.cs     # Yenilənmə tarixini təyin edən servis
├── 📂 Jobs/
│   ├── InsuranceJobHandler.cs        # Sığorta background job-u
│   └── WhatsAppJob.cs                # WhatsApp background job-u (*/2 cron)
├── 📂 whatsapp-bot/
│   ├── debug-whatsapp.js             # WhatsApp Web.js inteqrasiyası
│   └── package.json                  # Node.js dependencies
├── Program.cs                        # Ana proqram (Hangfire host + logging + DI)
└── Sigortamat.csproj                # .NET layihə faylı
```

## 🔧 Texniki Spesifikasiyalar

### Framework və Kitabxanalar
- **Platform**: .NET 9.0 Console Application
- **Background Jobs**: Hangfire Framework
- **Storage**: Hangfire InMemory (development üçün)
- **Web Automation**: Selenium WebDriver
- **Messaging**: WhatsApp Web.js (Node.js)

### Packages
```xml
<!-- Hangfire -->
<PackageReference Include="Hangfire.Core" Version="1.8.17" />
<PackageReference Include="Hangfire.InMemory" Version="1.0.0" />
<PackageReference Include="Hangfire.SqlServer" Version="1.8.17" />
<PackageReference Include="Hangfire.AspNetCore" Version="1.8.17" />

<!-- Selenium -->
<PackageReference Include="DotNetSeleniumExtras.WaitHelpers" Version="3.11.0" />
<PackageReference Include="Selenium.Support" Version="4.34.0" />
<PackageReference Include="Selenium.WebDriver" Version="4.34.0" />
<PackageReference Include="Selenium.WebDriver.ChromeDriver" Version="138.0.7204.9400" />
```

## 🗂️ Komponentlər Təfsilatı

### 1. 📄 Models/QueueItem.cs
**Məqsəd**: Queue elementləri üçün data model

```csharp
public class QueueItem
{
    public string Id { get; set; }
    public string Type { get; set; }        // "insurance" / "whatsapp"
    public string CarNumber { get; set; }
    public string PhoneNumber { get; set; }
    public string Message { get; set; }
    public bool IsProcessed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
```

**Xüsusiyyətlər**:
- ✅ Queue tipini təyin edir (insurance/whatsapp)
- ✅ İşləmə statusunu izləyir
- ✅ Yaradılma və işlənmə tarixlərini saxlayır

### 2. 🔧 Services/QueueRepository.cs
**Məqsəd**: Queue məlumatlarının idarəetməsi

**Funksiyalar**:
- `SeedTestData()` - Test məlumatları yaradır
- `GetUnprocessedInsuranceItems()` - İşlənməmiş sığorta queue-ları
- `GetUnprocessedWhatsAppItems()` - İşlənməmiş WhatsApp queue-ları
- `MarkAsProcessed()` - Queue-nu işlənmiş kimi işarələyir
- `ShowQueueStatus()` - Queue statusunu göstərir

**Xüsusiyyətlər**:
- ✅ Static metodlar (Hangfire uyğunluğu üçün)
- ✅ In-memory data storage simulasiyası
- ✅ Thread-safe əməliyyatlar

### 3. 🚗 Services/InsuranceService.cs
**Məqsəd**: Sığorta məlumatlarını yoxlama

**Funksiyalar**:
- `CheckInsuranceAsync(string carNumber)` - Avtomobil sığortasını yoxlayır

**Texniki detallar**:
- ✅ Async/await pattern
- ✅ Selenium WebDriver inteqrasiyası (gələcək)
- ✅ Məlumat formatlaması və queue-ya WhatsApp mesajı əlavə etməsi

### 4. 📱 Services/WhatsAppService.cs
**Məqsəd**: WhatsApp mesaj göndərmə

**Funksiyalar**:
- `SendMessageAsync(string phoneNumber, string message)` - WhatsApp mesajı göndərir

**Texniki detallar**:
- ✅ Node.js process çağırışı
- ✅ debug-whatsapp.js tool inteqrasiyası
- ✅ Error handling və logging

### 5. ⚙️ Jobs/InsuranceJob.cs
**Məqsəd**: Sığorta yoxlama background job-u

**Scheduler**: Hər dəqiqə işləyir (`Cron.Minutely`)

**İş axını**:
1. Queue-dan işlənməmiş sığorta elementlərini alır
2. Hər biri üçün `InsuranceService.CheckInsuranceAsync()` çağırır
3. Nəticəni WhatsApp queue-ya əlavə edir
4. Element statusunu işlənmiş kimi dəyişir

### 6. 📲 Jobs/WhatsAppJob.cs
**Məqsəd**: WhatsApp mesaj göndərmə background job-u

**Scheduler**: Hər 2 dəqiqə işləyir (`*/2 * * * *`)

**İş axını**:
1. Queue-dan işlənməmiş WhatsApp elementlərini alır
2. Hər biri üçün `WhatsAppService.SendMessageAsync()` çağırır
3. Element statusunu işlənmiş kimi dəyişir

### 7. 🎯 Program.cs
**Məqsəd**: Ana proqram və Hangfire host

**Komponentlər**:
- **Hangfire Configuration**: InMemory storage və server options
- **Recurring Jobs Setup**: Insurance və WhatsApp job-larının qurulması
- **Interactive Console**: ENTER ilə queue status, ESCAPE ilə çıxış

## 🔄 İş Axını (Workflow)

### 1. Sistem Başlanğıcı
```
Program.Main() başlayır
    ↓
QueueRepository.SeedTestData() - Test məlumatları yüklənir
    ↓
Hangfire konfiqurasiyası (InMemory storage)
    ↓
BackgroundJobServer başlayır (2 worker thread)
    ↓
Recurring job-lar qurulur:
    • InsuranceJobHandler - hər dəqiqə
    • WhatsAppJob - hər 2 dəqiqə
    ↓
Manual test job-ları əlavə edilir
    ↓
Interactive console loop başlayır
```

### 2. Sığorta Job Dövriyyəsi
```
InsuranceJob.ProcessInsuranceQueue() (hər dəqiqə)
    ↓
QueueRepository.GetUnprocessedInsuranceItems()
    ↓
Hər element üçün:
    ↓
InsuranceService.CheckInsuranceAsync(carNumber)
    ↓
Selenium WebDriver ilə sığorta saytına daxil olur
    ↓
Məlumatları əldə edir və formatlaşdırır
    ↓
WhatsApp queue-ya mesaj əlavə edir
    ↓
QueueRepository.MarkAsProcessed(elementId)
```

### 3. WhatsApp Job Dövriyyəsi
```
WhatsAppJob.ProcessWhatsAppQueue() (hər 2 dəqiqə)
    ↓
QueueRepository.GetUnprocessedWhatsAppItems()
    ↓
Hər element üçün:
    ↓
WhatsAppService.SendMessageAsync(phone, message)
    ↓
Node.js process başladır: debug-whatsapp.js
    ↓
WhatsApp Web.js ilə mesaj göndərilir
    ↓
QueueRepository.MarkAsProcessed(elementId)
```

## 🎮 İstifadə Təlimatları

### Sistemin İşə Salınması
```bash
# Layihəni build et
dotnet build

# Sistemi işə sal
dotnet run
```

### İnteraktiv Komandalar
- **ENTER** - Queue statusunu göstər
- **ESCAPE** - Sistemdən çıx
- **CTRL+C** - Sistemi dayandır

### Queue Status Nümunəsi
```
📊 QUEUE STATUS:
==================================================
📋 Ümumi: 6
✅ Proses olunmuş: 4
⏳ Gözləyən: 2
```

## 🚀 Production Hazırlığı

### Qarşıdan Gələn Dəyişikliklər

1. **Database Inteqrasiyası**:
   ```csharp
   // InMemory-dən SQL Server-ə keçid
   .UseSqlServerStorage(connectionString)
   ```

2. **Real Sığorta Sayt Inteqrasiyası**:
   - Selenium WebDriver ilə real saytlara daxil olma
   - CAPTCHA həlli mexanizmləri
   - Error handling və retry logic

3. **Monitoring və Logging**:
   - Hangfire Dashboard əlavə edilməsi
   - Application Insights inteqrasiyası
   - Custom metrics və alertlər

4. **Scalability**:
   - Multiple worker instances
   - Redis-based Hangfire storage
   - Load balancing

## 🛠️ Development Setup

### Tələblər
- .NET 9.0 SDK
- Node.js (WhatsApp Web.js üçün)
- Chrome/Chromium (Selenium üçün)

### WhatsApp Bot Setup
```bash
cd whatsapp-bot
npm install
```

### Test Məlumatları
Sistem başlayanda avtomatik olaraq test məlumatları yüklənir:
- 3 sığorta yoxlama queue-u
- 3 WhatsApp mesaj queue-u

## ⚡ Performance Xüsusiyyətləri

- **Concurrent Processing**: 2 parallel worker thread
- **Queue Separation**: insurance və whatsapp queue-ları ayrıca
- **Memory Efficient**: InMemory storage minimal yaddaş istifadəsi
- **Error Handling**: Job failure automatik retry mexanizmi
- **Scheduling**: Cron-based recurring job sistemi

## 🔒 Təhlükəsizlik

- **Process Isolation**: Node.js process ayrıca thread-də işləyir
- **Error Boundaries**: Hər job öz error handling-ə malikdir
- **Data Validation**: Queue elementləri validate edilir
- **Safe Shutdown**: Graceful application termination

Bu arxitektura layihənin bütün komponentlərini əhatə edir və gələcək inkişaf üçün möhkəm baza təşkil edir. 🎯

# SigortaYoxla - Arxitektura

## Stack
- .NET 9.0
- Entity Framework Core 
- Hangfire
- Azure SQL Database

## Komponentlər
- Console App
- Background Jobs (Hangfire)
- Database (Azure SQL)
- Dashboard (http://localhost:5000/hangfire)

## Queue Sistemi
- Persistent queue (SQL)
- Insurance job - hər dəqiqə
- WhatsApp job - hər 2 dəqiqə
