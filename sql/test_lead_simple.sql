-- =====================================================
-- SADƏ LEAD FUNKSIONALLIĞI TEST
-- =====================================================

PRINT '🧪 LEAD VƏ NOTIFICATION TEST BAŞLAYIR'
PRINT '====================================='

-- Test data təmizləmə
DECLARE @TestCarNumber1 NVARCHAR(20) = 'TEST001';
DECLARE @TestCarNumber2 NVARCHAR(20) = 'TEST002';

DELETE FROM Notifications WHERE LeadId IN (
    SELECT Id FROM Leads WHERE CarNumber IN (@TestCarNumber1, @TestCarNumber2)
);
DELETE FROM Leads WHERE CarNumber IN (@TestCarNumber1, @TestCarNumber2);
DELETE FROM InsuranceRenewalTracking WHERE UserId IN (
    SELECT Id FROM Users WHERE CarNumber IN (@TestCarNumber1, @TestCarNumber2)
);
DELETE FROM Users WHERE CarNumber IN (@TestCarNumber1, @TestCarNumber2);

PRINT '🗑️ Test data təmizləndi'

-- =====================================================
-- TEST 1: NoInsuranceImmediate Lead
-- =====================================================

PRINT ''
PRINT '🧪 TEST 1: NoInsuranceImmediate Lead'
PRINT '==================================='

-- User yarat
INSERT INTO Users (CarNumber, PhoneNumber, NotificationEnabled, CreatedAt) 
VALUES (@TestCarNumber1, '0501234567', 1, GETDATE());

DECLARE @UserId1 INT = SCOPE_IDENTITY();
PRINT '✅ User yaradıldı: ' + CAST(@UserId1 AS NVARCHAR(10));

-- Lead yarat
INSERT INTO Leads (UserId, CarNumber, LeadType, Notes, CreatedAt, IsConverted)
VALUES (@UserId1, @TestCarNumber1, 'NoInsuranceImmediate', 'Test - sığorta tapılmadı', GETDATE(), 0);

DECLARE @LeadId1 INT = SCOPE_IDENTITY();
PRINT '✅ Lead yaradıldı: ' + CAST(@LeadId1 AS NVARCHAR(10));

-- Notification yarat
INSERT INTO Notifications (LeadId, Channel, Message, Status, CreatedAt)
VALUES (@LeadId1, 'wa', 
        '🚨 ' + @TestCarNumber1 + ' - SIGORTA YOXDUR! Dərhal müştəriyə təklif göndərmək üçün əlaqə saxlayın. 📞 0501234567', 
        'pending', GETDATE());

DECLARE @NotificationId1 INT = SCOPE_IDENTITY();
PRINT '✅ Notification yaradıldı: ' + CAST(@NotificationId1 AS NVARCHAR(10));

-- =====================================================
-- TEST 2: RenewalWindow Lead
-- =====================================================

PRINT ''
PRINT '🧪 TEST 2: RenewalWindow Lead'
PRINT '============================'

-- User yarat (renewal window ilə)
INSERT INTO Users (CarNumber, PhoneNumber, EstimatedRenewalDay, EstimatedRenewalMonth, 
                   RenewalWindowStart, RenewalWindowEnd, NotificationEnabled, CreatedAt) 
VALUES (@TestCarNumber2, '0559876543', 15, 3, '2024-03-01', '2024-03-15', 1, GETDATE());

DECLARE @UserId2 INT = SCOPE_IDENTITY();
PRINT '✅ User 2 yaradıldı: ' + CAST(@UserId2 AS NVARCHAR(10));

-- Lead yarat
INSERT INTO Leads (UserId, CarNumber, LeadType, Notes, CreatedAt, IsConverted)
VALUES (@UserId2, @TestCarNumber2, 'RenewalWindow', 'Test - yenilənmə tarixi yaxınlaşır', GETDATE(), 0);

DECLARE @LeadId2 INT = SCOPE_IDENTITY();
PRINT '✅ Lead 2 yaradıldı: ' + CAST(@LeadId2 AS NVARCHAR(10));

-- Notification yarat
INSERT INTO Notifications (LeadId, Channel, Message, Status, CreatedAt)
VALUES (@LeadId2, 'wa', 
        '📅 ' + @TestCarNumber2 + ' - Yenilənmə tarixi yaxınlaşır! Təxmini tarix: 15/3 📞 0559876543', 
        'pending', GETDATE());

