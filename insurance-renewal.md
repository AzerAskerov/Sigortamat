# Sığorta Yenilənmə Tarixi İzləmə Sistemi

## Sistemin Əsas Prinsipi

Bu sistem avtomobil sığortasının yenilənmə tarixini avtomatik təyin edir:
- ISB.az saytında müxtəlif tarixlərlə sorğular göndərir
- Sığorta vəziyyətindəki dəyişiklikləri izləyir
- İkili axtarış alqoritmi ilə yenilənmə tarixini təxmin edir
- ISB.az gündəlik 3 sorğu limitinə hörmət edir (`ProcessAfter` sahəsi ilə təxirə salma)

## Verilənlər Bazası Strukturu

### Yeni Cədvəllər
```sql
-- İstifadəçilər və yenilənmə tarixləri
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CarNumber NVARCHAR(20) NOT NULL UNIQUE,
    PhoneNumber NVARCHAR(20) NULL,
    NotificationEnabled BIT NOT NULL DEFAULT 1,
    EstimatedRenewalDay INT NULL,
    EstimatedRenewalMonth INT NULL,
    LastConfirmedRenewalDate DATETIME NULL,
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

## Model Sinifləri

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
```

## Əsas Proses Fazaları

### Step 1: İlkin Yoxlama [TƏSDIQLƏNIB]
1. İstifadəçi yaradılır (`Users` cədvəli)
2. İzləmə qeydi yaradılır (`CurrentPhase = "Initial"`)
3. Cari tarixlə ilk sığorta yoxlaması

### Step 2: İl Axtarışı [TƏSDIQLƏNIB]
1. Əvvəlki illəri ardıcıl yoxlayır
2. Sığorta vəziyyətində dəyişiklik axtarır
3. İl sərhədi tapıldıqda `MonthSearch` fazasına keçir

### Step 3: Ay Axtarışı [TƏSDIQLƏNIB]
1. İkili axtarış alqoritmi istifadə edir
2. Tarix intervalını tədricən daraldır
3. 1 ay dəqiqliyinə çatdıqda `FinalCheck` fazasına keçir

### Step 4: FinalCheck Fazası [TƏSDIQLƏNIB]
1. Təxmini yenilənmə tarixi hesablanır
2. İstifadəçi məlumatları yenilənir (gün/ay)
3. Bildiriş planlaşdırma prosesi başlayır
4. Status `Completed`-ə keçir

### Step 5: Bildiriş Sistemi [TƏSDİQLƏNMƏYİB]
WhatsApp bildirişlərinin planlaşdırılması və göndərilməsi.

## Əsas Servis Metodu - ProcessRenewalResult

```csharp
public async Task ProcessRenewalResult(InsuranceJob completedJob)
{
    var tracking = await _context.InsuranceRenewalTracking
        .FindAsync(completedJob.InsuranceRenewalTrackingId);
    
    if (tracking == null) return;
    
    switch (tracking.CurrentPhase)
    {
        case "Initial":
            // YearSearch fazasına keçid
            tracking.CurrentPhase = "YearSearch";
            tracking.NextCheckDate = completedJob.CheckDate.AddYears(-1);
            await CreateNextJob(tracking, completedJob.CarNumber);
            break;
            
        case "YearSearch":
            var previousJob = await GetPreviousJob(tracking.Id, completedJob.CheckDate);
            bool hasChanges = DetectChanges(previousJob, completedJob);
            
            if (hasChanges) {
                // MonthSearch fazasına keçid
                tracking.CurrentPhase = "MonthSearch";
                tracking.NextCheckDate = completedJob.CheckDate.AddMonths(6);
            } else {
                // Daha əvvələ get
                tracking.NextCheckDate = completedJob.CheckDate.AddYears(-1);
            }
            await CreateNextJob(tracking, completedJob.CarNumber);
            break;
            
        case "MonthSearch":
            // İkili axtarış alqoritmi
            var allJobs = await GetAllRelatedJobs(tracking.Id);
            var laterJobs = allJobs.Where(j => j.CheckDate > completedJob.CheckDate).ToList();
            var nearestLater = laterJobs.OrderBy(j => j.CheckDate).First();
            
            TimeSpan dateDiff = nearestLater.CheckDate - completedJob.CheckDate;
            
            if (dateDiff.TotalDays <= 31) {
                // FinalCheck fazasına keçid
                tracking.CurrentPhase = "FinalCheck";
                await UpdateUserWithEstimatedDate(tracking.UserId, completedJob, nearestLater);
            } else {
                // İkili axtarış davam edir
                DateTime nextDate = CalculateNextSearchDate(completedJob, nearestLater, allJobs);
                tracking.NextCheckDate = nextDate;
                await CreateNextJob(tracking, completedJob.CarNumber);
            }
            break;
    }
    
    // İzləmə məlumatlarını yenilə
    tracking.LastCheckDate = completedJob.CheckDate;
    tracking.ChecksPerformed++;
    tracking.LastCheckResult = completedJob.ResultText;
    tracking.UpdatedAt = DateTime.Now;
    await _context.SaveChangesAsync();
}

// Dəyişiklik aşkarlama metodu
private bool DetectChanges(InsuranceJob job1, InsuranceJob job2)
{
    return string.IsNullOrEmpty(job1.Company) != string.IsNullOrEmpty(job2.Company) ||
           job1.Company != job2.Company ||
           job1.VehicleBrand != job2.VehicleBrand ||
           job1.VehicleModel != job2.VehicleModel;
}
```

