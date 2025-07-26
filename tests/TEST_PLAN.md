# Lead & Notification Test Plan

Bu test planı Lead → Notification → Telegram Approval → WhatsApp axınının tam test edilməsi üçün hazırlanıb.

## Test Environment Setup

### Prerequisites
1. **Database**: Azure SQL connection aktiv
2. **Telegram Bot**: Token və admin ID konfiqurasiya edilib
3. **C# Application**: `dotnet run` işlək vəziyyətdə
4. **Test Data**: Təmiz başlanğıc (əvvəlki test data təmizlənib)

### Environment Check Commands
```bash
# Database connection test
sqlcmd -S sigortayoxla.database.windows.net -d SigortamatDb -U a.azar1988 -P "54EhP6.G@RKcp8#" -Q "SELECT 1"

# Telegram bot test
curl "https://api.telegram.org/bot8399345423:AAF9cf9mvp4il39G4N8_vQu6Xu-5cxkgKDM/getMe"

# Application build check
dotnet build
```

## Test Scenarios

### Scenario 1: Create Lead Only Test
**Məqsəd**: Yalnız lead və notification yaratmaq, sonra əsas proqrama buraxmaq

#### Steps:
1. **Test data yaratma**:
   ```bash
   dotnet run -- test create-lead-only
   ```

2. **Gözlənilən çıxış**:
   ```
   ✅ User created with ID: [X]
   ✅ Lead created with ID: [Y] (Type: NoInsuranceImmediate)
   ✅ Notification created with ID: [Z] (Status: pending)
   📱 Telegram mesajı admin-ə göndərildi
   ```

3. **Database verification**:
   ```sql
   -- Son yaradılan notification
   SELECT TOP 1 l.CarNumber, n.Id, n.Status, n.CreatedAt 
   FROM Notifications n 
   JOIN Leads l ON n.LeadId = l.Id 
   ORDER BY n.CreatedAt DESC;
   ```

4. **Telegram confirmation**: Admin Telegram-da yeni mesaj görməli

#### Expected Results:
- ✅ User, Lead, və Notification DB-də yaranmalı
- ✅ Telegram mesajı admin-ə çatmalı
- ✅ Notification status "pending" olmalı

### Scenario 2: Main Application Full Pipeline Test
**Məqsəd**: Əsas proqramı işə salıb tam approval axınını test etmək

#### Steps:
1. **Main application başlatma**:
   ```bash
   dotnet run
   ```

2. **Hangfire jobs monitoring**:
   - Console-da hər 2 saniyədə Telegram job işləməli
   - Hər 2 dəqiqədə WhatsApp job işləməli

3. **Admin approval process**:
   - Telegram-da gələn mesajda "✅ Təsdiqlə" düyməsi basılmalı
   - Mesaj redaktə edilməli (status əlavə edilməli)

4. **WhatsApp queue processing**:
   - Console-da "WhatsApp job started" mesajı
   - "Message processed" və ya "No pending whatsapp jobs" çıxışı

5. **Database verification** (approval-dan sonra):
   ```sql
   -- Notification status yoxlama
   SELECT l.CarNumber, n.Status, n.ApprovedAt, n.SentAt
   FROM Notifications n 
   JOIN Leads l ON n.LeadId = l.Id 
   WHERE n.Id = [NotificationId];
   
   -- Queue item yoxlama
   SELECT Type, Status, CarNumber, PhoneNumber, ProcessedAt
   FROM Queues 
   WHERE RefId = [NotificationId];
   ```

#### Expected Results:
- ✅ Admin təsdiqindən sonra notification status "approved"
- ✅ WhatsApp queue item yaranmalı
- ✅ WhatsApp job mesajı emal etməli
- ✅ Final status "sent" olmalı

### Scenario 3: Full End-to-End Test
**Məqsəd**: Tam dövrə test - yaratma, təsdiq, göndərmə

#### Steps:
1. **Setup phase**:
   ```bash
   # Əvvəlki test data təmizlə
   sqlcmd -S sigortayoxla.database.windows.net -d SigortamatDb -U a.azar1988 -P "54EhP6.G@RKcp8#" -i sql/test_data_cleanup.sql
   ```

2. **Execution phase**:
   ```bash
   # Lead yarat
   dotnet run -- test create-lead-only
   
   # Əsas proqramı başlat (yeni terminal)
   dotnet run
   ```

3. **Manual approval phase**:
   - Telegram-da mesaj gözlə (10-15 saniyə)
   - "✅ Təsdiqlə" düyməsini bas
   - Mesajın redaktə olunduğunu təsdiq et