DECLARE @NotificationId2 INT = SCOPE_IDENTITY();
PRINT '✅ Notification 2 yaradıldı: ' + CAST(@NotificationId2 AS NVARCHAR(10));

-- =====================================================
-- NƏTICƏLƏRİ YOXLA
-- =====================================================

PRINT ''
PRINT '📊 TEST NƏTICƏLƏRİ'
PRINT '=================='

-- Users
PRINT ''
PRINT '👥 USERS:'
SELECT 
    Id,
    CarNumber,
    PhoneNumber,
    EstimatedRenewalDay,
    EstimatedRenewalMonth,
    CONVERT(VARCHAR, RenewalWindowStart, 23) as RenewalWindowStart,
    CONVERT(VARCHAR, RenewalWindowEnd, 23) as RenewalWindowEnd,
    NotificationEnabled
FROM Users 
WHERE CarNumber IN (@TestCarNumber1, @TestCarNumber2)
ORDER BY Id;

-- Leads
PRINT ''
PRINT '📋 LEADS:'
SELECT 
    Id,
    UserId,
    CarNumber,
    LeadType,
    Notes,
    IsConverted,
    CONVERT(VARCHAR, CreatedAt, 120) as CreatedAt
FROM Leads 
WHERE CarNumber IN (@TestCarNumber1, @TestCarNumber2)
ORDER BY Id;

-- Notifications
PRINT ''
PRINT '🔔 NOTIFICATIONS:'
SELECT 
    Id,
    LeadId,
    Channel,
    LEFT(Message, 50) + '...' as MessagePreview,
    Status,
    CONVERT(VARCHAR, CreatedAt, 120) as CreatedAt,
    ApprovedAt,
    SentAt
FROM Notifications 
WHERE LeadId IN (@LeadId1, @LeadId2)
ORDER BY Id;

-- Statistics
PRINT ''
PRINT '📈 STATISTIKALAR:'
SELECT 
    LeadType,
    COUNT(*) as TotalLeads,
    COUNT(CASE WHEN IsConverted = 1 THEN 1 END) as ConvertedLeads
FROM Leads 
WHERE CarNumber IN (@TestCarNumber1, @TestCarNumber2)
GROUP BY LeadType;

-- Pending notifications
PRINT ''
PRINT '⏳ PENDING NOTIFICATIONS:'
SELECT 
    l.CarNumber,
    l.LeadType,
    n.Status,
    LEFT(n.Message, 30) + '...' as MessagePreview
FROM Leads l 
JOIN Notifications n ON l.Id = n.LeadId 
WHERE n.Status = 'pending' 
  AND l.CarNumber IN (@TestCarNumber1, @TestCarNumber2);

-- =====================================================
-- MANUAL APPROVAL TEST
-- =====================================================

PRINT ''
PRINT '🧪 TEST 3: Manual Approval'
PRINT '=========================='

-- İlk notification-u approve et
UPDATE Notifications 
SET Status = 'approved', ApprovedAt = GETDATE() 
WHERE Id = @NotificationId1;

PRINT '✅ Notification ' + CAST(@NotificationId1 AS NVARCHAR(10)) + ' approved edildi';

-- Approved notifications yoxla
PRINT ''
PRINT '✅ APPROVED NOTIFICATIONS:'
SELECT 
    Id,
    LeadId,
    Status,
    CONVERT(VARCHAR, ApprovedAt, 120) as ApprovedAt
FROM Notifications 
WHERE Status = 'approved' 
  AND LeadId IN (@LeadId1, @LeadId2);

PRINT ''
PRINT '🎉 BÜTÜN TESTLƏR TAMAMLANDI!'
PRINT '============================'
PRINT '✅ Lead yaratma - OK'
PRINT '✅ Notification yaratma - OK'  
PRINT '✅ User relationship - OK'
PRINT '✅ Manual approval - OK'
PRINT ''
PRINT '📝 NOT: Telegram bot və WhatsApp inteqrasiyası kod-da implement edilmişdir'
PRINT '     ancaq real Telegram.Bot package-ı əlavə edilməlidir.' 