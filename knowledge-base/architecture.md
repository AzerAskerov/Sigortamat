# 🚀 Sigorta Yoxla - Hangfire Background Job Sistemi

## 📋 Layihə Haqqında

Bu layihə **Hangfire** əsaslı background job sistemidir. Sistem avtomatik olaraq:
- 🚗 Avtomobil sığorta məlumatlarını yoxlayır
- 📱 WhatsApp vasitəsilə müştərilərə məlumat göndərir
- 📊 Queue əsaslı task idarəetməsi həyata keçirir
- 🔔 **YENİ**: Lead idarəetməsi və Telegram vasitəsilə admin təsdiqi
- 🤖 **YENİ**: Telegram bot ilə notification təsdiqləmə sistemi

## 🏗️ Arxitektura Sxemi

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           SİGORTA YOXLA SİSTEMİ                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐  ┌─────────────┐    │
│  │   Program   │    │   Models    │    │  Services   │  │   Jobs      │    │
│  │             │    │             │    │             │  │             │    │
│  │ - Main()    │    │ User        │    │QueueRepo    │  │InsuranceJob │    │
│  │ - Hangfire  │    │ Queue       │    │Insurance    │  │WhatsAppJob  │    │
│  │   Config    │    │ Lead ⭐     │    │WhatsApp     │  │TelegramJob⭐│    │
│  │ - Telegram  │    │ Notification│    │Telegram⭐   │  │             │    │
│  │   Bot ⭐    │    │             │    │Lead⭐       │  │             │    │
│  └─────────────┘    └─────────────┘    └─────────────┘  └─────────────┘    │
│         │                   │                   │               │          │
│         └───────────────────┼───────────────────┼───────────────┘          │
│                             │                   │                          │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                      HANGFIRE JOBS                                 │   │
│  │                                                                     │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────────┐  │   │
│  │  │InsuranceJob │  │WhatsAppJob  │  │     TelegramBotService      │  │   │
│  │  │             │  │             │  │                             │  │   │
│  │  │Hər dəqiqə   │  │Hər 2 dəqiqə │  │ Admin approval requests     │  │   │
│  │  │işləyir      │  │işləyir      │  │ Lead notifications ⭐       │  │   │
│  │  └─────────────┘  └─────────────┘  └─────────────────────────────┘  │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                             │                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                    LEAD & NOTIFICATION FLOW ⭐                     │   │
│  │                                                                     │   │
│  │  Lead Created → Notification (Pending) → Telegram Approval →       │   │
│  │  Admin Approval → WhatsApp Queue → Message Sent                    │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                             │                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                 XARICI SİSTEMLƏR                                    │   │
│  │                                                                     │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────────┐  │   │
│  │  │  Selenium   │  │ WhatsApp    │  │     Telegram Bot API ⭐     │  │   │
│  │  │ (Sığorta    │  │ Web.js      │  │                             │  │   │
│  │  │  Saytları)  │  │ (Node.js)   │  │ Long-polling updates        │  │   │
│  │  └─────────────┘  └─────────────┘  └─────────────────────────────┘  │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

⭐ **Yeni Komponentlər** - 0.3.0 versiyasında əlavə edilib

## 📁 Layihə Strukturu

```
sigortaYoxla/
├── 📂 Models/
│   ├── Queue.cs                      # Queue məlumat modeli (Priority, RetryCount, ProcessAfter)
│   ├── InsuranceRenewalTracking.cs   # Yenilənmə izləmə prosesi
│   ├── User.cs                       # Avtomobil/istifadəçi (+ RenewalWindow sahələri ⭐)
│   ├── Lead.cs ⭐                    # Potensial satış lead-ləri
│   └── Notification.cs ⭐            # Notification təsdiqi sistemi
├── 📂 Services/
│   ├── QueueRepository.cs            # Queue idarəetməsi
│   ├── InsuranceService.cs           # Sığorta yoxlama xidməti
│   ├── WhatsAppService.cs            # WhatsApp mesaj göndərmə
│   ├── RenewalTrackingService.cs     # Yenilənmə tarixini təyin edən servis
│   ├── TelegramBotService.cs ⭐      # Telegram bot approval sistemi
│   ├── LeadService.cs ⭐             # Lead yaratma və idarəetmə
│   └── NotificationService.cs ⭐     # Notification approval idarəetməsi
├── 📂 Jobs/
│   ├── InsuranceJobHandler.cs        # Sığorta background job-u
│   ├── WhatsAppJob.cs                # WhatsApp background job-u (*/2 cron)
│   └── TelegramBotHostedService.cs ⭐ # Telegram bot background service
├── 📂 whatsapp-bot/
│   ├── debug-whatsapp.js             # WhatsApp Web.js inteqrasiyası
│   └── package.json                  # Node.js dependencies
├── Program.cs                        # Ana proqram (Hangfire + Telegram host + logging + DI)
└── Sigortamat.csproj                # .NET layihə faylı
```

## 🔧 Texniki Spesifikasiyalar

