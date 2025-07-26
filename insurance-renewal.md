# Sığorta Yenilənmə Tarixi İzləmə Sistemi

## Sistemin Əsas Prinsipi

Bu sistem avtomobil sığortasının yenilənmə tarixini avtomatik təyin edir:
- ISB.az saytında müxtəlif tarixlərlə sorğular göndərir
- Sığorta vəziyyətindəki dəyişiklikləri izləyir
- İkili axtarış alqoritmi ilə yenilənmə tarixini təxmin edir
- ISB.az gündəlik 3 sorğu limitinə hörmət edir (`ProcessAfter` sahəsi ilə təxirə salma)
- **YENİ**: Lead yaratma və Telegram vasitəsilə admin təsdiqi sistemi

## Verilənlər Bazası Strukturu

### Yeni Cədvəllər
```sql
-- İstifadəçilər və yenilənmə tarixləri (yenilənmiş)
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CarNumber NVARCHAR(20) NOT NULL UNIQUE,
    PhoneNumber NVARCHAR(20) NULL,
    NotificationEnabled BIT NOT NULL DEFAULT 1,
    EstimatedRenewalDay INT NULL,
    EstimatedRenewalMonth INT NULL,
    LastConfirmedRenewalDate DATETIME NULL,
    RenewalWindowStart DATETIME NULL,      -- YENİ
    RenewalWindowEnd DATETIME NULL,        -- YENİ
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NULL
)

-- İzləmə prosesi məlumatları
CREATE TABLE InsuranceRenewalTracking (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    CurrentPhase NVARCHAR(20) NOT NULL, -- "Initial", "YearSearch", "MonthSearch", "FinalCheck", "Completed"
    LastCheckDate DATETIME NULL,
    NextCheckDate DATETIME NULL,
    ChecksPerformed INT NOT NULL DEFAULT 0,
    LastCheckResult NVARCHAR(MAX) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
)

-- YENİ: Potensial satış lead-ləri
CREATE TABLE Leads (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    CarNumber NVARCHAR(20) NOT NULL,
    LeadType NVARCHAR(50) NOT NULL,     -- 'NoInsuranceImmediate', 'RenewalWindow', etc.
    Notes NVARCHAR(MAX) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    IsConverted BIT NOT NULL DEFAULT 0,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
)

-- YENİ: Notification approval sistemi
CREATE TABLE Notifications (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    LeadId INT NOT NULL,
    Channel NVARCHAR(10) NOT NULL DEFAULT 'wa',    -- 'wa' (WhatsApp)
    Message NVARCHAR(2000) NOT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'pending', -- 'pending', 'approved', 'sent', 'error'
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    ApprovedAt DATETIME NULL,
    SentAt DATETIME NULL,
    FOREIGN KEY (LeadId) REFERENCES Leads(Id) ON DELETE CASCADE
)
```

### Mövcud Cədvəllərə Əlavələr
```sql
-- Queues cədvəlinə
ALTER TABLE Queues ADD 
    ProcessAfter DATETIME NULL,
    Priority INT NOT NULL DEFAULT 1,
    RetryCount INT NOT NULL DEFAULT 0,
    ErrorMessage NVARCHAR(MAX) NULL,
    CompletedAt DATETIME NULL,
    StartedAt DATETIME NULL,
    UpdatedAt DATETIME NULL;

-- InsuranceJobs cədvəlinə
ALTER TABLE InsuranceJobs ADD 
    CheckDate DATETIME NULL,
    InsuranceRenewalTrackingId INT NULL,
    Status NVARCHAR(20) NULL,
    ProcessingTimeMs INT NULL,
    ResultText NVARCHAR(MAX) NULL,
    VehicleBrand NVARCHAR(100) NULL,
    VehicleModel NVARCHAR(100) NULL,
    Company NVARCHAR(150) NULL,
    ProcessedAt DATETIME NULL;
```