4. **Monitoring phase**:
   - Console loglarını izlə
   - WhatsApp job çıxışını gözlə
   - Database statusunu yoxla

5. **Verification phase**:
   ```sql
   -- Full pipeline status
   SELECT 
       l.CarNumber,
       l.LeadType,
       n.Status as NotificationStatus,
       n.ApprovedAt,
       n.SentAt,
       q.Status as QueueStatus,
       q.ProcessedAt
   FROM Leads l
   JOIN Notifications n ON l.Id = n.LeadId
   LEFT JOIN Queues q ON q.RefId = n.Id
   WHERE l.CarNumber = '10RL033';
   ```

#### Expected Results:
- ✅ Notification: pending → approved → sent
- ✅ Queue: pending → processed
- ✅ Telegram mesajı redaktə edilib
- ✅ Console-da WhatsApp "success" mesajı

## Test Data Management

### Test Car Numbers
Hər test üçün unikal car number istifadə etmək tövsiyə olunur:
```
10RL033_001, 10RL033_002, 10RL033_003, ...
```

### Cleanup Commands
```bash
# Specific test data cleanup
sqlcmd -S sigortayoxla.database.windows.net -d SigortamatDb -U a.azar1988 -P "54EhP6.G@RKcp8#" -Q "DELETE FROM Notifications WHERE Id > 100; DELETE FROM Leads WHERE Id > 100; DELETE FROM Users WHERE Id > 100;"

# Full test data cleanup
sqlcmd -S sigortayoxla.database.windows.net -d SigortamatDb -U a.azar1988 -P "54EhP6.G@RKcp8#" -i sql/test_data_cleanup.sql
```

## Troubleshooting

### Common Issues

#### 1. "Query is too old" Telegram Error
**Səbəb**: Admin 10+ saniyə sonra approval basmışdır
**Həll**: 
- Mesaj hələ də redaktə olunmalıdır
- Notification status yenilənməlidir
- Normal proses davam etməlidir

#### 2. WhatsApp Job "No pending jobs"
**Səbəb**: Approval prosesi tamamlanmamışdır
**Yoxlama**:
```sql
SELECT Status FROM Notifications WHERE Id = [NotificationId];
```
**Həll**: Status "approved" olana qədər gözləyin

#### 3. Telegram Message Not Received
**Səbəb**: Bot token və ya admin ID səhvdir
**Yoxlama**:
```bash
curl "https://api.telegram.org/bot8399345423:AAF9cf9mvp4il39G4N8_vQu6Xu-5cxkgKDM/getUpdates"
```

#### 4. Database Connection Issues
**Həll**:
```bash
# Connection test
sqlcmd -S sigortayoxla.database.windows.net -d SigortamatDb -U a.azar1988 -P "54EhP6.G@RKcp8#" -Q "SELECT GETDATE()"
```

## Performance Expectations

### Response Times
- **Lead creation**: <2 saniyə
- **Telegram message send**: <5 saniyə
- **Admin approval processing**: <3 saniyə
- **WhatsApp queue addition**: <2 saniyə
- **Total pipeline**: 30-60 saniyə (admin approval daxil)

### Success Criteria
- ✅ **95%+ Telegram delivery rate**
- ✅ **90%+ WhatsApp processing success**
- ✅ **<10 saniyə** automation response time
- ✅ **Zero data corruption** during testing

## Test Report Template

```markdown
## Test Execution Report
**Date**: [Date]
**Tester**: [Name]
**Scenario**: [Scenario Name]

### Results:
- [ ] Lead created successfully
- [ ] Notification pending status
- [ ] Telegram message received
- [ ] Admin approval processed  
- [ ] WhatsApp queue populated
- [ ] Message sent successfully
- [ ] Database states correct

### Issues Found:
[None / List issues]

### Performance Notes:
[Timing observations]

### Recommendations:
[Suggestions for improvement]
```

## Automated Test Integration

Test planını automated testing ilə genişləndirmək üçün:

```bash
# Future: Automated test runner
dotnet test --filter "Category=LeadPipeline"

# Future: Performance testing
dotnet run -- test performance --iterations=10

# Future: Load testing  
dotnet run -- test load --concurrent=5
```

Bu test planı Lead & Notification sisteminin tam və etibarlı test edilməsini təmin edir. Hər test ssenariосын mütəmadi olaraq icra edərək sistemin sabitliyini qorumaq mümkündür. 