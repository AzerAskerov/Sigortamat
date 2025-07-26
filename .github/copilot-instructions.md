# AI Agent Instructions for Sigortamat

## Project Overview
**Sigortamat** is an automated insurance checker system that uses Hangfire background jobs to:
1. Check car insurance status via web scraping with Selenium WebDriver
2. Send notifications via WhatsApp Web.js
3. Manage tasks through SQL Server queue with web dashboard
4. **NEW**: Lead management and Telegram bot approval system for notifications

## Architecture Pattern
- **Queue-based processing**: All work items flow through `Queue` entities in SQL Server
- **Dual-language approach**: C# for background services, Node.js for WhatsApp automation
- **Console + Web hybrid**: Console app hosts both Hangfire server and web dashboard
- **Service-oriented**: Clear separation between `InsuranceService`, `WhatsAppService`, and `QueueRepository`
- **NEW**: Telegram bot integration for admin approval workflow
- **NEW**: Lead generation and notification approval pipeline

## Key Development Workflows

### Running the Application
```powershell
dotnet run
# Access dashboard at http://localhost:5000/hangfire
# Press ENTER to show queue status, ESC to exit
# Telegram bot automatically starts for admin approvals
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

### NEW: Telegram Bot Configuration
```json
{
  "Telegram": {
    "BotToken": "8399345423:AAF9cf9mvp4il39G4N8_vQu6Xu-5cxkgKDM",
    "AdminId": 1762884854
  }
}
```

## Current System Architecture

### Queue Management
- Queue types: `"insurance"`, `"whatsapp"`, and `"whatsapp-notification"` in `Queue.Type`
- Status flow: `"pending"` → `"processing"` → `"completed"` or `"failed"`
- Priority support with retry mechanisms
- **ProcessAfter field** for scheduling future job processing (ISB.az daily limit management)
- **QueueItems table has been removed** - only use new Queue system

### Insurance Processing
- **Real mode**: Selenium WebDriver + Chrome to scrape ISB.az
- **HTML parsing**: Direct extraction from table elements using CSS selectors
- **PolicyNumber and ExpiryDate fields removed** - no longer stored or processed
- Single worker configuration to prevent multiple browser instances
- **NEW**: Lead generation for "no insurance" cases

### WhatsApp Integration
- Node.js WhatsApp Web.js for message sending
- Automated session management
- Message queue processing with retry logic
- **NEW**: Admin approval integration via Telegram bot

### NEW: Lead Management System
- **Lead Types**: NoInsuranceImmediate, RenewalWindow, CompanyChange
- **Notification Pipeline**: Leads → Notifications → Telegram approval → WhatsApp sending
- **Admin Approval**: Telegram bot with inline keyboard for approvals
- **Tracking**: Lead conversion and ROI metrics

### NEW: Telegram Bot Service
- **Long-polling**: No webhooks required, direct API polling
- **Inline Keyboards**: Approval buttons for notifications
- **Admin Authorization**: Only configured admin can approve
- **Error Recovery**: Automatic reconnection and retry logic

## Project Structure
```
sigortamat/
├── Program.cs              # Main entry point + Hangfire + Telegram setup
├── appsettings.json       # Configuration (DB connections, Chrome, Telegram)
│
├── Data/
│   ├── ApplicationDbContext.cs        # EF Core context  
│   └── ApplicationDbContextFactory.cs # Factory for context creation
│
├── Models/
│   ├── Queue.cs            # Main queue entity
│   ├── InsuranceJob.cs     # Insurance job details (linked to Queue)
│   ├── WhatsAppJob.cs      # WhatsApp job details (linked to Queue)
│   ├── InsuranceResult.cs  # Insurance result model
│   ├── User.cs             # User entity (+ RenewalWindowStart/End fields)
│   ├── Lead.cs ⭐          # NEW: Lead management
│   ├── Notification.cs ⭐   # NEW: Notification approval system
│   └── InsuranceRenewalTracking.cs # Renewal tracking
│
├── Services/
│   ├── QueueRepository.cs              # Queue management operations
│   ├── InsuranceService.cs             # Insurance checking with Selenium
│   ├── InsuranceJobRepository.cs       # Insurance job operations
│   ├── WhatsAppService.cs              # WhatsApp integration
│   ├── WhatsAppJobRepository.cs        # WhatsApp job operations
│   ├── RenewalTrackingService.cs       # Insurance renewal date tracking
│   ├── TelegramBotService.cs ⭐        # NEW: Telegram bot management
│   ├── LeadService.cs ⭐               # NEW: Lead creation and management
│   └── NotificationService.cs ⭐        # NEW: Notification approval workflow
│
├── Jobs/
│   ├── InsuranceJob.cs    # Background job for insurance checking
│   ├── WhatsAppJob.cs     # Background job for WhatsApp sending
│   └── TelegramBotHostedService.cs ⭐ # NEW: Telegram bot background service
│
├── whatsapp-bot/          # Node.js WhatsApp automation
│   ├── package.json
│   ├── debug-whatsapp.js  # WhatsApp integration script
│   └── auth_data/         # WhatsApp session storage
│
└── Migrations/            # EF Core database migrations
```

## Database Schema

### Main Tables:
1. **Queues** - Central queue management
   - Id, Type, Status, Priority, CreatedAt, StartedAt, CompletedAt, ErrorMessage, RetryCount, ProcessAfter

2. **InsuranceJobs** - Insurance job details
   - Id, QueueId (FK), CarNumber, Company, VehicleBrand, VehicleModel, Status, ResultText, ProcessingTimeMs, CheckDate, InsuranceRenewalTrackingId, CreatedAt, ProcessedAt

3. **WhatsAppJobs** - WhatsApp job details  
   - Id, QueueId (FK), PhoneNumber, MessageText, DeliveryStatus, ErrorDetails, SentAt, CreatedAt

4. **Users** - User data and renewal date estimates
   - Id, CarNumber, PhoneNumber, EstimatedRenewalDay, EstimatedRenewalMonth, LastConfirmedRenewalDate, NotificationEnabled, RenewalWindowStart ⭐, RenewalWindowEnd ⭐, CreatedAt, UpdatedAt

5. **InsuranceRenewalTracking** - Insurance renewal date tracking
   - Id, UserId (FK), CurrentPhase, LastCheckDate, NextCheckDate, ChecksPerformed, LastCheckResult, CreatedAt, UpdatedAt

6. **NEW: Leads** ⭐ - Lead management
   - Id, UserId (FK), CarNumber, LeadType, Notes, CreatedAt, IsConverted

7. **NEW: Notifications** ⭐ - Notification approval system
   - Id, LeadId (FK), Channel, Message, Status, CreatedAt, ApprovedAt, SentAt

## Key Features

### Insurance Checking (Real Mode)
- Uses Selenium WebDriver with Chrome
- Scrapes ISB.az website for real insurance data
- HTML table parsing with CSS selectors
- Stores real company names and vehicle information
- **No longer stores PolicyNumber or ExpiryDate**
- **NEW**: Creates leads when no insurance data found

### Queue Processing
- Priority-based processing
- Automatic retry with failure tracking
- Single worker configuration for browser automation
- Real-time status monitoring

### WhatsApp Integration
- WhatsApp Web.js automation
- Session persistence
- Message queue with retry logic
- **NEW**: Admin approval required before sending

### Insurance Renewal Tracking
- **Multi-phase tracking system** to determine insurance renewal dates
- Phase flow: `"Initial"` → `"YearSearch"` → `"MonthSearch"` → `"FinalCheck"` → `"Completed"`
- Binary search algorithm to efficiently determine renewal dates
- ISB.az daily limit management with scheduled processing
- User notification planning based on discovered renewal dates
- Automatic job rescheduling when daily limits are exceeded
- **NEW**: Enhanced binary search with company-based strategy
- **NEW**: Immediate lead generation for "no insurance" cases

### NEW: Lead Management System ⭐
- **Lead Types**: NoInsuranceImmediate, RenewalWindow, CompanyChange
- **Automated Lead Creation**: System creates leads based on insurance status
- **Conversion Tracking**: Monitor lead to customer conversion rates
- **Integration**: Seamless integration with renewal tracking system

### NEW: Telegram Bot Approval System ⭐
- **Admin Notifications**: Real-time approval requests via Telegram
- **Inline Keyboards**: One-click approval buttons
- **Long-polling**: No webhook setup required
- **Security**: Admin-only access with configured chat ID
- **Error Recovery**: Automatic reconnection on network issues

## Code Review Guidelines

### General Principles
- Check if HTML parsing handles all possible response formats from ISB.az
- Verify error handling in Selenium automation
- Ensure queue operations are transactional
- Validate proper browser cleanup after operations
- **NEW**: Verify Telegram bot token security and admin authorization
- **NEW**: Check lead generation logic for edge cases

### Service Classes
- **InsuranceService**: Review HTML selectors for potential website changes
- **WhatsAppService**: Verify process cleanup and proper exit code handling
- **QueueRepository**: Check transaction handling and retry logic
- **NEW**: **TelegramBotService**: Verify callback handling and error recovery
- **NEW**: **LeadService**: Check lead creation logic and business rules
- **NEW**: **NotificationService**: Verify approval workflow and status transitions

### Job Processing
- Verify jobs don't exceed timeout limits
- Ensure proper status updates (pending → processing → completed/failed)
- Check error handling and retry mechanisms
- **NEW**: Verify Telegram bot hosted service lifecycle management

### Browser Automation
- Validate element selectors are robust
- Check for proper resource cleanup
- Verify error handling for network timeouts

### Database Operations
- Ensure proper indexing for queue queries
- Verify transaction handling in repository methods
- Check connection pooling configuration
- **NEW**: Verify foreign key relationships for leads and notifications
- **NEW**: Check renewal window field updates

### NEW: Telegram Integration
- Validate bot token configuration and security
- Check admin chat ID authorization
- Verify callback data validation and sanitization
- Test error recovery and reconnection logic
- Validate inline keyboard functionality

## Common Commands

### Database Operations
```powershell
# Check InsuranceJobs table
sqlcmd -S sigortayoxla.database.windows.net -d SigortamatDb -U a.azar1988 -P "54EhP6.G@RKcp8#" -Q "SELECT TOP 5 CarNumber, Company, VehicleBrand, VehicleModel, Status, CreatedAt FROM InsuranceJobs ORDER BY CreatedAt DESC"