## Model Sinifləri (Yenilənmiş)

```csharp
public class User
{
    public int Id { get; set; }
    public string CarNumber { get; set; }
    public string? PhoneNumber { get; set; }
    public bool NotificationEnabled { get; set; } = true;
    public int? EstimatedRenewalDay { get; set; }
    public int? EstimatedRenewalMonth { get; set; }
    public DateTime? LastConfirmedRenewalDate { get; set; }
    public DateTime? RenewalWindowStart { get; set; }    // YENİ
    public DateTime? RenewalWindowEnd { get; set; }      // YENİ
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}

public class InsuranceRenewalTracking
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CurrentPhase { get; set; }
    public DateTime? LastCheckDate { get; set; }
    public DateTime? NextCheckDate { get; set; }
    public int ChecksPerformed { get; set; } = 0;
    public string? LastCheckResult { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public User User { get; set; }
}

// YENİ: Lead modeli
public class Lead
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CarNumber { get; set; } = string.Empty;
    public string LeadType { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsConverted { get; set; } = false;
    public User User { get; set; }
}

// YENİ: Notification modeli
public class Notification
{
    public int Id { get; set; }
    public int LeadId { get; set; }
    public string Channel { get; set; } = "wa";
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public Lead Lead { get; set; }
}
```

## Əsas Proses Fazaları (Yenilənmiş)

### Step 1: İlkin Yoxlama [TƏSDIQLƏNIB]
1. İstifadəçi yaradılır (`Users` cədvəli)
2. İzləmə qeydi yaradılır (`CurrentPhase = "Initial"`)
3. Cari tarixlə ilk sığorta yoxlaması
4. **YENİ**: Əgər məlumat tapılmırsa → Lead yaradılır (`NoInsuranceImmediate`)

### Step 2: İl Axtarışı [TƏSDIQLƏNIB]
1. Əvvəlki illəri ardıcıl yoxlayır
2. Sığorta vəziyyətində dəyişiklik axtarır
3. İl sərhədi tapıldıqda `MonthSearch` fazasına keçir

### Step 3: Ay Axtarışı [TƏSDIQLƏNIB - Enhanced]
1. **İkili axtarış alqoritmi** istifadə edir (Strategy 1: VAR/YOX, Strategy 2: Company-based)
2. Tarix intervalını tədricən daraldır
3. **14 gün** dəqiqliyinə çatdıqda `FinalCheck` fazasına keçir (əvvəlki 31 gün yerinə)

### Step 4: FinalCheck Fazası [TƏSDIQLƏNIB - Enhanced]
1. Təxmini yenilənmə tarixi hesablanır
2. İstifadəçi məlumatları yenilənir (gün/ay + **renewal window**)
3. **YENİ**: Lead yaradılır (`RenewalWindow` tipi)
4. Status `Completed`-ə keçir

### Step 5: Bildiriş Sistemi [İCRA OLUNUR]
Lead və Notification approval boru kəməri artıq ayrıca sənəddə izah edilib – bax: `lead-notifications.md`. Bu mərhələ həmin boru kəmərinə keçidi hazırlayır.

#### 5.1 Lead Yaratma Ssenarilərə
- **NoInsuranceImmediate**: İlk yoxlamada sığorta tapılmır
- **RenewalWindow**: Yenilənmə tarixi müəyyənləşir
- **CompanyChange**: Sığorta şirkəti dəyişir

#### 5.2 Approval Axını
```
Lead → Notification (pending) → Telegram admin request → 
Admin approval → WhatsApp queue → Message sent
```

#### 5.3 Telegram Bot Konfiqurasiyası
```json
{
  "Telegram": {
    "BotToken": "8399345423:AAF9cf9mvp4il39G4N8_vQu6Xu-5cxkgKDM",
    "AdminId": 1762884854
  }
}
```

## Əsas Servis Metodu - ProcessRenewalResult (Enhanced)

