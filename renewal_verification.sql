-- =====================================================
-- Sığorta Yenilənmə Tarixi İzləmə Sistemi Yoxlama Skripti
-- =====================================================

-- 1. İstifadəçilər və izləmə proseslərinin ümumi vəziyyəti
PRINT '=== 1. İSTİFADƏÇİLƏR VƏ İZLƏMƏ PROSESLƏRİ ==='
SELECT 
    u.Id as UserId,
    u.CarNumber,
    u.PhoneNumber,
    u.EstimatedRenewalDay,
    u.EstimatedRenewalMonth,
    u.LastConfirmedRenewalDate,
    u.CreatedAt as UserCreatedAt,
    t.Id as TrackingId,
    t.CurrentPhase,
    t.ChecksPerformed,
    t.LastCheckDate,
    t.NextCheckDate,
    t.LastCheckResult,
    t.CreatedAt as TrackingCreatedAt
FROM Users u
LEFT JOIN InsuranceRenewalTracking t ON u.Id = t.UserId
ORDER BY u.CreatedAt DESC;

-- 2. Aktiv izləmə prosesləri (Completed olmayan)
PRINT '=== 2. AKTİV İZLƏMƏ PROSESLƏRİ ==='
SELECT 
    u.CarNumber,
    t.CurrentPhase,
    t.ChecksPerformed,
    t.LastCheckDate,
    t.NextCheckDate,
    t.LastCheckResult,
    DATEDIFF(MINUTE, t.CreatedAt, GETDATE()) as MinutesSinceCreated
FROM Users u
JOIN InsuranceRenewalTracking t ON u.Id = t.UserId
WHERE t.CurrentPhase != 'Completed'
ORDER BY t.CreatedAt DESC;

-- 3. Hər faza üçün işlərin sayı
PRINT '=== 3. FAZA ÜZRƏ İŞLƏRİN SAYI ==='
SELECT 
    t.CurrentPhase,
    COUNT(*) as JobCount,
    COUNT(CASE WHEN j.Status = 'completed' THEN 1 END) as CompletedJobs,
    COUNT(CASE WHEN j.Status = 'pending' THEN 1 END) as PendingJobs,
    COUNT(CASE WHEN j.Status = 'failed' THEN 1 END) as FailedJobs
FROM InsuranceRenewalTracking t
LEFT JOIN InsuranceJobs j ON t.Id = j.InsuranceRenewalTrackingId
GROUP BY t.CurrentPhase
ORDER BY 
    CASE t.CurrentPhase 
        WHEN 'Initial' THEN 1
        WHEN 'YearSearch' THEN 2
        WHEN 'MonthSearch' THEN 3
        WHEN 'FinalCheck' THEN 4
        WHEN 'Completed' THEN 5
    END;

-- 4. Son 24 saatda yaradılan işlər
PRINT '=== 4. SON 24 SAATDA YARADILAN İŞLƏR ==='
SELECT 
    j.Id as JobId,
    u.CarNumber,
    t.CurrentPhase,
    j.CheckDate,
    j.Status,
    j.Company,
    j.VehicleBrand,
    j.VehicleModel,
    j.CreatedAt,
    DATEDIFF(MINUTE, j.CreatedAt, GETDATE()) as MinutesAgo
FROM InsuranceJobs j
JOIN InsuranceRenewalTracking t ON j.InsuranceRenewalTrackingId = t.Id
JOIN Users u ON t.UserId = u.Id
WHERE j.CreatedAt >= DATEADD(HOUR, -24, GETDATE())
ORDER BY j.CreatedAt DESC;

-- 5. Queue vəziyyəti
PRINT '=== 5. QUEUE VƏZİYYƏTİ ==='
SELECT 
    q.Id as QueueId,
    q.Type,
    q.Status,
    q.Priority,
    q.ProcessAfter,
    q.CreatedAt,
    q.StartedAt,
    q.CompletedAt,
    q.ErrorMessage,
    q.RetryCount,
    DATEDIFF(MINUTE, q.CreatedAt, GETDATE()) as MinutesSinceCreated
FROM Queues q
WHERE q.CreatedAt >= DATEADD(HOUR, -24, GETDATE())
ORDER BY q.CreatedAt DESC;

-- 6. Tamamlanmış izləmə prosesləri
PRINT '=== 6. TAMAMLANMIŞ İZLƏMƏ PROSESLƏRİ ==='
SELECT 
    u.CarNumber,
    u.EstimatedRenewalDay,
    u.EstimatedRenewalMonth,
    u.LastConfirmedRenewalDate,
    t.ChecksPerformed,
    t.LastCheckResult,
    t.CreatedAt as ProcessStarted,
    t.UpdatedAt as ProcessCompleted,
    DATEDIFF(MINUTE, t.CreatedAt, t.UpdatedAt) as ProcessDurationMinutes
