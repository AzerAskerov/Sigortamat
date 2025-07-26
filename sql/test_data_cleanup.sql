-- =====================================================
-- Test Məlumatlarının Təmizlənməsi və Yeni Test Məlumatlarının Yaradılması
-- =====================================================

PRINT '=== KÖHNƏ TEST MƏLUMATLARININ TƏMİZLƏNMƏSİ ==='

-- 1. Köhnə test məlumatlarını sil
DELETE FROM InsuranceJobs WHERE InsuranceRenewalTrackingId IS NOT NULL;
DELETE FROM InsuranceRenewalTracking;
DELETE FROM Users;

-- Queue-ları da təmizlə (renewal tracking ilə bağlı olanları)
DELETE FROM Queues WHERE Type = 'insurance' AND Id IN (
    SELECT DISTINCT QueueId FROM InsuranceJobs WHERE InsuranceRenewalTrackingId IS NOT NULL
);

PRINT '✅ Köhnə test məlumatları silindi'

-- =====================================================
-- YENİ TEST MƏLUMATLARININ YARADILMASI
-- =====================================================

PRINT '=== YENİ TEST MƏLUMATLARININ YARADILMASI ==='

-- 1. İstifadəçiləri yarat
INSERT INTO Users (CarNumber, PhoneNumber, NotificationEnabled, CreatedAt) VALUES 
('10RL033', '994707877878', 1, GETDATE()),
('90HB986', '994501234567', 1, GETDATE()),
('10RL035', '994709876543', 1, GETDATE());

PRINT '✅ 3 yeni istifadəçi yaradıldı'

-- 2. İzləmə proseslərini yarat
INSERT INTO InsuranceRenewalTracking (UserId, CurrentPhase, NextCheckDate, ChecksPerformed, CreatedAt) 
SELECT 
    u.Id,
    'Initial',
    GETDATE(),
    0,
    GETDATE()
FROM Users u
WHERE u.CarNumber IN ('10RL033', '90HB986', '10RL035');

PRINT '✅ 3 yeni izləmə prosesi yaradıldı'

-- 3. İlk insurance job-ları yarat
INSERT INTO Queues (Type, Status, Priority, CreatedAt)
SELECT 'insurance', 'pending', 1, GETDATE()
FROM InsuranceRenewalTracking t
JOIN Users u ON t.UserId = u.Id
WHERE t.CurrentPhase = 'Initial';

-- Job-ları yarat
INSERT INTO InsuranceJobs (QueueId, CarNumber, CheckDate, InsuranceRenewalTrackingId, Status, CreatedAt)
SELECT 
    q.Id,
    u.CarNumber,
    GETDATE(),
    t.Id,
    'pending',
    GETDATE()
FROM InsuranceRenewalTracking t
JOIN Users u ON t.UserId = u.Id
JOIN Queues q ON q.Type = 'insurance' AND q.Status = 'pending'
WHERE t.CurrentPhase = 'Initial'
    AND q.CreatedAt >= DATEADD(MINUTE, -1, GETDATE());

PRINT '✅ İlk insurance job-ları yaradıldı'

-- =====================================================
-- YARADILAN MƏLUMATLARIN YOXLANMASI
-- =====================================================

PRINT '=== YARADILAN MƏLUMATLARIN YOXLANMASI ==='

-- İstifadəçiləri yoxla
SELECT 'Users' as TableName, COUNT(*) as RecordCount FROM Users
UNION ALL
SELECT 'InsuranceRenewalTracking' as TableName, COUNT(*) as RecordCount FROM InsuranceRenewalTracking
UNION ALL
SELECT 'InsuranceJobs' as TableName, COUNT(*) as RecordCount FROM InsuranceJobs
UNION ALL
SELECT 'Queues' as TableName, COUNT(*) as RecordCount FROM Queues;

-- Detallı məlumatları göstər
SELECT 
    u.CarNumber,
    u.PhoneNumber,
    t.CurrentPhase,
    t.NextCheckDate,
    j.Status as JobStatus,
    q.Status as QueueStatus
FROM Users u
JOIN InsuranceRenewalTracking t ON u.Id = t.UserId
LEFT JOIN InsuranceJobs j ON t.Id = j.InsuranceRenewalTrackingId
LEFT JOIN Queues q ON j.QueueId = q.Id
ORDER BY u.CarNumber;

PRINT '✅ Test məlumatları uğurla yaradıldı və yoxlandı'
PRINT '🚗 Test avtomobilləri: 10RL033, 90HB986, 10RL035'
PRINT '📱 Test telefon nömrələri: 994707877878, 994501234567, 994709876543' 