```csharp
public async Task ProcessRenewalResult(InsuranceJob completedJob)
{
    var tracking = await _context.InsuranceRenewalTracking
        .Include(t => t.User)
        .FirstOrDefaultAsync(t => t.Id == completedJob.InsuranceRenewalTrackingId);
    
    if (tracking == null) return;
    
    switch (tracking.CurrentPhase)
    {
        case "Initial":
            await ProcessInitialPhaseAsync(tracking, completedJob);
            break;
            
        case "YearSearch":
            await ProcessYearSearchPhaseAsync(tracking, completedJob);
            break;
            
        case "MonthSearch":
            await ProcessMonthSearchPhaseAsync(tracking, completedJob);
            break;
    }
    
    // İzləmə məlumatlarını yenilə
    tracking.LastCheckDate = completedJob.CheckDate;
    tracking.ChecksPerformed++;
    tracking.LastCheckResult = completedJob.ResultText;
    tracking.UpdatedAt = DateTime.Now;
    await _context.SaveChangesAsync();
}

// YENİ: İlkin faza - "No insurance" halını idarə edir
private async Task ProcessInitialPhaseAsync(InsuranceRenewalTracking tracking, InsuranceJob completedJob)
{
    bool hasInsuranceData = !string.IsNullOrWhiteSpace(completedJob.Company) ||
                            !string.IsNullOrWhiteSpace(completedJob.VehicleBrand) ||
                            !string.IsNullOrWhiteSpace(completedJob.VehicleModel);

    if (!hasInsuranceData)
    {
        // Dərhal sığorta tapılmır - Lead yarat və proses bitir
        tracking.CurrentPhase = "Completed";
        
        var lead = new Lead
        {
            UserId = tracking.UserId,
            CarNumber = tracking.User.CarNumber,
            LeadType = "NoInsuranceImmediate",
            Notes = "İlk yoxlamada sığorta məlumatları tapılmadı"
        };
        
        _context.Leads.Add(lead);
        await _context.SaveChangesAsync();
        
        // Notification yaradılması (LeadService vasitəsilə)
        await _leadService.CreateNotificationForLeadAsync(lead);
    }
    else
    {
        // Normal axın - YearSearch fazasına keç
        tracking.CurrentPhase = "YearSearch";
        tracking.NextCheckDate = completedJob.CheckDate?.AddYears(-1);
        await CreateNextJobAsync(tracking, tracking.User.CarNumber);
    }
}

// Enhanced MonthSearch - company-based strategy əlavə edilib
private async Task ProcessMonthSearchPhaseAsync(InsuranceRenewalTracking tracking, InsuranceJob completedJob)
{
    var allJobs = await GetAllRelatedJobsAsync(tracking.Id);
    
    // Strategy 1: Classic VAR/YOX search
    var oppositeJob = FindOppositeInsuranceStatusJob(allJobs, completedJob);
    
    if (oppositeJob == null)
    {
        // Strategy 2: Company-based search
        oppositeJob = FindDifferentCompanyJob(allJobs, completedJob);
    }
    
    if (oppositeJob != null)
    {
        var dateDiff = Math.Abs((oppositeJob.CheckDate - completedJob.CheckDate).Value.TotalDays);
        
        if (dateDiff <= 14) // 31-dən 14-ə dəyişib
        {
            // FinalCheck fazasına keç
            tracking.CurrentPhase = "FinalCheck";
            await UpdateUserWithEstimatedDateAsync(tracking.UserId, completedJob, oppositeJob);
            tracking.CurrentPhase = "Completed";
        }
        else
        {
            // Binary search davam edir
            var nextDate = CalculateNextSearchDateAsync(completedJob, oppositeJob, allJobs);
            tracking.NextCheckDate = nextDate;
            await CreateNextJobAsync(tracking, tracking.User.CarNumber);
        }
    }
}

// Enhanced User update - renewal window əlavə edilib
private async Task UpdateUserWithEstimatedDateAsync(int userId, InsuranceJob earlierJob, InsuranceJob laterJob)
{
    var user = await _context.Users.FindAsync(userId);
    if (user == null) return;

    // Orta nöqtəni hesabla
    var middleDate = earlierJob.CheckDate.Value.AddDays(
        (laterJob.CheckDate.Value - earlierJob.CheckDate.Value).TotalDays / 2);

    user.EstimatedRenewalDay = middleDate.Day;
    user.EstimatedRenewalMonth = middleDate.Month;
    user.LastConfirmedRenewalDate = middleDate;
    
    // YENİ: Renewal window
    user.RenewalWindowStart = earlierJob.CheckDate;
    user.RenewalWindowEnd = laterJob.CheckDate;
    user.UpdatedAt = DateTime.Now;

    await _context.SaveChangesAsync();
    
    // YENİ: RenewalWindow lead yarat
    var lead = new Lead
    {
        UserId = userId,
        CarNumber = user.CarNumber,
        LeadType = "RenewalWindow",
        Notes = $"Yenilənmə tarixi: {middleDate:dd/MM/yyyy}, Interval: {user.RenewalWindowStart:dd/MM} - {user.RenewalWindowEnd:dd/MM}"
    };
    
    _context.Leads.Add(lead);
    await _context.SaveChangesAsync();
    
    // Notification approval prosesi
    await _leadService.CreateNotificationForLeadAsync(lead);
}
```

