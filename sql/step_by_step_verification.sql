-- =====================================================
-- Sığorta Yenilənmə Tarixi İzləmə Sistemi - Addım-Addım Yoxlama
-- =====================================================

PRINT '=== SİSTEMİN ADDIM-ADDIM YOXLANMASI ==='

-- =====================================================
-- ADDIM 1: İlkin Vəziyyət Yoxlaması
-- =====================================================
PRINT '=== ADDIM 1: İLKİN VƏZİYYƏT YOXLAMASI ==='

-- 1.1 İstifadəçilərin mövcudluğu
SELECT '1.1 - İstifadəçilər' as CheckPoint, COUNT(*) as Count FROM Users;

-- 1.2 Aktiv izləmə prosesləri
SELECT '1.2 - Aktiv İzləmə Prosesləri' as CheckPoint, COUNT(*) as Count 
FROM InsuranceRenewalTracking 
WHERE CurrentPhase != 'Completed';

-- 1.3 Initial fazada olan proseslər
SELECT '1.3 - Initial Fazada' as CheckPoint, COUNT(*) as Count 
FROM InsuranceRenewalTracking 
WHERE CurrentPhase = 'Initial';

-- 1.4 Pending job-lar
SELECT '1.4 - Pending Job-lar' as CheckPoint, COUNT(*) as Count 
FROM InsuranceJobs 
WHERE Status = 'pending';

-- =====================================================
-- ADDIM 2: İlkin Faza Prosesi (Initial → YearSearch)
-- =====================================================
PRINT '=== ADDIM 2: İLKİN FAZA PROSESİ ==='

-- 2.1 Initial fazada olan avtomobillər
SELECT 
    u.CarNumber,
    t.CurrentPhase,
    t.NextCheckDate,
    j.Status as JobStatus,
    j.CheckDate,
    q.Status as QueueStatus
FROM Users u
JOIN InsuranceRenewalTracking t ON u.Id = t.UserId
LEFT JOIN InsuranceJobs j ON t.Id = j.InsuranceRenewalTrackingId
LEFT JOIN Queues q ON j.QueueId = q.Id
WHERE t.CurrentPhase = 'Initial';

-- 2.2 Gözlənilən proses:
-- - Initial fazada olan job-lar emal olunmalı
-- - Job tamamlandıqda CurrentPhase "YearSearch"-ə keçmeli
-- - NextCheckDate 1 il əvvələ təyin olunmalı (CheckDate - 1 year)

-- =====================================================
-- ADDIM 3: İl Axtarışı Fazası (YearSearch)
-- =====================================================
PRINT '=== ADDIM 3: İL AXTARIŞI FAZASI ==='

-- 3.1 YearSearch fazasında olan proseslər
SELECT 
    u.CarNumber,
    t.CurrentPhase,
    t.ChecksPerformed,
    t.LastCheckDate,
    t.NextCheckDate,
    j.Status as LastJobStatus,
    j.Company as LastCompany
FROM Users u
JOIN InsuranceRenewalTracking t ON u.Id = t.UserId
LEFT JOIN InsuranceJobs j ON t.Id = j.InsuranceRenewalTrackingId AND j.Status = 'completed'
WHERE t.CurrentPhase = 'YearSearch'
ORDER BY t.CreatedAt DESC;

-- 3.2 Gözlənilən proses:
-- - YearSearch fazasında job-lar ardıcıl olaraq 1 il əvvələ yoxlanmalı
-- - Sığorta vəziyyətində dəyişiklik tapıldıqda MonthSearch fazasına keçmeli
-- - Dəyişiklik tapılmayanda daha əvvələ getməli

-- =====================================================
-- ADDIM 4: Ay Axtarışı Fazası (MonthSearch)
-- =====================================================
PRINT '=== ADDIM 4: AY AXTARIŞI FAZASI ==='

-- 4.1 MonthSearch fazasında olan proseslər
SELECT 
    u.CarNumber,
    t.CurrentPhase,
    t.ChecksPerformed,
    t.LastCheckDate,
    t.NextCheckDate,
    j.Status as LastJobStatus,
    j.Company as LastCompany
FROM Users u
JOIN InsuranceRenewalTracking t ON u.Id = t.UserId
LEFT JOIN InsuranceJobs j ON t.Id = j.InsuranceRenewalTrackingId AND j.Status = 'completed'
WHERE t.CurrentPhase = 'MonthSearch'
ORDER BY t.CreatedAt DESC;

