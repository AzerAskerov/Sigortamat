-- Step 1: Clean up all existing test data
PRINT '--- BAŞLANĞIÇ: Bütün test məlumatları təmizlənir...';

-- To avoid foreign key issues, delete in reverse order of creation
DELETE FROM InsuranceJobs;
DELETE FROM Queues;
DELETE FROM InsuranceRenewalTracking;
DELETE FROM Users;
PRINT '--- Köhnə məlumatlar silindi.';

-- Step 2: Reset identity counters to start from 1 again
PRINT '--- Identity sayğacları sıfırlanır...';
DBCC CHECKIDENT ('InsuranceJobs', RESEED, 0);
DBCC CHECKIDENT ('Queues', RESEED, 0);
DBCC CHECKIDENT ('InsuranceRenewalTracking', RESEED, 0);
DBCC CHECKIDENT ('Users', RESEED, 0);
PRINT '--- Identity sayğacları sıfırlandı.';


-- Step 3: Insert data for multiple NV numbers
PRINT '--- Çoxlu test NV üçün data yaradılır...';

DECLARE @NVList TABLE (CarNumber NVARCHAR(20));
INSERT INTO @NVList (CarNumber) VALUES
('99JP083'), ('77JG472'), ('90AM566'), ('90HB987'), ('77JG327'),
('10RL033'), ('99JF842'), ('77JD145'), ('77JV167'), ('99JL076'),
('77JV472'), ('77JG145'), ('90AM123'), ('10RL999'), ('99JP999');

DECLARE @CurrentCarNumber NVARCHAR(20);
DECLARE @UserId INT;
DECLARE @TrackingId INT;
DECLARE @QueueId INT;

DECLARE cur CURSOR FOR SELECT CarNumber FROM @NVList;
OPEN cur;
FETCH NEXT FROM cur INTO @CurrentCarNumber;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- User
    INSERT INTO Users (CarNumber, PhoneNumber, NotificationEnabled, CreatedAt)
    VALUES (@CurrentCarNumber, '0559876543', 1, GETDATE());
    SET @UserId = SCOPE_IDENTITY();

    -- Tracking
    INSERT INTO InsuranceRenewalTracking (UserId, CurrentPhase, CreatedAt, ChecksPerformed)
    VALUES (@UserId, 'Initial', GETDATE(), 0);
    SET @TrackingId = SCOPE_IDENTITY();

    -- Queue
    INSERT INTO Queues (Type, Status, CreatedAt, Priority, RetryCount)
    VALUES ('insurance', 'pending', GETDATE(), 1, 0);
    SET @QueueId = SCOPE_IDENTITY();

    -- Job
    INSERT INTO InsuranceJobs (QueueId, CarNumber, Status, CreatedAt, CheckDate, InsuranceRenewalTrackingId)
    VALUES (@QueueId, @CurrentCarNumber, 'pending', GETDATE(), GETDATE(), @TrackingId);

    PRINT 'Test data yaradıldı: ' + @CurrentCarNumber;

    FETCH NEXT FROM cur INTO @CurrentCarNumber;
END

CLOSE cur;
DEALLOCATE cur;

PRINT '--- Çoxlu test NV üçün data yaradılması tamamlandı!'; 