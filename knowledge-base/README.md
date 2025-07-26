# Sigortamat Knowledge Base

Bu qovluqda proyektin bütün əsas sənədləşdirmə materialları yerləşir. Hər bir sənəd müəyyən sahəni əhatə edir və layihəni başa düşmək üçün ardıcıl oxunmalıdır.

## 📋 Table of Contents

### 1. Project Overview
- **[Project Overview](project-overview.md)** - Layihənin ümumi təqdimatı, əsas xüsusiyyətlər və texnologiyalar

### 2. Architecture & Technical Documentation  
- **[Architecture](architecture.md)** - Sistem arxitekturası, komponentlər və texniki spesifikasiyalar
- **[Insurance Renewal](insurance-renewal.md)** - Sığorta yenilənmə izləmə sistemi və alqoritmlər

### 3. Lead & Notification System
- **[Lead Notifications](lead-notifications.md)** - Lead idarəetməsi və Telegram approval pipeline sistemi

### 4. Project History
- **[Changelog](changelog.md)** - Versiya tarixçəsi və əlavə edilən xüsusiyyətlər

## 🗂️ Sənəd Strukturu

```
knowledge-base/
├── README.md                 # Bu fayl - məzmun cədvəli
├── project-overview.md       # Layihə ümumi təqdimatı  
├── architecture.md           # Texniki arxitektura
├── insurance-renewal.md      # Sığorta renewal sistemi
├── lead-notifications.md     # Lead və notification sistemi
└── changelog.md              # Versiya dəyişiklikləri
```

## 📖 Oxuma ardıcıllığı

Layihə ilə tanış olmaq üçün aşağıdakı ardıcıllıqla oxumaq tövsiyə olunur:

1. **Başlanğıc**: [Project Overview](project-overview.md)
2. **Texniki detallar**: [Architecture](architecture.md)  
3. **Əsas funksiya**: [Insurance Renewal](insurance-renewal.md)
4. **Yeni xüsusiyyət**: [Lead Notifications](lead-notifications.md)
5. **Tarixçə**: [Changelog](changelog.md)

## 🔗 Digər resurslar

### SQL Scripts
- **Qovluq**: `/sql/` - Bütün SQL skriptləri və migrasiyalar
- **README**: [SQL README](../sql/README.md)

### Test Documentation  
- **Qovluq**: `/tests/` - Test planları və ssenariləri
- **Test Plan**: [TEST_PLAN.md](../tests/TEST_PLAN.md)

### Kod bazası
- **Models**: `/Models/` - Data modellər və entity-lər
- **Services**: `/Services/` - Biznes məntiqi xidmətləri
- **Jobs**: `/Jobs/` - Background job-lar və Hangfire task-lar

## ⚠️ Qeydlər

- Sənədlər versiya 0.3.0-a uyğundur
- Bütün sənədlər layihə ilə sinxrondadır
- Dəyişikliklər changelog-da qeyd edilir
- Texniki suallar üçün architecture.md-ə baxın 