# Check Queue status
sqlcmd -S sigortayoxla.database.windows.net -d SigortamatDb -U a.azar1988 -P "54EhP6.G@RKcp8#" -Q "SELECT Type, Status, COUNT(*) as Count FROM Queues GROUP BY Type, Status"

# NEW: Check leads and notifications
sqlcmd -S sigortayoxla.database.windows.net -d SigortamatDb -U a.azar1988 -P "54EhP6.G@RKcp8#" -Q "SELECT l.CarNumber, l.LeadType, n.Status, n.CreatedAt FROM Leads l LEFT JOIN Notifications n ON l.Id = n.LeadId ORDER BY l.CreatedAt DESC"

# NEW: Check pending approvals
sqlcmd -S sigortayoxla.database.windows.net -d SigortamatDb -U a.azar1988 -P "54EhP6.G@RKcp8#" -Q "SELECT l.CarNumber, n.Message, n.Status FROM Notifications n JOIN Leads l ON n.LeadId = l.Id WHERE n.Status = 'pending'"

# Add test insurance job
sqlcmd -S sigortayoxla.database.windows.net -d SigortamatDb -U a.azar1988 -P "54EhP6.G@RKcp8#" -Q "INSERT INTO Queues (Type, Status, Priority) VALUES ('insurance', 'pending', 1); DECLARE @QueueId INT = SCOPE_IDENTITY(); INSERT INTO InsuranceJobs (QueueId, CarNumber, Status) VALUES (@QueueId, '10RL096', 'pending');"

