-- Test data-nı təmizlə və 50DD444 üçün yeni test case yarat
PRINT '--- BAŞLANĞIÇ: Test məlumatlarının təmizlənməsi və yeni test case yaradılması';

-- Addım 1: Mövcud məlumatların silinməsi (foreign key constraints ucbatindan sira verecnli)
PRINT '--- Mövcud məlumatlar silinir...';
DELETE FROM WhatsAppJobs;
DELETE FROM InsuranceJobs;
DELETE FROM Queues;
DELETE FROM InsuranceRenewalTracking;
DELETE FROM Users;
PRINT '--- Köhnə məlumatlar silindi.';

-- Addım 2: Cədvəllərin Identity sayğaclarının sıfırlanması
PRINT '--- Identity sayğacları sıfırlanır...';
DBCC CHECKIDENT ('Users', RESEED, 0);
DBCC CHECKIDENT ('InsuranceRenewalTracking', RESEED, 0);
DBCC CHECKIDENT ('Queues', RESEED, 0);
DBCC CHECKIDENT ('InsuranceJobs', RESEED, 0);
DBCC CHECKIDENT ('WhatsAppJobs', RESEED, 0);
PRINT '--- Identity sayğacları sıfırlandı.';

-- Addım 3: 77BV028 üçün vahid test keysinin yaradılması
PRINT '--- 77BV028 üçün yeni test keysi yaradılır...';

DECLARE @CarNumber NVARCHAR(20) = '77BV028';
DECLARE @UserId INT;
DECLARE @TrackingId INT;
DECLARE @QueueId INT;

-- İstifadəçi yarat
INSERT INTO Users (CarNumber, PhoneNumber, NotificationEnabled, CreatedAt)
VALUES (@CarNumber, '0559876543', 1, GETDATE());
SET @UserId = SCOPE_IDENTITY();
PRINT 'İstifadəçi yaradıldı. ID: ' + CAST(@UserId AS VARCHAR);

-- Renewal tracking yarat
INSERT INTO InsuranceRenewalTracking (UserId, CurrentPhase, NextCheckDate, ChecksPerformed, CreatedAt)
VALUES (@UserId, 'Initial', GETDATE(), 0, GETDATE());
SET @TrackingId = SCOPE_IDENTITY();
PRINT 'İzləmə prosesi yaradıldı. ID: ' + CAST(@TrackingId AS VARCHAR);

-- Queue yarat
INSERT INTO Queues (Type, Status, Priority, RetryCount, CreatedAt)
VALUES ('insurance', 'pending', 1, 0, GETDATE());
SET @QueueId = SCOPE_IDENTITY();
PRINT 'Növbə yaradıldı. ID: ' + CAST(@QueueId AS VARCHAR);

-- Insurance Job yarat
INSERT INTO InsuranceJobs (QueueId, CarNumber, CheckDate, InsuranceRenewalTrackingId, Status, CreatedAt)
VALUES (@QueueId, @CarNumber, GETDATE(), @TrackingId, 'pending', GETDATE());
PRINT 'Sığorta işi yaradıldı və növbəyə bağlandı. Job Queue ID: ' + CAST(@QueueId AS VARCHAR);

PRINT '--- Vahid test keysi uğurla yaradıldı.';

-- Addım 4: Yaradılan məlumatların yoxlanması
PRINT '--- Yoxlama ---';
SELECT * FROM Users WHERE CarNumber = @CarNumber;
SELECT * FROM InsuranceRenewalTracking WHERE UserId = @UserId;
SELECT * FROM Queues WHERE Id = @QueueId;
SELECT * FROM InsuranceJobs WHERE QueueId = @QueueId; 