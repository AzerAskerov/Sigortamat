# Sigortamat - Project Overview

Avtomatlaşdırılmış sığorta sistemi - Sığorta yoxlaması və WhatsApp mesaj avtomatlaşdırması + **YENİ**: Lead idarəetməsi və Telegram admin təsdiqi.

## 🆕 Yeni Xüsusiyyətlər (v0.3.0)
- 🤖 **Telegram Bot Approval**: Admin WhatsApp mesajları göndərilməzdən əvvəl Telegram vasitəsilə təsdiqləyir
- 📊 **Lead Management**: Potensial satış imkanları avtomatik aşkarlanır və izlənilir
- 📅 **Renewal Window Tracking**: Daha dəqiq yenilənmə tarix intervalları
- 🎯 **Enhanced Binary Search**: Şirkət dəyişiklikləri əsasında yenilənmə tarixləri axtarışı

## Başlatma
1. `appsettings.json`-da connection string və Telegram bot konfiqurasiyası qur
2. `dotnet run`
3. Dashboard: http://localhost:5000/hangfire
4. Telegram bot avtomatik başlayır və admin approval-ları gözləyir

## Stack
- .NET 9.0 + EF Core
- Hangfire + Azure SQL
- Selenium WebDriver
- **YENİ**: Telegram.Bot API
- **YENİ**: Lead & Notification Pipeline

## 🤖 Telegram Bot Konfiqurasiyası

```json
{
  "Telegram": {
    "BotToken": "8399345423:AAF9cf9mvp4il39G4N8_vQu6Xu-5cxkgKDM",
    "AdminId": 1762884854
  }
}
```

### Telegram Approval Axını
1. **Lead yaranır** (məs: sığorta tapılmır)
2. **Notification yaradılır** (pending status)
3. **Telegram bot admin-ə mesaj göndərir** təsdiqləmə düyməsi ilə
4. **Admin "✅ APPROVE" basır**
5. **WhatsApp queue-ya əlavə edilir**
6. **WhatsApp mesajı göndərilir**

## 🎯 İstifadə

### Əsas Proqram (Full Pipeline)
```bash
dotnet run
```

Bu komanda:
1. Avtomobil nömrələrini yoxlayır
2. Lead-ləri avtomatik yaradır
3. Telegram vasitəsilə admin təsdiqi alır
4. Təsdiqlənmiş mesajları WhatsApp vasitəsilə göndərir

## 📊 Lead Management Sistemi

### Lead Tipləri
- **NoInsuranceImmediate**: Dərhal sığorta tapılmır
- **RenewalWindow**: Yenilənmə tarixi müəyyənləşib
- **CompanyChange**: Sığorta şirkəti dəyişib

### Lead Yaratma Nümunəsi
```sql
-- Manual lead yaratma test üçün
INSERT INTO Users (CarNumber) VALUES ('TEST123');
DECLARE @UserId INT = SCOPE_IDENTITY();
INSERT INTO Leads (UserId, CarNumber, LeadType) VALUES (@UserId, 'TEST123', 'NoInsuranceImmediate');
```

## 🗄️ Azure SQL Database Configuration

### Connection String
```
Server: sigortayoxla.database.windows.net
Database: SigortamatDb
User: a.azar1988
Password: [configured]
```

### Database Schema (Yenilənmiş)
```sql
-- QueueItems table columns:
Id (int) - Primary key
Type (nvarchar) - 'insurance', 'whatsapp', 'whatsapp-notification'
CarNumber (nvarchar) - Avtomobil nömrəsi
PhoneNumber (nvarchar) - Telefon nömrəsi
Message (nvarchar) - WhatsApp mesajı
IsProcessed (bit) - İşlənib/işlənməyib
CreatedAt (datetime2) - Yaradılma tarixi
ProcessedAt (datetime2) - İşlənmə tarixi
Error (nvarchar) - Xəta mesajı

-- YENİ: Leads table
Id (int) - Primary key
UserId (int) - Foreign key to Users
CarNumber (nvarchar) - Avtomobil nömrəsi
LeadType (nvarchar) - 'NoInsuranceImmediate', 'RenewalWindow', etc.
Notes (nvarchar) - Əlavə qeydlər
CreatedAt (datetime2) - Yaradılma tarixi
IsConverted (bit) - Lead çevrildi/çevrilmədi

-- YENİ: Notifications table
Id (int) - Primary key
LeadId (int) - Foreign key to Leads
Channel (nvarchar) - 'wa' (WhatsApp)
Message (nvarchar) - Göndəriləcək mesaj
Status (nvarchar) - 'pending', 'approved', 'sent', 'error'
CreatedAt (datetime2) - Yaradılma tarixi
ApprovedAt (datetime2) - Təsdiqləmə tarixi
SentAt (datetime2) - Göndərilmə tarixi

-- YENİ: Users table extensions
RenewalWindowStart (datetime2) - Yenilənmə intervalının başlanğıcı
RenewalWindowEnd (datetime2) - Yenilənmə intervalının sonu
```