## YENİ: Lead & Notification Services

```csharp
// LeadService.cs
public class LeadService
{
    private readonly ApplicationDbContext _context;
    private readonly TelegramBotService _telegramBot;
    
    public async Task CreateNotificationForLeadAsync(Lead lead)
    {
        var user = await _context.Users.FindAsync(lead.UserId);
        var message = GenerateMessageForLead(lead, user);
        
        var notification = new Notification
        {
            LeadId = lead.Id,
            Channel = "wa",
            Message = message,
            Status = "pending"
        };
        
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        
        // Telegram bot ilə admin-ə göndər
        await _telegramBot.SendApprovalRequestAsync(notification);
    }
    
    private string GenerateMessageForLead(Lead lead, User user)
    {
        return lead.LeadType switch
        {
            "NoInsuranceImmediate" => $"🚨 {lead.CarNumber} - SIGORTA YOXDUR!\n" +
                                      $"Dərhal müştəriyə təklif göndərmək üçün əlaqə saxlayın.\n" +
                                      $"📞 {user.PhoneNumber ?? "Telefon yoxdur"}",
                                      
            "RenewalWindow" => $"📅 {lead.CarNumber} - Yenilənmə tarixi yaxınlaşır!\n" +
                               $"Təxmini tarix: {user.EstimatedRenewalDay}/{user.EstimatedRenewalMonth}\n" +
                               $"📞 {user.PhoneNumber ?? "Telefon yoxdur"}",
                               
            _ => $"🔄 {lead.CarNumber} - Yeni lead: {lead.LeadType}\n" +
                 $"📞 {user.PhoneNumber ?? "Telefon yoxdur"}"
        };
    }
}

// TelegramBotService.cs  
public class TelegramBotService
{
    private readonly ITelegramBotClient _botClient;
    private readonly long _adminChatId;
    
    public async Task SendApprovalRequestAsync(Notification notification)
    {
        var lead = await _context.Leads.Include(l => l.User)
                                       .FirstAsync(l => l.Id == notification.LeadId);
        
        var text = $"🚗 **{lead.CarNumber}** ({lead.User.PhoneNumber ?? "N/A"})\n" +
                   $"📋 Lead tip: **{lead.LeadType}**\n\n" +
                   $"📱 Göndəriləcək WhatsApp mesajı:\n" +
                   $"```\n{notification.Message}\n```";

        var keyboard = new InlineKeyboardMarkup(
            InlineKeyboardButton.WithCallbackData("✅ TƏSDİQLƏ", $"approve:{notification.Id}")
        );

        await _botClient.SendTextMessageAsync(_adminChatId, text, 
                                             parseMode: ParseMode.Markdown,
                                             replyMarkup: keyboard);
    }
}
```