### Framework və Kitabxanalar
- **Platform**: .NET 9.0 Console Application
- **Background Jobs**: Hangfire Framework
- **Storage**: Hangfire SQL Server + EF Core
- **Web Automation**: Selenium WebDriver
- **Messaging**: WhatsApp Web.js (Node.js)
- **Bot Framework**: Telegram.Bot ⭐
- **Hosted Services**: IHostedService for Telegram bot ⭐

### Packages
```xml
<!-- Hangfire -->
<PackageReference Include="Hangfire.Core" Version="1.8.17" />
<PackageReference Include="Hangfire.SqlServer" Version="1.8.17" />
<PackageReference Include="Hangfire.AspNetCore" Version="1.8.17" />

<!-- EF Core -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.0" />

<!-- Selenium -->
<PackageReference Include="DotNetSeleniumExtras.WaitHelpers" Version="3.11.0" />
<PackageReference Include="Selenium.Support" Version="4.34.0" />
<PackageReference Include="Selenium.WebDriver" Version="4.34.0" />
<PackageReference Include="Selenium.WebDriver.ChromeDriver" Version="138.0.7204.9400" />

<!-- Telegram Bot ⭐ -->
<PackageReference Include="Telegram.Bot" Version="21.0.0" />
```

## 🔔 YENİ: Lead & Notification Sistemi

### Lead Yaranma Ssenarilərə
1. **NoInsuranceImmediate**: İlk sığorta sorğusunda məlumat tapılmır
2. **RenewalWindow**: Sığorta yenilənmə tarixi müəyyənləşir
3. **CompanyChange**: Sığorta şirkəti dəyişikliyi aşkarlanır

### Notification Approval Axını
```
Lead Yaranır → Notification (WaitingApprove) → Telegram bot admin-ə göndərir
    ↓
Admin "✅ APPROVE" düyməsini basır → Status: Approved → WhatsApp Queue
    ↓
WhatsApp Job mesajı göndərir → Status: Sent
```

### Telegram Bot Xüsusiyyətləri
- **Long-polling**: HTTP webhook yox, birbaşa polling
- **Inline Keyboard**: Təsdiqləmə düymələri
- **Admin Chat ID**: Konfiqurasiyada təyin edilir
- **Error Handling**: Reconnection və retry mexanizmləri

## 🗂️ YENİ Komponentlər Təfsilatı

### 1. 📄 Models/Lead.cs ⭐
**Məqsəd**: Potensial satış imkanları üçün data model

```csharp
public class Lead
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CarNumber { get; set; }
    public string LeadType { get; set; }    // "NoInsuranceImmediate", "RenewalWindow", etc.
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsConverted { get; set; }
}
```

### 2. 📄 Models/Notification.cs ⭐
**Məqsəd**: Admin təsdiqi tələb edən bildirişlər

```csharp
public class Notification
{
    public int Id { get; set; }
    public int LeadId { get; set; }
    public string Channel { get; set; } = "wa";  // whatsapp
    public string Message { get; set; }
    public string Status { get; set; }          // pending, approved, sent, error
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? SentAt { get; set; }
}
```

### 3. 🔧 Services/TelegramBotService.cs ⭐
**Məqsəd**: Admin approval automation

**Funksiyalar**:
- `SendApprovalRequestAsync(Notification)` - Admin-ə təsdiqləmə istəyi
- `HandleCallbackAsync(string callbackData)` - Düymə basılması işlənməsi

**Konfiqurasiya**:
```json
{
  "Telegram": {
    "BotToken": "8399345423:AAF9cf9mvp4il39G4N8_vQu6Xu-5cxkgKDM",
    "AdminId": 1762884854
  }
}
```

### 4. 🔧 Services/LeadService.cs ⭐
**Məqsəd**: Lead yaratma və idarəetmə

**Funksiyalar**:
- `CreateLeadWithNotificationAsync()` - Lead + Notification yaradır
- `ConvertLeadAsync()` - Lead-i "converted" vəziyyətinə keçirir

### 5. ⚙️ Jobs/TelegramBotHostedService.cs ⭐
**Məqsəd**: Telegram bot background service

**Xüsusiyyətlər**:
- **IHostedService** inteqrasiyası
- **Long-polling** Telegram API updates
- **Callback handling** approval düymələri üçün
- **Error recovery** və reconnection

## 🔄 YENİ İş Axını (Enhanced Workflow)

### 1. Lead Yaranma Axını ⭐
```
InsuranceService məlumat yoxlayır
    ↓
Əgər məlumat yoxdursa:
    ↓
LeadService.CreateLeadWithNotificationAsync()
    ↓
Lead (NoInsuranceImmediate) + Notification (pending) yaradılır
    ↓
TelegramBotService admin-ə approval istəyi göndərir
```

### 2. Approval Axını ⭐
```
Admin Telegram-da "✅ APPROVE" basır
    ↓
TelegramBotHostedService callback alır
    ↓
NotificationService.ApproveAsync(notificationId)
    ↓
Notification status: approved, WhatsApp Queue-ya əlavə edilir
    ↓
WhatsApp Job mesajı göndərir, status: sent
```