FROM Users u
JOIN InsuranceRenewalTracking t ON u.Id = t.UserId
WHERE t.CurrentPhase = 'Completed'
ORDER BY t.UpdatedAt DESC;

-- 7. Xəta verən işlər
PRINT '=== 7. XƏTA VERƏN İŞLƏR ==='
SELECT 
    j.Id as JobId,
    u.CarNumber,
    t.CurrentPhase,
    j.CheckDate,
    j.Status,
    j.CreatedAt,
    q.ErrorMessage
FROM InsuranceJobs j
JOIN InsuranceRenewalTracking t ON j.InsuranceRenewalTrackingId = t.Id
JOIN Users u ON t.UserId = u.Id
JOIN Queues q ON j.QueueId = q.Id
WHERE j.Status = 'failed' OR q.Status = 'failed'
ORDER BY j.CreatedAt DESC;

-- 8. ProcessAfter təxirə salınmış işlər
PRINT '=== 8. PROCESS AFTER TƏXİRƏ SALINMIŞ İŞLƏR ==='
SELECT 
    q.Id as QueueId,
    q.Type,
    q.Status,
    q.ProcessAfter,
    q.CreatedAt,
    DATEDIFF(MINUTE, GETDATE(), q.ProcessAfter) as MinutesUntilProcess,
    CASE 
        WHEN q.ProcessAfter > GETDATE() THEN 'Gələcəkdə'
        WHEN q.ProcessAfter <= GETDATE() THEN 'Gecikmiş'
    END as ProcessStatus
FROM Queues q
WHERE q.ProcessAfter IS NOT NULL
ORDER BY q.ProcessAfter;

-- 9. İkili axtarış alqoritmi yoxlaması (MonthSearch fazası)
PRINT '=== 9. İKİLİ AXTARIŞ ALQORİTMİ YOXLAMASI ==='
SELECT 
    u.CarNumber,
    t.CurrentPhase,
    t.ChecksPerformed,
    j1.CheckDate as FirstCheck,
    j2.CheckDate as SecondCheck,
    j1.Company as FirstCompany,
    j2.Company as SecondCompany,
    DATEDIFF(DAY, j1.CheckDate, j2.CheckDate) as DaysBetweenChecks
FROM InsuranceRenewalTracking t
JOIN Users u ON t.UserId = u.Id
JOIN InsuranceJobs j1 ON t.Id = j1.InsuranceRenewalTrackingId AND j1.Status = 'completed'
JOIN InsuranceJobs j2 ON t.Id = j2.InsuranceRenewalTrackingId AND j2.Status = 'completed'
WHERE t.CurrentPhase = 'MonthSearch'
    AND j1.CheckDate < j2.CheckDate
ORDER BY t.CreatedAt DESC;

-- 10. Sistem statistikası
PRINT '=== 10. SİSTEM STATİSTİKASI ==='
SELECT 
    'Users' as TableName,
    COUNT(*) as RecordCount
FROM Users
UNION ALL
SELECT 
    'InsuranceRenewalTracking' as TableName,
    COUNT(*) as RecordCount
FROM InsuranceRenewalTracking
UNION ALL
SELECT 
    'InsuranceJobs' as TableName,
    COUNT(*) as RecordCount
FROM InsuranceJobs
UNION ALL
SELECT 
    'Queues' as TableName,
    COUNT(*) as RecordCount
FROM Queues
UNION ALL
SELECT 
    'Active Trackings' as TableName,
    COUNT(*) as RecordCount
FROM InsuranceRenewalTracking
WHERE CurrentPhase != 'Completed';

-- 11. Son 1 saatda yaradılan yeni izləmə prosesləri
PRINT '=== 11. SON 1 SAATDA YARADILAN YENİ İZLƏMƏ PROSESLƏRİ ==='
SELECT 
    u.CarNumber,
    t.CurrentPhase,
    t.ChecksPerformed,
    t.CreatedAt,
    DATEDIFF(MINUTE, t.CreatedAt, GETDATE()) as MinutesAgo
FROM Users u
JOIN InsuranceRenewalTracking t ON u.Id = t.UserId
WHERE t.CreatedAt >= DATEADD(HOUR, -1, GETDATE())
ORDER BY t.CreatedAt DESC; 