## FinalCheck Fazasının İşlənməsi (Enhanced)

```csharp
// FinalCheck fazasındakı qeydləri emal edən metod (artıq avtomatik olaraq Lead yaradır)
public async Task ProcessFinalCheckTrackingsAsync()
{
    var finalCheckTrackings = await _context.InsuranceRenewalTracking
        .Where(t => t.CurrentPhase == "FinalCheck")
        .Include(t => t.User)
        .ToListAsync();
    
    foreach (var tracking in finalCheckTrackings)
    {
        // UpdateUserWithEstimatedDateAsync-də artıq Lead yaradılır
        // və notification approval prosesi başlayır
        
        // Prosesi tamamlandı kimi qeyd et
        tracking.CurrentPhase = "Completed";
        tracking.LastCheckResult = $"Prosess tamamlandı. Lead yaradıldı: RenewalWindow";
        tracking.UpdatedAt = DateTime.Now;
    }
    
    await _context.SaveChangesAsync();
}

// Hangfire job konfiqurasiyası
RecurringJob.AddOrUpdate<RenewalTrackingService>(
    "process-finalcheck-trackings",
    service => service.ProcessFinalCheckTrackingsAsync(),
    Cron.Daily(8)
);
```

## Gündəlik Limit İdarə Edilməsi

ISB.az saytı hər avtomobil üçün gündə 3 sorğu limitini tətbiq edir:

```csharp
// InsuranceService-də limit yoxlaması
if (result.ErrorMessage == "DailyLimitExceeded")
{
    var queue = await _queueRepository.GetQueueAsync(job.QueueId);
    queue.Status = "scheduled";
    queue.ProcessAfter = DateTime.Today.AddDays(1).AddHours(8);
    await _queueRepository.UpdateQueueAsync(queue);
}
```

## API və Konfiqurasiya (Enhanced)

```csharp
// Controller (əgər lazım olarsa)
[ApiController]
[Route("api/[controller]")]
public class RenewalController : ControllerBase
{
    [HttpPost("track")]
    public async Task<IActionResult> StartTracking(string carNumber)
    {
        await _renewalService.StartRenewalTracking(carNumber);
        return Ok(new { message = "Renewal tracking started", carNumber });
    }
    
    // YENİ: Lead endpoints
    [HttpGet("leads")]
    public async Task<IActionResult> GetLeads([FromQuery] string? leadType = null)
    {
        var leads = await _leadService.GetLeadsAsync(leadType);
        return Ok(leads);
    }
    
    [HttpPost("leads/{id}/convert")]
    public async Task<IActionResult> ConvertLead(int id)
    {
        await _leadService.ConvertLeadAsync(id);
        return Ok(new { message = "Lead converted successfully" });
    }
}

// Program.cs-də servis qeydiyyatı (enhanced)
builder.Services.AddScoped<RenewalTrackingService>();
builder.Services.AddScoped<LeadService>();
builder.Services.AddScoped<TelegramBotService>();
builder.Services.AddScoped<NotificationService>();

// Telegram bot hosting
builder.Services.AddHostedService<TelegramBotHostedService>();

// Telegram bot client
builder.Services.AddSingleton<ITelegramBotClient>(provider =>
{
    var config = provider.GetService<IConfiguration>();
    var token = config["Telegram:BotToken"];
    return new TelegramBotClient(token);
});

RecurringJob.AddOrUpdate<InsuranceJob>(
    "process-insurance-jobs", 
    job => job.ProcessPendingInsuranceJobsAsync(), 
    Cron.MinuteInterval(1)
);
```

