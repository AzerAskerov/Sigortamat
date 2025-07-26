# Sigortamat

Avtomatlaşdırılmış sığorta sistemi - Sığorta yoxlaması və WhatsApp mesaj avtomatlaşdırması + Lead idarəetməsi və Telegram admin təsdiqi.

## 📚 Documentation

Bu proyektin tam sənədləşdirməsi aşağıdakı qovluqlarda təşkil edilib:

### 🎯 Quick Start
1. **[Project Overview](knowledge-base/project-overview.md)** - Layihəyə giriş və əsas xüsusiyyətlər
2. **[Installation & Setup](knowledge-base/project-overview.md#başlatma)** - Quraşdırma və konfiqurasiya
3. **[Test Plan](tests/TEST_PLAN.md)** - Sistem test etmə təlimatları

### 📖 Complete Documentation
- **[Knowledge Base](knowledge-base/README.md)** - Bütün sənədlərin indeksi
- **[Architecture](knowledge-base/architecture.md)** - Sistem arxitekturası  
- **[Insurance Renewal](knowledge-base/insurance-renewal.md)** - Renewal tracking sistemi
- **[Lead Notifications](knowledge-base/lead-notifications.md)** - Lead və approval pipeline
- **[SQL Scripts](sql/README.md)** - Database skriptləri və migrasiyalar

## 🚀 Quick Commands

```bash
# Sistemi işə sal
dotnet run

# Test lead yarat
dotnet run -- test create-lead-only

# SQL test data yaradma
sqlcmd -S sigortayoxla.database.windows.net -d SigortamatDb -U a.azar1988 -P "54EhP6.G@RKcp8#" -i sql/setup_single_test.sql

# Hangfire dashboard
# http://localhost:5000/hangfire
```

## 🆕 Latest Features (v0.3.0)

- 🤖 **Telegram Bot Approval**: Admin approval pipeline
- 📊 **Lead Management**: Automated lead detection  
- 📅 **Renewal Window Tracking**: Enhanced renewal date tracking
- 🎯 **Enhanced Binary Search**: Company-based renewal date detection

## 📞 Support

Ətraflı məlumat üçün [Knowledge Base](knowledge-base/README.md) bölməsinə baxın.
