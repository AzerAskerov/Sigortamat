# SQL Scripts Collection

Bu qovluqda proyektin bütün SQL skriptləri yerləşir. Hər bir skript müəyyən məqsəd üçün nəzərdə tutulub və müvafiq qaydada istifadə edilməlidir.

## Test Skriptləri

### Əsas Test Skriptləri
- **`setup_single_test.sql`** - Tək avtomobil üçün test data yaradır
- **`setup_bulk_test.sql`** - 15 avtomobil üçün bulk test data yaradır  
- **`test_data_cleanup.sql`** - Test datalarını təmizləyir
- **`test_lead_functionality.sql`** - Lead funksionallığının tam test skripti
- **`test_lead_simple.sql`** - Sadə lead test skripti

### Verificasiya Skriptləri
- **`renewal_verification.sql`** - Renewal tracking prosesinin vəziyyətini yoxlayır
- **`step_by_step_verification.sql`** - Addım-addım proses yoxlaması

## Database Migrasiya Skriptləri

### Schema Dəyişiklikləri
- **`add_renewal_window_to_users.sql`** - Users cədvəlinə renewal window sahələri əlavə edir
- **`add_whatsapp_queue.sql`** - WhatsApp queue sahələri əlavə edir

## İstifadə Təlimatları

### Single Test Setup
```bash
sqlcmd -S sigortayoxla.database.windows.net -d SigortamatDb -U a.azar1988 -P "54EhP6.G@RKcp8#" -i setup_single_test.sql
```

### Bulk Test Setup  
```bash
sqlcmd -S sigortayoxla.database.windows.net -d SigortamatDb -U a.azar1988 -P "54EhP6.G@RKcp8#" -i setup_bulk_test.sql
```

### Test Data Cleanup
```bash
sqlcmd -S sigortayoxla.database.windows.net -d SigortamatDb -U a.azar1988 -P "54EhP6.G@RKcp8#" -i test_data_cleanup.sql
```

### Verification
```bash
sqlcmd -S sigortayoxla.database.windows.net -d SigortamatDb -U a.azar1988 -P "54EhP6.G@RKcp8#" -i renewal_verification.sql
```

## Test Avtomobil Nömrələri

Bu skriptlərdə istifadə edilən test avtomobil nömrələri:

```
99JP083  99JL074  99JP086  99JL076  99JL075  90AM566
90AM533  99JP075  99JP087  77JG472  99JK047  99JP081
77JV167  99JS099  90JC930  90JK930  99JF483  99JV526
77JG327  77JK538  77JK590  99JF842  77JD145  77JB587
01AD795  01AD794  50CY385  55CE825  77QY058  77DX441
77RQ865  20CZ125  77KY920  74BB838  99ZY083
```

## Faylların Təsviri

| Fayl | Məqsəd | İstifadə |
|------|--------|----------|
| `setup_single_test.sql` | Tək NV test | Development və debugging |
| `setup_bulk_test.sql` | Çoxlu NV test | Performance test |
| `test_data_cleanup.sql` | Data təmizləmə | Test əvvəli hazırlıq |
| `renewal_verification.sql` | Status yoxlama | Process monitoring |
| `step_by_step_verification.sql` | Ətraflı analiz | Deep debugging |
| `test_lead_functionality.sql` | Lead test | Feature testing |
| `test_lead_simple.sql` | Sadə lead test | Quick testing |
| `add_renewal_window_to_users.sql` | Schema update | Database migration |
| `add_whatsapp_queue.sql` | Queue update | Feature migration |

## Qeydlər

- Bütün skriptlər production database üçün təhlükəsizdir
- Test data real istifadəçi məlumatlarını təsir etmir
- Cleanup skriptləri yalnız test datalarını silir
- Migrasiya skriptləri idempotent-dir (təkrar işə salınması təhlükəsizdir) 