## FinalCheck Fazasının İşlənməsi

```csharp
// FinalCheck fazasındakı qeydləri emal edən metod
public async Task ProcessFinalCheckTrackingsAsync()
{
    var finalCheckTrackings = await _context.InsuranceRenewalTracking
        .Where(t => t.CurrentPhase == "FinalCheck")
        .Include(t => t.User)
        .ToListAsync();
    
    foreach (var tracking in finalCheckTrackings)
    {
        // Bildiriş planlaşdırma (Step 5-də implementasiya ediləcək)
        await ScheduleNotificationsAsync(tracking.User);
        
        // Prosesi tamamlandı kimi qeyd et
        tracking.CurrentPhase = "Completed";
        tracking.LastCheckResult = $"Prosess tamamlandı. Təxmini tarix: {tracking.User.EstimatedRenewalDay}/{tracking.User.EstimatedRenewalMonth}";
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

## API və Konfiqurasiya

```csharp
// Controller
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
}

// Program.cs-də servis qeydiyyatı
builder.Services.AddScoped<RenewalTrackingService>();
RecurringJob.AddOrUpdate<InsuranceJob>(
    "process-insurance-jobs", 
    job => job.ProcessPendingInsuranceJobsAsync(), 
    Cron.MinuteInterval(1)
);
```

## Nümunə: 10RL033 Avtomobili Prosesi

### 1. İlkin Mərhələ
```sql
-- User yaradılması
INSERT INTO Users (CarNumber, CreatedAt) VALUES ('10RL033', '2025-07-18 14:30:00')

-- İzləmə qeydi yaradılması  
INSERT INTO InsuranceRenewalTracking (UserId, CurrentPhase, NextCheckDate, CreatedAt) 
VALUES (1, 'Initial', '2025-07-18 14:30:00', '2025-07-18 14:30:00')

-- İlk iş yaradılması
INSERT INTO InsuranceJobs (QueueId, CarNumber, CheckDate, InsuranceRenewalTrackingId) 
VALUES (101, '10RL033', '2025-07-18', 1)
```

### 2. YearSearch Fazası
- 2025-07-18: Sığorta var (AtaSığorta)  
- 2024-07-18: Sığorta var (AtaSığorta)
- 2023-07-18: Sığorta yoxdur → Dəyişiklik tapıldı

### 3. MonthSearch Fazası  
- 2024-01-18: Sığorta var (AzSığorta) → 2024-07-18 ilə eynidir
- 2023-10-18: Sığorta var (Paşa Sığorta) → 2024-01-18 ilə fərqlidir
- 2023-12-18: İnterval 1 aydan azdır → FinalCheck fazasına keç

### 4. FinalCheck Fazası
```sql
-- İstifadəçi məlumatlarının yenilənməsi
UPDATE Users SET 
    EstimatedRenewalDay = 5, 
    EstimatedRenewalMonth = 1,
    LastConfirmedRenewalDate = '2024-01-05',
    UpdatedAt = '2025-07-18 15:00:00'
WHERE Id = 1

-- İzləmə prosesinin tamamlanması  
UPDATE InsuranceRenewalTracking SET 
    CurrentPhase = 'Completed',
    LastCheckResult = 'Prosess tamamlandı. Təxmini tarix: 5/1'
WHERE Id = 1
```

## Xülasə

Bu sistem 4 əsas mərhələdə işləyir:
1. **Initial**: Cari tarixlə sığorta vəziyyəti yoxlanır
2. **YearSearch**: İllik intervallarla geriyə gedərək dəyişiklik axtarılır
3. **MonthSearch**: İkili axtarış ilə ay dəqiqliyinə qədər daraldılır
4. **FinalCheck**: Təxmini tarix hesablanır və bildiriş prosesi başladılır

Hər mərhələdə ISB.az-ın gündəlik limitlərinə hörmət edilir və sistem avtomatik olaraq növbəti günə təxirə salır.