### 3. Renewal Window Tracking ⭐
```
RenewalTrackingService renewal tarixi tapır
    ↓
User.RenewalWindowStart & RenewalWindowEnd yenilənir
    ↓
Lead yaradılır (RenewalWindow type)
    ↓
Notification approval prosesi başlayır
```

## 📊 YENİ Verilənlər Bazası Cədvəlləri

### Leads Cədvəli ⭐
```sql
CREATE TABLE Leads (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL REFERENCES Users(Id),
    CarNumber NVARCHAR(20) NOT NULL,
    LeadType NVARCHAR(50) NOT NULL,
    Notes NVARCHAR(MAX) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    IsConverted BIT NOT NULL DEFAULT 0
);
```

### Notifications Cədvəli ⭐
```sql
CREATE TABLE Notifications (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    LeadId INT NOT NULL REFERENCES Leads(Id) ON DELETE CASCADE,
    Channel NVARCHAR(10) NOT NULL DEFAULT 'wa',
    Message NVARCHAR(2000) NOT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'pending',
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    ApprovedAt DATETIME NULL,
    SentAt DATETIME NULL
);
```

### Users Cədvəli Yeniləmələri ⭐
```sql
-- Əlavə edilən sahələr
ALTER TABLE Users ADD 
    RenewalWindowStart DATETIME NULL,
    RenewalWindowEnd DATETIME NULL;
```

## 🎮 YENİ İstifadə Təlimatları

### Telegram Bot Qurulumu ⭐
```bash
# Bot token və admin ID konfiqurasiyası
# appsettings.json-da təyin edin

# Sistem başladıldıqda bot avtomatik aktivləşir
dotnet run
```

### Lead Yaratma Test ⭐
```sql
-- Manual lead yaratma
INSERT INTO Users (CarNumber) VALUES ('TEST123');
DECLARE @UserId INT = SCOPE_IDENTITY();
INSERT INTO Leads (UserId, CarNumber, LeadType) VALUES (@UserId, 'TEST123', 'NoInsuranceImmediate');
```

### Notification Status Yoxlama ⭐
```sql
-- Pending notifications
SELECT l.CarNumber, n.Message, n.Status, n.CreatedAt 
FROM Notifications n 
JOIN Leads l ON n.LeadId = l.Id 
WHERE n.Status = 'pending' 
ORDER BY n.CreatedAt DESC;
```

## 🚀 Production Hazırlığı

### YENİ Tələblər ⭐
1. **Telegram Bot API Konfiqurasiyası**:
   - Bot token Environment variables-da saxlanmalı
   - Admin chat ID-si konfiqurasiya edilməli

2. **Notification Monitoring**:
   - Pending notifications alertləri
   - Failed approval retry məkanimzəsi

3. **Lead Analytics**:
   - Conversion rate tracking
   - Lead source analysis
   - ROI metrics

## ⚡ Performance Xüsusiyyətləri (Yenilənmiş)

- **Concurrent Processing**: Insurance, WhatsApp, və Telegram job-ları paralel
- **Lead Pipeline**: Asynchronous lead processing və notification queue
- **Telegram Long-polling**: Real-time approval without webhooks ⭐
- **Smart Binary Search**: Enhanced MonthSearch company-based strategy ⭐
- **Queue Separation**: insurance, whatsapp, və notification queue-ları ayrıca ⭐

## 🔒 Təhlükəsizlik (Yenilənmiş)

- **Telegram Bot Token Security**: Environment variables istifadəsi ⭐
- **Admin Authorization**: Yalnız konfiqurasiya edilmiş admin ID ⭐
- **Callback Data Validation**: Malicious callback prevention ⭐
- **Process Isolation**: Node.js və Telegram bot ayrıca thread-lər
- **Error Boundaries**: Hər komponent öz error handling-ə malik

Bu arxitektura layihənin bütün komponentlərini, o cümlədən yeni lead idarəetməsi və Telegram approval sistemini əhatə edir. 🎯⭐

# SigortaYoxla - Arxitektura

## Stack
- .NET 9.0
- Entity Framework Core 
- Hangfire
- Azure SQL Database
- **YENİ**: Telegram.Bot ⭐
- **YENİ**: Lead Management ⭐

## Komponentlər
- Console App
- Background Jobs (Hangfire)
- Database (Azure SQL)
- Dashboard (http://localhost:5000/hangfire)
- **YENİ**: Telegram Bot Approval ⭐
- **YENİ**: Lead & Notification Pipeline ⭐

## 🆕 Lead & Notification Pipeline (qısa icmal)
Sistemdə *lead* yarandıqda adminə Telegram vasitəsilə təsdiqləmə (approval) gedişi və təsdiqlənmiş bildirişlərin WhatsApp queue-ya ötürülməsi mexanizmi əlavə edilib. Ətraflı bax: `lead-notifications.md`.

## Queue Sistemi
- Persistent queue (SQL)
- Insurance job - hər dəqiqə
- WhatsApp job - hər 2 dəqiqə
- **YENİ**: Telegram approval system ⭐
- **YENİ**: Lead generation workflow ⭐ 