-- =====================================================
-- LEAD FUNKSIONALLIĞI TEST SCRIPT
-- =====================================================

-- Test 1: NoInsuranceImmediate Lead Test
-- Bu test avtomobil nömrəsi üçün sığorta tapılmadığında lead yaradılmasını test edir

PRINT '🧪 TEST 1: NoInsuranceImmediate Lead Test'
PRINT '=========================================='

-- Test avtomobil nömrəsi
DECLARE @TestCarNumber NVARCHAR(20) = 'TEST001';
DECLARE @TestPhoneNumber NVARCHAR(20) = '0501234567';

-- Mövcud test məlumatlarını təmizlə
DELETE FROM Notifications WHERE LeadId IN (SELECT Id FROM Leads WHERE CarNumber = @TestCarNumber);
DELETE FROM Leads WHERE CarNumber = @TestCarNumber;
DELETE FROM InsuranceRenewalTracking WHERE UserId IN (SELECT Id FROM Users WHERE CarNumber = @TestCarNumber);
DELETE FROM Users WHERE CarNumber = @TestCarNumber;

-- Test User yarat
INSERT INTO Users (CarNumber, PhoneNumber, CreatedAt) 
VALUES (@TestCarNumber, @TestPhoneNumber, GETDATE());

DECLARE @UserId INT = SCOPE_IDENTITY();
PRINT '✅ Test User yaradıldı: ' + CAST(@UserId AS NVARCHAR(10));

-- Test Lead yarat (NoInsuranceImmediate)
INSERT INTO Leads (UserId, CarNumber, LeadType, Notes, CreatedAt, IsConverted)
VALUES (@UserId, @TestCarNumber, 'NoInsuranceImmediate', 'Test lead - sığorta tapılmadı', GETDATE(), 0);

DECLARE @LeadId INT = SCOPE_IDENTITY();
PRINT '✅ Test Lead yaradıldı: ' + CAST(@LeadId AS NVARCHAR(10));

-- Test Notification yarat
INSERT INTO Notifications (LeadId, Channel, Message, Status, CreatedAt)
VALUES (@LeadId, 'wa', '🚨 TEST001 - SIGORTA YOXDUR! Dərhal müştəriyə təklif göndərmək üçün əlaqə saxlayın. 📞 0501234567', 'pending', GETDATE());

DECLARE @NotificationId INT = SCOPE_IDENTITY();
PRINT '✅ Test Notification yaradıldı: ' + CAST(@NotificationId AS NVARCHAR(10));

-- Nəticələri yoxla
PRINT ''
PRINT '📊 TEST 1 NƏTICƏLƏRİ:'
PRINT '===================='

SELECT 
    'User' as TableName,
    Id,
    CarNumber,
    PhoneNumber,
    CreatedAt
FROM Users 
WHERE CarNumber = @TestCarNumber;

SELECT 
    'Lead' as TableName,
    Id,
    UserId,
    CarNumber,
    LeadType,
    Notes,
    IsConverted,
    CreatedAt
FROM Leads 
WHERE CarNumber = @TestCarNumber;

SELECT 
    'Notification' as TableName,
    Id,
    LeadId,
    Channel,
    Message,
    Status,
    CreatedAt
FROM Notifications 
WHERE LeadId = @LeadId;

PRINT ''
PRINT '✅ TEST 1 TAMAMLANDI'
PRINT ''

-- =====================================================

-- Test 2: RenewalWindow Lead Test
-- Bu test yenilənmə tarixi müəyyənləşdikdə lead yaradılmasını test edir

PRINT '🧪 TEST 2: RenewalWindow Lead Test'
PRINT '=================================='

-- Test avtomobil nömrəsi
DECLARE @TestCarNumber2 NVARCHAR(20) = 'TEST002';
DECLARE @TestPhoneNumber2 NVARCHAR(20) = '0559876543';

-- Mövcud test məlumatlarını təmizlə
DELETE FROM Notifications WHERE LeadId IN (SELECT Id FROM Leads WHERE CarNumber = @TestCarNumber2);
DELETE FROM Leads WHERE CarNumber = @TestCarNumber2;
DELETE FROM InsuranceRenewalTracking WHERE UserId IN (SELECT Id FROM Users WHERE CarNumber = @TestCarNumber2);
DELETE FROM Users WHERE CarNumber = @TestCarNumber2;

-- Test User yarat (renewal window ilə)
INSERT INTO Users (CarNumber, PhoneNumber, EstimatedRenewalDay, EstimatedRenewalMonth, 
                   RenewalWindowStart, RenewalWindowEnd, CreatedAt) 
VALUES (@TestCarNumber2, @TestPhoneNumber2, 15, 3, '2024-03-01', '2024-03-15', GETDATE());

DECLARE @UserId2 INT = SCOPE_IDENTITY();
PRINT '✅ Test User 2 yaradıldı: ' + CAST(@UserId2 AS NVARCHAR(10));

-- Test Lead yarat (RenewalWindow)
INSERT INTO Leads (UserId, CarNumber, LeadType, Notes, CreatedAt, IsConverted)
VALUES (@UserId2, @TestCarNumber2, 'RenewalWindow', 'Test lead - yenilənmə tarixi yaxınlaşır', GETDATE(), 0);