## Nümunə: 10RL033 Avtomobili Prosesi (Enhanced)

### 1. İlkin Mərhələ
```sql
-- User yaradılması
INSERT INTO Users (CarNumber, CreatedAt) VALUES ('10RL033', '2025-07-26 14:30:00')

-- İzləmə qeydi yaradılması  
INSERT INTO InsuranceRenewalTracking (UserId, CurrentPhase, NextCheckDate, CreatedAt) 
VALUES (1, 'Initial', '2025-07-26 14:30:00', '2025-07-26 14:30:00')

-- İlk iş yaradılması
INSERT INTO InsuranceJobs (QueueId, CarNumber, CheckDate, InsuranceRenewalTrackingId) 
VALUES (101, '10RL033', '2025-07-26', 1)
```

### 2. YearSearch Fazası
- 2025-07-26: Sığorta var (AtaSığorta)  
- 2024-07-26: Sığorta var (AtaSığorta)
- 2023-07-26: Sığorta yoxdur → Dəyişiklik tapıldı

### 3. MonthSearch Fazası (Enhanced Binary Search)
- 2024-01-26: Sığorta var (AzSığorta) → 2024-07-26 ilə company fərqli
- 2023-10-26: Sığorta var (Paşa Sığorta) → 2024-01-26 ilə company fərqli
- 2023-12-26: İnterval 14 gündən azdır → FinalCheck fazasına keç

### 4. FinalCheck Fazası (Enhanced)
```sql
-- İstifadəçi məlumatlarının yenilənməsi (Enhanced)
UPDATE Users SET 
    EstimatedRenewalDay = 5, 
    EstimatedRenewalMonth = 1,
    LastConfirmedRenewalDate = '2024-01-05',
    RenewalWindowStart = '2023-12-26',     -- YENİ
    RenewalWindowEnd = '2024-01-05',       -- YENİ
    UpdatedAt = '2025-07-26 15:00:00'
WHERE Id = 1

-- Lead yaradılması (YENİ)
INSERT INTO Leads (UserId, CarNumber, LeadType, Notes, CreatedAt)
VALUES (1, '10RL033', 'RenewalWindow', 'Yenilənmə tarixi: 05/01/2024, Interval: 26/12 - 05/01', '2025-07-26 15:00:00')

-- Notification yaradılması (YENİ)
INSERT INTO Notifications (LeadId, Channel, Message, Status, CreatedAt)
VALUES (1, 'wa', '📅 10RL033 - Yenilənmə tarixi yaxınlaşır!...', 'pending', '2025-07-26 15:00:00')

-- İzləmə prosesinin tamamlanması  
UPDATE InsuranceRenewalTracking SET 
    CurrentPhase = 'Completed',
    LastCheckResult = 'Prosess tamamlandı. Lead yaradıldı: RenewalWindow'
WHERE Id = 1
```

### 5. Approval Prosesi (YENİ)
1. **Telegram bot admin-ə mesaj göndərir** approval düyməsi ilə
2. **Admin "✅ TƏSDİQLƏ" basır**
3. **Notification status "approved"-ə keçir**
4. **WhatsApp queue-ya əlavə edilir**
5. **WhatsApp mesajı göndərilir, status "sent"-ə keçir**

## Test Case-lər üçün NV Nömrələri (Enhanced)

### 🔸 TEST CASE 1: İlkin məlumat tapılmır
**Məqsəd:** `NoInsuranceImmediate` lead yaratma test etmək  
**Gözlənilən nəticə:** Lead yaranması və Telegram approval  
**Test NV-lər:**
- `90ZZ999` - məlumat vermədiyinə eminlik
- `TEST001` - test üçün dummy NV