# Add test WhatsApp job
sqlcmd -S sigortayoxla.database.windows.net -d SigortamatDb -U a.azar1988 -P "54EhP6.G@RKcp8#" -Q "INSERT INTO Queues (Type, Status, Priority) VALUES ('whatsapp', 'pending', 1); DECLARE @QueueId INT = SCOPE_IDENTITY(); INSERT INTO WhatsAppJobs (QueueId, PhoneNumber, MessageText, DeliveryStatus) VALUES (@QueueId, '994707877878', '🎉 Test mesajı!', 'pending');"

# Check renewal tracking status
sqlcmd -S sigortayoxla.database.windows.net -d SigortamatDb -U a.azar1988 -P "54EhP6.G@RKcp8#" -Q "SELECT u.CarNumber, t.CurrentPhase, t.ChecksPerformed, u.EstimatedRenewalDay, u.EstimatedRenewalMonth, u.RenewalWindowStart, u.RenewalWindowEnd FROM Users u JOIN InsuranceRenewalTracking t ON u.Id = t.UserId ORDER BY t.CreatedAt DESC"

# Add test renewal tracking
sqlcmd -S sigortayoxla.database.windows.net -d SigortamatDb -U a.azar1988 -P "54EhP6.G@RKcp8#" -Q "INSERT INTO Users (CarNumber) VALUES ('10RL033'); DECLARE @UserId INT = SCOPE_IDENTITY(); INSERT INTO InsuranceRenewalTracking (UserId, CurrentPhase) VALUES (@UserId, 'Initial');"
```

### NEW: Telegram Bot Testing
```bash
# Test bot token
curl "https://api.telegram.org/bot8399345423:AAF9cf9mvp4il39G4N8_vQu6Xu-5cxkgKDM/getMe"