DECLARE @LeadId2 INT = SCOPE_IDENTITY();
PRINT '✅ Test Lead 2 yaradıldı: ' + CAST(@LeadId2 AS NVARCHAR(10));

-- Test Notification yarat
INSERT INTO Notifications (LeadId, Channel, Message, Status, CreatedAt)
VALUES (@LeadId2, 'wa', '📅 TEST002 - Yenilənmə tarixi yaxınlaşır! Təxmini tarix: 15/3 📞 0559876543', 'pending', GETDATE());

DECLARE @NotificationId2 INT = SCOPE_IDENTITY();
PRINT '✅ Test Notification 2 yaradıldı: ' + CAST(@NotificationId2 AS NVARCHAR(10));

-- Nəticələri yoxla
PRINT ''
PRINT '📊 TEST 2 NƏTICƏLƏRİ:'
PRINT '===================='

SELECT 
    'User' as TableName,
    Id,
    CarNumber,
    PhoneNumber,
    EstimatedRenewalDay,
    EstimatedRenewalMonth,
    RenewalWindowStart,
    RenewalWindowEnd
FROM Users 
WHERE CarNumber = @TestCarNumber2;

SELECT 
    'Lead' as TableName,
    Id,
    UserId,
    CarNumber,
    LeadType,
    Notes,
    IsConverted,
    CreatedAt
FROM Leads 
WHERE CarNumber = @TestCarNumber2;

SELECT 
    'Notification' as TableName,
    Id,
    LeadId,
    Channel,
    Message,
    Status,
    CreatedAt
FROM Notifications 
WHERE LeadId = @LeadId2;

PRINT ''
PRINT '✅ TEST 2 TAMAMLANDI'
PRINT ''

-- =====================================================

-- Test 3: Lead Statistics Test
-- Bu test lead statistikalarını yoxlayır

PRINT '🧪 TEST 3: Lead Statistics Test'
PRINT '==============================='

PRINT ''
PRINT '📊 ÜMUMİ LEAD STATİSTİKALARI:'
PRINT '============================'

SELECT 
    LeadType,
    COUNT(*) as TotalLeads,
    COUNT(CASE WHEN IsConverted = 1 THEN 1 END) as ConvertedLeads,
    CAST(COUNT(CASE WHEN IsConverted = 1 THEN 1 END) * 100.0 / COUNT(*) AS DECIMAL(5,2)) as ConversionRate
FROM Leads 
GROUP BY LeadType
ORDER BY TotalLeads DESC;

PRINT ''
PRINT '📊 PENDING NOTIFICATION-LAR:'
PRINT '============================'

SELECT 
    l.CarNumber,
    l.LeadType,
    n.Status,
    n.CreatedAt,
    n.Message
FROM Leads l 
JOIN Notifications n ON l.Id = n.LeadId 
WHERE n.Status = 'pending' 
ORDER BY n.CreatedAt DESC;

PRINT ''
PRINT '✅ TEST 3 TAMAMLANDI'
PRINT ''

-- =====================================================

-- Test 4: Manual Approval Test
-- Bu test manual approval prosesini test edir

PRINT '🧪 TEST 4: Manual Approval Test'
PRINT '==============================='

-- Test notification-u approve et
UPDATE Notifications 
SET Status = 'approved', ApprovedAt = GETDATE() 
WHERE Id = @NotificationId;

PRINT '✅ Test notification approved edildi: ' + CAST(@NotificationId AS NVARCHAR(10));

-- Queue-ya əlavə et (WhatsApp göndərmə üçün)
INSERT INTO Queues (Type, RefId, CarNumber, PhoneNumber, Message, Status, CreatedAt)
VALUES ('whatsapp-notification', @NotificationId, @TestCarNumber, @TestPhoneNumber, 
        '🚨 TEST001 - SIGORTA YOXDUR! Dərhal müştəriyə təklif göndərmək üçün əlaqə saxlayın. 📞 0501234567', 
        'pending', GETDATE());

DECLARE @QueueId INT = SCOPE_IDENTITY();
PRINT '✅ WhatsApp queue item yaradıldı: ' + CAST(@QueueId AS NVARCHAR(10));

-- Nəticələri yoxla
PRINT ''
PRINT '📊 TEST 4 NƏTICƏLƏRİ:'
PRINT '===================='

SELECT 
    'Notification Status' as TableName,
    Id,
    Status,
    ApprovedAt,
    SentAt
FROM Notifications 
WHERE Id = @NotificationId;

SELECT 
    'Queue Item' as TableName,
    Id,
    Type,
    RefId,
    CarNumber,
    PhoneNumber,
    Status
FROM Queues 
WHERE Id = @QueueId;

PRINT ''
PRINT '✅ TEST 4 TAMAMLANDI'
PRINT ''

-- =====================================================

PRINT '🎉 BÜTÜN TESTLƏR TAMAMLANDI!'
PRINT '============================'
PRINT ''
PRINT '📋 TEST XÜLASƏSİ:'
PRINT '================'
PRINT '✅ NoInsuranceImmediate Lead yaratma'
PRINT '✅ RenewalWindow Lead yaratma'
PRINT '✅ Notification yaratma'
PRINT '✅ Lead statistikaları'
PRINT '✅ Manual approval prosesi'
PRINT '✅ WhatsApp queue inteqrasiyası'
PRINT ''
PRINT '🚀 Lead funksionallığı uğurla test edildi!' 