## 📁 Fayl Strukturu (Yenilənmiş)

```
sigortamat/
├── Program.cs              # Əsas proqram + Telegram bot
├── SigortaChecker.cs       # Selenium sığorta yoxlayıcısı
├── WhatsAppService.cs      # WhatsApp xidməti
├── Models/
│   ├── Lead.cs ⭐          # YENİ: Lead modeli
│   ├── Notification.cs ⭐   # YENİ: Notification modeli
│   └── User.cs             # User (+ renewal window sahələri)
├── Services/
│   ├── TelegramBotService.cs ⭐      # YENİ: Telegram bot
│   ├── LeadService.cs ⭐             # YENİ: Lead idarəetməsi
│   ├── NotificationService.cs ⭐     # YENİ: Notification approval
│   ├── RenewalTrackingService.cs    # Renewal tracking (enhanced)
│   ├── InsuranceService.cs          # Insurance checking
│   └── WhatsAppService.cs           # WhatsApp service
├── Jobs/
│   ├── TelegramBotHostedService.cs ⭐ # YENİ: Telegram background service
│   ├── InsuranceJob.cs              # Insurance job
│   └── WhatsAppJob.cs               # WhatsApp job
├── azure-sql-test.sql      # SQL query nümunələri
├── .vscode/
│   ├── settings.json       # MCP server konfiqurasiyası
│   └── mssql-connections.json # SQL Server bağlantıları
├── whatsapp-bot/
│   ├── package.json        # Node.js dependencies
│   ├── whatsapp-sender.js  # WhatsApp bot
│   ├── messages.json       # Bulk mesaj nümunəsi
│   └── auth_data/          # WhatsApp session məlumatları
└── README.md
```

## 📱 WhatsApp Bot Xüsusiyyətləri

- **QR Authentication**: İlk dəfə QR kod skan edin
- **Session Saxlanması**: Növbəti dəfələr avtomatik qoşulur
- **Bulk Messaging**: Çox mesaj paralel göndərə bilir
- **Error Handling**: Uğursuz mesajları qeyd edir
- **Rate Limiting**: Mesajlar arası 2 saniyə gözləmə
- **YENİ**: Admin approval integration ⭐

## 🤖 Telegram Bot Xüsusiyyətləri ⭐

- **Long-polling**: HTTP webhook tələb etmir
- **Inline Keyboard**: Təsdiqləmə düymələri
- **Admin Authorization**: Yalnız konfigurasiya edilmiş admin
- **Error Recovery**: Avtomatik reconnection
- **Real-time Approval**: Dərhal mesaj təsdiqi

## 🔄 Queue İşləri üçün İstifadə

Bu sistem təkrarlanan işlər üçün hazır hazırlanmışdır:

1. **Scheduled Jobs**: Cron job və ya Windows Task Scheduler ilə
2. **Message Queue**: RabbitMQ, Azure Service Bus və s. ilə inteqrasiya
3. **Database Integration**: Avtomobil-telefon mapping-i üçün
4. **YENİ**: Lead tracking və conversion analytics ⭐
5. **YENİ**: Telegram approval pipeline ⭐

## 🧪 Nümunə Test Avtomobil Nömrələri

Sınaq məqsədi ilə `setup_single_test.sql` və ya API testləri edərkən aşağıdakı dövlət qeydiyyat nişanlarından istənilən birini **təsadüfi** seçib istifadə etmək tövsiyə olunur. Bu nömrələr real istifadəçilə əlaqəli deyil və yalnız test üçün nəzərdə tutulub.

```
99JP083  99JL074  99JP086  99JL076  99JL075  90AM566
90AM533  99JP075  99JP087  77JG472  99JK047  99JP081
77JV167  99JS099  90JC930  90JK930  99JF483  99JV526
77JG327  77JK538  77JK590  99JF842  77JD145  77JB587
01AD795  01AD794  50CY385  55CE825  77QY058  77DX441
77RQ865  20CZ125  77KY920  74BB838  99ZY083
```

### YENİ: Bulk Test Data ⭐
15 maşın üçün bulk test data yaratmaq:
```bash
# SQL script işə sal
sqlcmd -S sigortayoxla.database.windows.net -d SigortamatDb -U a.azar1988 -P "54EhP6.G@RKcp8#" -i setup_bulk_test.sql
```

## ⚠️ Qeydlər

- Test rejimində öz telefon nömrənizi istifadə edin
- WhatsApp Business API qaydalarına riayət edin  
- Rate limiting-ə diqqət edin (spam kimi qəbul edilə bilər)
- Auth məlumatlarını (.auth_data/) git-ə commit etməyin
- Database credentials-ı production-da environment variables ilə idarə edin
- **YENİ**: Telegram bot token-unu təhlükəsiz saxlayın ⭐
- **YENİ**: Admin approval prosesini test edin ⭐ 