# Get admin chat updates
curl "https://api.telegram.org/bot8399345423:AAF9cf9mvp4il39G4N8_vQu6Xu-5cxkgKDM/getUpdates"

# Send test message to admin
curl -X POST "https://api.telegram.org/bot8399345423:AAF9cf9mvp4il39G4N8_vQu6Xu-5cxkgKDM/sendMessage" -H "Content-Type: application/json" -d '{"chat_id": 1762884854, "text": "Test message from bot"}'
```

### Development Tips
- Always use real car numbers for testing (format: 10RL096, 90HB986, etc.)
- Monitor Chrome DevTools for debugging web scraping issues
- Use single worker configuration to prevent browser conflicts
- Check Hangfire dashboard for job status and errors
- **NEW**: Monitor Telegram bot logs for approval workflow issues
- **NEW**: Test lead generation with various insurance status scenarios

## Testing Guidelines
- Use real Azerbaijani car plate numbers for authentic testing
- Test with both valid and invalid car numbers
- Verify HTML parsing extracts correct company and vehicle data
- **Do not test PolicyNumber or ExpiryDate** - these fields are no longer used
- **NEW**: Test Telegram bot approval workflow end-to-end
- **NEW**: Verify lead creation for different scenarios (no insurance, company change, etc.)
- **NEW**: Test notification approval and WhatsApp integration

## Error Handling
- Chrome driver issues: Check chromedriver.exe in bin folder
- Database connection: Verify Azure SQL connection string
- WhatsApp session: Delete auth_data folder to reset session
- HTML parsing failures: Check if ISB.az website structure changed
- **NEW**: Telegram bot connection: Verify token and admin chat ID
- **NEW**: Lead creation failures: Check user and tracking relationships
- **NEW**: Notification approval errors: Verify callback data format

## Performance Notes
- Single worker prevents multiple browser instances
- Rate limiting between requests to avoid blocking
- Chrome runs in visible mode for debugging
- Database connection pooling via EF Core
- **NEW**: Telegram long-polling optimized for low latency
- **NEW**: Lead creation batched with database transactions

## Recent Updates
- **[2025-07-26]** Added comprehensive Lead Management System with NoInsuranceImmediate, RenewalWindow, and CompanyChange lead types
- **[2025-07-26]** Implemented Telegram Bot approval system for admin notification approval before WhatsApp sending
- **[2025-07-26]** Added TelegramBotService with long-polling, inline keyboards, and error recovery
- **[2025-07-26]** Enhanced RenewalTrackingService with immediate lead generation for "no insurance" cases
- **[2025-07-26]** Added RenewalWindowStart and RenewalWindowEnd fields to Users table for precise renewal period tracking
- **[2025-07-26]** Enhanced binary search algorithm in MonthSearch phase with company-based strategy (ATƏŞGAH vs AZƏRBAYCAN SƏNAYE)
- **[2025-07-26]** Changed MonthSearch completion condition from 31 days to 14 days for tighter renewal window estimation
- **[2025-07-26]** Fixed YearSearch to MonthSearch phase transition bug in GetPreviousJobAsync method
- **[2025-07-26]** Added bulk test data setup via setup_bulk_test.sql for testing 15 different car numbers simultaneously
- **[2025-07-19]** Added insurance renewal tracking system with multi-phase approach (Initial → YearSearch → MonthSearch → FinalCheck → Completed)
- **[2025-07-19]** Added ProcessAfter field to Queue model for future job scheduling and ISB.az daily limit management
- **[2025-07-19]** Implemented binary search algorithm for determining insurance renewal dates efficiently
- **[2025-07-19]** Created Users and InsuranceRenewalTracking tables for renewal date tracking and user management
- **[2025-07-19]** Added RenewalTrackingService and NotificationService for comprehensive renewal management
- **[2025-07-18]** Project renamed from "SigortaYoxla" to "Sigortamat" - includes all namespaces, project files, and database name updates
- **[2025-07-18]** Legacy references remain in Migration files and .env configuration - intentionally preserved for EF Core compatibility
- **[2025-07-18]** WhatsApp queue testing via direct SQL commands - successfully created test queue with phone 994707877878
- **Removed PolicyNumber and ExpiryDate fields** from InsuranceJob model
- **Removed QueueItems table** - migrated to new Queue system
- **Simplified InsuranceResult model** - removed unused fields
- **Updated HTML parsing** - focus on company and vehicle data only
- **Single worker configuration** - prevents browser conflicts
- **Added Code Review Guidelines** - for better code quality and maintenance
- **Updated WhatsApp script references** - now using debug-whatsapp.js instead of whatsapp-sender.js

## Legacy References (Intentionally Preserved)
- **Migration files**: Contain old "SigortaYoxla" references - do not modify these as EF Core requires historical accuracy
- **.env file**: Contains "sigortayoxla" in server name and some connection strings - preserved for database connectivity
- **Database server name**: sigortayoxla.database.windows.net remains unchanged for Azure SQL connection
- **Git repository**: Repository name remains "sigortaYoxla" on GitHub for historical continuity

## Updating These Instructions

When making significant changes to the codebase, please follow these steps:

1. **Update the relevant sections** in this instructions file to reflect the current architecture
2. **Add a bullet point to the "Recent Updates" section** describing the change
3. **Include the date** of the update in the bullet point
4. **Remove outdated entries** from the "Recent Updates" section after they become well-established parts of the system
5. **Ensure any removed features** are also removed from all other sections of this document

Example format for "Recent Updates" section:
```
## Recent Updates
- **[2025-07-26]** Added comprehensive Telegram bot approval system - improves notification workflow security
- **[2025-07-25]** Removed deprecated feature Y - no longer needed
```

This ensures the documentation stays current and highlights recent changes for developers.
