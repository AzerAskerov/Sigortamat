# AI Agent Instructions for SigortaYoxla

## Project Overview
**SigortaYoxla** is an automated insurance checker system that uses Hangfire background jobs to:
1. Check car insurance status via web scraping
2. Send notifications via WhatsApp Web.js
3. Manage tasks through SQL Server queue with web dashboard

## Architecture Pattern
- **Queue-based processing**: All work items flow through `QueueItem` entities in SQL Server
- **Dual-language approach**: C# for background services, Node.js for WhatsApp automation
- **Console + Web hybrid**: Console app hosts both Hangfire server and web dashboard
- **Service-oriented**: Clear separation between `InsuranceService`, `WhatsAppService`, and `QueueRepository`

## Key Development Workflows

### Running the Application
```powershell
dotnet run
# Access dashboard at http://localhost:5000/hangfire
# Press ENTER to show queue status, ESC to exit
```

### Setting Up WhatsApp Bot
```powershell
cd whatsapp-bot
npm install
# First run requires QR code scan for WhatsApp Web.js authentication
```

### Database Migrations
```powershell
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

## Project-Specific Patterns

### Queue Processing Convention
- Two queue types: `"insurance"` and `"whatsapp"` in `QueueItem.Type`
- All queue operations go through static `QueueRepository` methods
- Jobs mark items as processed using `MarkAsProcessed(id, error?)`
- Rate limiting: 1s for insurance, 3s for WhatsApp

### Service Integration Pattern
```csharp
// Services don't use DI - direct instantiation in jobs
public InsuranceJob() {
    _insuranceService = new InsuranceService();
}
```

### Console Output Convention
- Emojis for visual status: 🚗 🔄 ✅ ❌ 📱 📋
- Structured headers with padding: `"=".PadRight(40, '=')`
- Azerbaijani language for user messages

### Cross-Process Communication
- C# calls Node.js via `Process.Start()` with command line args
- WhatsApp service expects: `node debug-whatsapp.js {phone} "{message}"`
- Exit codes determine success/failure (0 = success)

## Critical Configuration
- **Connection String**: Uses LocalDB by default in `appsettings.json`
- **Hangfire Queues**: `["insurance", "whatsapp", "default"]` with 2 workers
- **Recurring Jobs**: Insurance (every minute), WhatsApp (every 2 minutes)
- **Dashboard Auth**: `AllowAllAuthorizationFilter` - no authentication

## File Organization Rules
- **Models/**: Data entities (only `QueueItem` currently)
- **Services/**: Business logic (Insurance, WhatsApp, QueueRepository)
- **Jobs/**: Hangfire job classes with `[Queue]` attributes
- **Data/**: EF Core context and factory
- **whatsapp-bot/**: Node.js WhatsApp automation (separate package.json)

## Common Debugging Points
- Check `QueueRepository.ShowQueueStatus()` for queue state
- WhatsApp requires authenticated session in `auth_data/session/`
- Insurance service is currently simulated (2s delay)
- Database issues: verify LocalDB/SQL Server connection

## Integration Dependencies
- **External**: WhatsApp Web (requires phone authentication)
- **Internal**: SQL Server for queue persistence
- **Runtime**: Node.js must be available in PATH for WhatsApp service

When modifying this codebase:
- Maintain the emoji console output style
- Use static methods in `QueueRepository` for data access
- Add appropriate rate limiting for external services
- Follow the queue-first processing pattern for new features