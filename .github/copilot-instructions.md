# AI Agent Instructions for SigortaYoxla

## Project Overview
**SigortaYoxla** is an automated insurance checker system that uses Hangfire background jobs to:
1. Check car insurance status via web scraping with Selenium WebDriver
2. Send notifications via WhatsApp Web.js
3. Manage tasks through SQL Server queue with web dashboard

## Architecture Pattern
- **Queue-based processing**: All work items flow through `Queue` entities in SQL Server
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

## Current System Architecture

### Queue Management
- Three queue types: `"insurance"` and `"whatsapp"` in `Queue.Type`
- Status flow: `"pending"` → `"processing"` → `"completed"` or `"failed"`
- Priority support with retry mechanisms
- **QueueItems table has been removed** - only use new Queue system

### Insurance Processing
- **Real mode**: Selenium WebDriver + Chrome to scrape ISB.az
- **HTML parsing**: Direct extraction from table elements using CSS selectors
- **PolicyNumber and ExpiryDate fields removed** - no longer stored or processed
- Single worker configuration to prevent multiple browser instances

### WhatsApp Integration
- Node.js WhatsApp Web.js for message sending
- Automated session management
- Message queue processing with retry logic

## Project Structure
```
sigortaYoxla/
├── Program.cs              # Main entry point + Hangfire setup
├── appsettings.json       # Configuration (DB connections, Chrome settings)
│
├── Data/
│   ├── ApplicationDbContext.cs        # EF Core context  
│   └── ApplicationDbContextFactory.cs # Factory for context creation
│
├── Models/
│   ├── Queue.cs            # Main queue entity
│   ├── InsuranceJob.cs     # Insurance job details (linked to Queue)
│   ├── WhatsAppJob.cs      # WhatsApp job details (linked to Queue)
│   └── InsuranceResult.cs  # Insurance result model
│
├── Services/
│   ├── QueueRepository.cs         # Queue management operations
│   ├── InsuranceService.cs        # Insurance checking with Selenium
│   ├── InsuranceJobRepository.cs  # Insurance job operations
│   ├── WhatsAppService.cs         # WhatsApp integration
│   └── WhatsAppJobRepository.cs   # WhatsApp job operations
│
├── Jobs/
│   ├── InsuranceJob.cs    # Background job for insurance checking
│   └── WhatsAppJob.cs     # Background job for WhatsApp sending
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
   - Id, Type, Status, Priority, CreatedAt, StartedAt, CompletedAt, ErrorMessage, RetryCount

2. **InsuranceJobs** - Insurance job details
   - Id, QueueId (FK), CarNumber, Company, VehicleBrand, VehicleModel, Status, ResultText, ProcessingTimeMs, CreatedAt, ProcessedAt

3. **WhatsAppJobs** - WhatsApp job details  
   - Id, QueueId (FK), PhoneNumber, MessageText, DeliveryStatus, ErrorDetails, SentAt, CreatedAt

## Key Features

### Insurance Checking (Real Mode)
- Uses Selenium WebDriver with Chrome
- Scrapes ISB.az website for real insurance data
- HTML table parsing with CSS selectors
- Stores real company names and vehicle information
- **No longer stores PolicyNumber or ExpiryDate**

### Queue Processing
- Priority-based processing
- Automatic retry with failure tracking
- Single worker configuration for browser automation
- Real-time status monitoring

### WhatsApp Integration
- WhatsApp Web.js automation
- Session persistence
- Message queue with retry logic

## Code Review Guidelines

### General Principles
- Check if HTML parsing handles all possible response formats from ISB.az
- Verify error handling in Selenium automation
- Ensure queue operations are transactional
- Validate proper browser cleanup after operations

### Service Classes
- **InsuranceService**: Review HTML selectors for potential website changes
- **WhatsAppService**: Verify process cleanup and proper exit code handling
- **QueueRepository**: Check transaction handling and retry logic

### Job Processing
- Verify jobs don't exceed timeout limits
- Ensure proper status updates (pending → processing → completed/failed)
- Check error handling and retry mechanisms

### Browser Automation
- Validate element selectors are robust
- Check for proper resource cleanup
- Verify error handling for network timeouts

### Database Operations
- Ensure proper indexing for queue queries
- Verify transaction handling in repository methods
- Check connection pooling configuration

## Common Commands

### Database Operations
```powershell
# Check InsuranceJobs table
sqlcmd -S sigortayoxla.database.windows.net -d SigortaYoxlaDb -U a.azar1988 -P "54EhP6.G@RKcp8#" -Q "SELECT TOP 5 CarNumber, Company, VehicleBrand, VehicleModel, Status, CreatedAt FROM InsuranceJobs ORDER BY CreatedAt DESC"

# Check Queue status
sqlcmd -S sigortayoxla.database.windows.net -d SigortaYoxlaDb -U a.azar1988 -P "54EhP6.G@RKcp8#" -Q "SELECT Type, Status, COUNT(*) as Count FROM Queues GROUP BY Type, Status"

# Add test insurance job
sqlcmd -S sigortayoxla.database.windows.net -d SigortaYoxlaDb -U a.azar1988 -P "54EhP6.G@RKcp8#" -Q "INSERT INTO Queues (Type, Status, Priority) VALUES ('insurance', 'pending', 1); DECLARE @QueueId INT = SCOPE_IDENTITY(); INSERT INTO InsuranceJobs (QueueId, CarNumber, Status) VALUES (@QueueId, '10RL096', 'pending');"
```

### Development Tips
- Always use real car numbers for testing (format: 10RL096, 90HB986, etc.)
- Monitor Chrome DevTools for debugging web scraping issues
- Use single worker configuration to prevent browser conflicts
- Check Hangfire dashboard for job status and errors

## Testing Guidelines
- Use real Azerbaijani car plate numbers for authentic testing
- Test with both valid and invalid car numbers
- Verify HTML parsing extracts correct company and vehicle data
- **Do not test PolicyNumber or ExpiryDate** - these fields are no longer used

## Error Handling
- Chrome driver issues: Check chromedriver.exe in bin folder
- Database connection: Verify Azure SQL connection string
- WhatsApp session: Delete auth_data folder to reset session
- HTML parsing failures: Check if ISB.az website structure changed

## Performance Notes
- Single worker prevents multiple browser instances
- Rate limiting between requests to avoid blocking
- Chrome runs in visible mode for debugging
- Database connection pooling via EF Core

## Recent Updates
- **Removed PolicyNumber and ExpiryDate fields** from InsuranceJob model
- **Removed QueueItems table** - migrated to new Queue system
- **Simplified InsuranceResult model** - removed unused fields
- **Updated HTML parsing** - focus on company and vehicle data only
- **Single worker configuration** - prevents browser conflicts
- **Added Code Review Guidelines** - for better code quality and maintenance
- **Updated WhatsApp script references** - now using debug-whatsapp.js instead of whatsapp-sender.js

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
- **[2025-07-18]** Added new feature X - improves performance by 20%
- **[2025-07-15]** Removed deprecated feature Y - no longer needed
```

This ensures the documentation stays current and highlights recent changes for developers.