### 🔸 TEST CASE 2: Dəyişiklik olan (Company Change)
**Məqsəd:** `MonthSearch` fazasının company-based strategy testə  
**Gözlənilən nəticə:** `RenewalWindow` lead yaranması  
**Test NV-lər:**
- `99JF842` - ✅ **TESTED**: 2025 = ATEŞGAH, 2024 = AZƏRBAYCAN SƏNAYE (company change)
- `77JD145` - adətən şirkət dəyişikliyi var

### 🔸 TEST CASE 3: Dəyişiklik olmayan
**Məqsəd:** `YearSearch` fazasının çox il geri gedə bilməsini test etmək  
**Gözlənilən nəticə:** Sistemin `YearSearch` fazasında qalması  
**Test NV-lər:**
- `99JP083` - adətən ATEŞGAH SIGORTA verir (dəyişiklik yox)
- `77JG472` - eyni şirkət, uzun müddət
- `90AM566` - ✅ **TESTED**: 2025/2024/2023 = ATEŞGAH/İSUZU, 2022 = pending limit
- `90HB987` - ✅ **TESTED**: 2025/2024/2023 = ATEŞGAH, 2022 = NULL (pending limit)
- `77JG327` - ✅ **TESTED**: 2025/2024/2023 = ATEŞGAH/İSUZU, 2022 = NULL (pending limit)
- `10RL033` - adətən məlumat vermədiyi üçün dəyişiklik yoxdur

### 🔸 TEST CASE 4: Gündəlik limit
**Məqsəd:** ISB.az gündəlik limitə çatdıqda növbəti gün təxiri test etmək  
**Gözlənilən nəticə:** `ProcessAfter` sahəsinin təyin edilməsi  
**Test NV-lər:**
- `77RQ865` - limit testləri üçün

### 🔸 ANOMALY/NAMƏLUM CASE-LƏR
**Məqsəd:** ISB.az-dan gələn qeyri-standart cavabları sənədləşdirmək  
**Test NV-lər:**
- `99JF842` - bəzən qeyri-davamlı məlumat verir
- `77JD145` - anomal davranış göstərir

## Test Etmək üçün

### 1. Tək NV test:
```sql
-- setup_single_test.sql istifadə et
DECLARE @CarNumber NVARCHAR(20) = '99JF842';  -- istədiyiniz NV
-- Script qalanını avtomatik edəcək
```

### 2. Bulk test (15 NV):
```sql
-- setup_bulk_test.sql istifadə et
-- Avtomatik 15 müxtəlif NV test etəcək
```

### 3. Lead & Notification monitoring:
```sql
-- Pending approval-ları yoxla
SELECT l.CarNumber, l.LeadType, n.Status, n.CreatedAt 
FROM Leads l 
JOIN Notifications n ON l.Id = n.LeadId 
WHERE n.Status = 'pending' 
ORDER BY n.CreatedAt DESC;

-- Approval edilmiş amma göndərilməyən mesajlar
SELECT l.CarNumber, n.Status, n.ApprovedAt, n.SentAt
FROM Leads l 
JOIN Notifications n ON l.Id = n.LeadId 
WHERE n.Status = 'approved' AND n.SentAt IS NULL;
```

## Xülasə (Enhanced)

Bu sistem artıq 5 tamamlanmış mərhələdə işləyir:
1. **Initial**: Cari tarixlə sığorta vəziyyəti yoxlanır + No Insurance lead
2. **YearSearch**: İllik intervallarla geriyə gedərək dəyişiklik axtarılır
3. **MonthSearch**: Enhanced ikili axtarış (VAR/YOX + Company-based strategy)
4. **FinalCheck**: Təxmini tarix + renewal window hesablanır + Lead yaradılır
5. **Notification System**: Telegram admin approval + WhatsApp göndərmə

Hər mərhələdə ISB.az-ın gündəlik limitlərinə hörmət edilir və sistem avtomatik olaraq növbəti günə təxirə salır. **YENİ**: Lead management və Telegram approval sistemi tam işlək vəziyyətdədir.