-- 4.2 İkili axtarış alqoritmi yoxlaması
SELECT 
    u.CarNumber,
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

-- 4.3 Gözlənilən proses:
-- - İkili axtarış alqoritmi ilə tarix intervalı daraldılmalı
-- - 1 ay dəqiqliyinə çatdıqda FinalCheck fazasına keçmeli

-- =====================================================
-- ADDIM 5: Final Yoxlama Fazası (FinalCheck)
-- =====================================================
PRINT '=== ADDIM 5: FİNAL YOXLAMA FAZASI ==='

-- 5.1 FinalCheck fazasında olan proseslər
SELECT 
    u.CarNumber,
    u.EstimatedRenewalDay,
    u.EstimatedRenewalMonth,
    t.CurrentPhase,
    t.ChecksPerformed,
    t.LastCheckResult
FROM Users u
JOIN InsuranceRenewalTracking t ON u.Id = t.UserId
WHERE t.CurrentPhase = 'FinalCheck'
ORDER BY t.CreatedAt DESC;

-- 5.2 Gözlənilən proses:
-- - Təxmini yenilənmə tarixi hesablanmalı
-- - İstifadəçi məlumatları yenilənmeli (EstimatedRenewalDay, EstimatedRenewalMonth)
-- - Status "Completed"-ə keçmeli

-- =====================================================
-- ADDIM 6: Tamamlanmış Proseslər (Completed)
-- =====================================================
PRINT '=== ADDIM 6: TAMAMLANMIŞ PROSESLƏR ==='

-- 6.1 Tamamlanmış proseslər
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

-- =====================================================
-- ADDIM 7: Xəta Vəziyyətləri
-- =====================================================
PRINT '=== ADDIM 7: XƏTA VƏZİYYƏTLƏRİ ==='

-- 7.1 Uğursuz job-lar
SELECT 
    u.CarNumber,
    t.CurrentPhase,
    j.Status as JobStatus,
    j.CheckDate,
    q.ErrorMessage,
    j.CreatedAt
FROM Users u
JOIN InsuranceRenewalTracking t ON u.Id = t.UserId
JOIN InsuranceJobs j ON t.Id = j.InsuranceRenewalTrackingId
JOIN Queues q ON j.QueueId = q.Id
WHERE j.Status = 'failed' OR q.Status = 'failed'
ORDER BY j.CreatedAt DESC;

-- 7.2 ProcessAfter təxirə salınmış işlər
SELECT 
    u.CarNumber,
    t.CurrentPhase,
    q.ProcessAfter,
    DATEDIFF(MINUTE, GETDATE(), q.ProcessAfter) as MinutesUntilProcess
FROM Users u
JOIN InsuranceRenewalTracking t ON u.Id = t.UserId
JOIN InsuranceJobs j ON t.Id = j.InsuranceRenewalTrackingId
JOIN Queues q ON j.QueueId = q.Id
WHERE q.ProcessAfter IS NOT NULL
ORDER BY q.ProcessAfter;

-- =====================================================
-- ADDIM 8: Sistem Performansı
-- =====================================================
PRINT '=== ADDIM 8: SİSTEM PERFORMANSI ==='

-- 8.1 Son 1 saatda yaradılan işlər
SELECT 
    COUNT(*) as JobsCreatedLastHour,
    COUNT(CASE WHEN Status = 'completed' THEN 1 END) as CompletedJobs,
    COUNT(CASE WHEN Status = 'failed' THEN 1 END) as FailedJobs
FROM InsuranceJobs
WHERE CreatedAt >= DATEADD(HOUR, -1, GETDATE());

-- 8.2 Faza üzrə işlərin paylanması
SELECT 
    t.CurrentPhase,
    COUNT(*) as TotalJobs,
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

-- =====================================================
-- XÜLASƏ
-- =====================================================
PRINT '=== XÜLASƏ ==='
PRINT 'Sistem düzgün işləməlidir:'
PRINT '1. Initial → YearSearch → MonthSearch → FinalCheck → Completed'
PRINT '2. Hər fazada uyğun job-lar yaradılmalı'
PRINT '3. Sığorta vəziyyətində dəyişikliklər aşkarlanmalı'
PRINT '4. İkili axtarış alqoritmi düzgün işləmeli'
PRINT '5. Təxmini yenilənmə tarixi hesablanmalı'
PRINT '6. İstifadəçi məlumatları yenilənmeli' 