# SigortaYoxla

Sığorta yoxlaması və WhatsApp mesaj avtomatlaşdırması.

## Başlatma
1. `appsettings.json`-da connection string qur
2. `dotnet run`
3. Dashboard: http://localhost:5000/hangfire

## Stack
- .NET 9.0 + EF Core
- Hangfire + Azure SQL
- Selenium WebDriver

## 🎯 İstifadə

### Əsas Proqram (Full Pipeline)
```bash
dotnet run
```

Bu komanda:
1. Avtomobil nömrələrini yoxlayır
2. Nəticələri formatlayır  
3. WhatsApp mesajları göndərir

## 🗄️ Azure SQL Database Query-ləri

### MCP Server Configuration
VS Code-da Azure SQL database ilə işləmək üçün MCP server konfiqurasiya edilib:

```json
{
    "mcp.servers": {
        "azure-sql": {
            "command": "sqlcmd",
            "args": [
                "-S", "sigortayoxla.database.windows.net",
                "-d", "SigortaYoxlaDb", 
                "-U", "a.azar1988",
                "-P", "54EhP6.G@RKcp8#",
                "-Q"
            ]
        }
    },
    "mcp.enabled": true
}
```

### SQL Query Nümunələri

#### Command Line ilə Query:
```bash
# Bütün cədvələri göstər
sqlcmd -S sigortayoxla.database.windows.net -d SigortaYoxlaDb -U a.azar1988 -P "54EhP6.G@RKcp8#" -Q "SELECT name FROM sys.tables ORDER BY name"

# QueueItems table-ından son 5 record
sqlcmd -S sigortayoxla.database.windows.net -d SigortaYoxlaDb -U a.azar1988 -P "54EhP6.G@RKcp8#" -Q "SELECT TOP 5 Id, Type, CarNumber, PhoneNumber, IsProcessed, CreatedAt FROM QueueItems ORDER BY CreatedAt DESC"

# Type-a görə statistika
sqlcmd -S sigortayoxla.database.windows.net -d SigortaYoxlaDb -U a.azar1988 -P "54EhP6.G@RKcp8#" -Q "SELECT Type, COUNT(*) as Count FROM QueueItems GROUP BY Type"
```

#### VS Code SQL Extension ilə:
1. `Ctrl+Shift+P` → `MSSQL: Connect`
2. Server: `sigortayoxla.database.windows.net`
3. Database: `SigortaYoxlaDb`
4. User: `a.azar1988`
5. SQL query yazın və `Ctrl+Shift+E` ilə icra edin

#### AI Chat ilə SQL Query:
VS Code-da MCP server aktiv olduqda AI chat-də belə suallar verə bilərsiniz:
- "Azure SQL database-də QueueItems table-ından son 10 record-u göstər"
- "WhatsApp type-ında neçə pending job var?"
- "Bu gün yaradılmış bütün queue item-ləri göstər"

### Database Schema
```sql
-- QueueItems table columns:
Id (int) - Primary key
Type (nvarchar) - 'insurance' və ya 'whatsapp' 
CarNumber (nvarchar) - Avtomobil nömrəsi
PhoneNumber (nvarchar) - Telefon nömrəsi
Message (nvarchar) - WhatsApp mesajı
IsProcessed (bit) - İşlənib/işlənməyib
CreatedAt (datetime2) - Yaradılma tarixi
ProcessedAt (datetime2) - İşlənmə tarixi
Error (nvarchar) - Xəta mesajı
```

### Ayrı-ayrılıqda WhatsApp İstifadəsi

#### Tək mesaj göndər:
```bash
cd whatsapp-bot
node whatsapp-sender.js send 0501234567 "Test mesajı"
```

#### Bulk mesajlar göndər:
```bash
cd whatsapp-bot  
node whatsapp-sender.js bulk messages.json
```

#### Test mesajı:
```bash
cd whatsapp-bot
node whatsapp-sender.js test
```

## 📁 Fayl Strukturu

```
sigortaYoxla/
├── Program.cs              # Əsas proqram
├── SigortaChecker.cs       # Selenium sığorta yoxlayıcısı
├── WhatsAppService.cs      # WhatsApp xidməti
├── azure-sql-test.sql      # SQL query nümunələri
├── .vscode/
│   ├── settings.json       # MCP server konfiqurasiyası
│   └── mssql-connections.json # SQL Server bağlantıları
├── whatsapp-bot/
│   ├── package.json        # Node.js dependencies
│   ├── whatsapp-sender.js  # WhatsApp bot
│   ├── messages.json       # Bulk mesaj nümunəsi
│   └── auth_data/          # WhatsApp session məlumatları
└── README.md
```

## 🔧 Konfiqurasiya

### Telefon Nömrəsi Mapping-i
`Program.cs` faylında `GetPhoneNumberForCar` funksiyasını redaktə edin:

```csharp
private static string GetPhoneNumberForCar(string carNumber)
{
    var phoneMapping = new Dictionary<string, string>
    {
        { "90HB986", "0501234567" },  // Öz telefon nömrənizi yazın
        { "90HB987", "0559876543" },
        // Daha çox mapping əlavə edin...
    };
    
    return phoneMapping.TryGetValue(carNumber, out var phoneNumber) ? phoneNumber : "";
}
```

### WhatsApp Mesaj Formatı
`WhatsAppService.cs`-də `FormatInsuranceMessage` funksiyasını öz ehtiyacınıza görə redaktə edin.

## 📱 WhatsApp Bot Xüsusiyyətləri

- **QR Authentication**: İlk dəfə QR kod skan edin
- **Session Saxlanması**: Növbəti dəfələr avtomatik qoşulur
- **Bulk Messaging**: Çox mesaj paralel göndərə bilir
- **Error Handling**: Uğursuz mesajları qeyd edir
- **Rate Limiting**: Mesajlar arası 2 saniyə gözləmə

## 🔄 Queue İşləri üçün İstifadə

Bu sistem təkrarlanan işlər üçün hazır hazırlanmışdır:

1. **Scheduled Jobs**: Cron job və ya Windows Task Scheduler ilə
2. **Message Queue**: RabbitMQ, Azure Service Bus və s. ilə inteqrasiya
3. **Database Integration**: Avtomobil-telefon mapping-i üçün

## 🛠️ Problemlərin Həlli

### WhatsApp QR Kod Göstərmir
```bash
cd whatsapp-bot
rm -rf auth_data
node whatsapp-sender.js test
```

### C# Build Error
```bash
dotnet clean
dotnet restore  
dotnet build
```

### Node.js Package Issues
```bash
cd whatsapp-bot
rm -rf node_modules
npm install
```

### SQL Connection Issues
```bash
# Test Azure SQL connection
sqlcmd -S sigortayoxla.database.windows.net -d SigortaYoxlaDb -U a.azar1988 -P "54EhP6.G@RKcp8#" -Q "SELECT 1"

# Check if SQL Server extension installed
code --list-extensions | findstr mssql
```

## 📞 Dəstək

Hər hansı problem olduqda issue açın və ya pull request göndərin.

## ⚠️ Qeydlər

- Test rejimində öz telefon nömrənizi istifadə edin
- WhatsApp Business API qaydalarına riayət edin  
- Rate limiting-ə diqqət edin (spam kimi qəbul edilə bilər)
- Auth məlumatlarını (.auth_data/) git-ə commit etməyin
- Database credentials-ı production-da environment variables ilə idarə edin
