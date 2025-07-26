# Changelog

All notable changes to this project will be documented in this file.

## [0.3.0] - 2025-07-26
### Added
- **Lead Management System**: New `Lead` model to track potential sales opportunities (NoInsuranceImmediate, RenewalWindow, etc.)
- **Notification Approval Pipeline**: Telegram bot integration for admin approval before sending WhatsApp messages
- **TelegramBotService**: Automated approval requests to admin via Telegram with inline keyboard buttons
- **User Renewal Window**: Added `RenewalWindowStart` and `RenewalWindowEnd` fields to Users table for precise renewal period tracking
- **Enhanced Binary Search**: MonthSearch phase now supports both VAR/YOX strategy and company-based strategy for finding renewal dates
- **Immediate Lead Generation**: System creates leads instantly when no insurance data is found
- New database tables: `Leads` and `Notifications` for comprehensive lead and notification management
- Bulk test data setup via `setup_bulk_test.sql` for testing 15 different car numbers simultaneously

### Changed
- MonthSearch completion condition changed from 31 days to 14 days for tighter renewal window estimation
- `ProcessInitialPhaseAsync` now handles "no insurance" cases by creating leads immediately
- `GetPreviousJobAsync` logic corrected to find chronologically correct previous jobs in reverse search sequence
- Enhanced logging throughout RenewalTrackingService for better debugging and analysis

### Fixed
- YearSearch to MonthSearch phase transition bug where system incorrectly identified previous jobs
- Binary search failure when all jobs had insurance but different companies (ATƏŞGAH vs AZƏRBAYCAN SƏNAYE)
- EF Core migration conflicts with duplicate column additions
- SQL script variable scoping issues in bulk test data setup

## [0.2.0] - 2025-07-22
### Added
- Users & InsuranceRenewalTracking tables, EF Core models and migrations.
- RenewalTrackingService implementing multi-phase (Initial, YearSearch, MonthSearch, FinalCheck) algorithm.
- Priority / Retry / ProcessAfter support in Queues; extended InsuranceJobs columns.
- Logging via Microsoft.Extensions.Logging.Console.
- `setup_single_test.sql`, `test_data_cleanup.sql`, `renewal_verification.sql`, `step_by_step_verification.sql` helper scripts.
- Sample test car numbers section in README.

### Changed
- `Program.cs` registers new repositories/services and simplified `CustomJobActivator`.
- WhatsApp job schedule changed to run every 2 minutes.
- Updated documentation in ARCHITECTURE.md and insurance-renewal.md.

### Removed
- Web layer/controller references (console-only application). 