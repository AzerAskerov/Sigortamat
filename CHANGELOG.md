# Changelog

All notable changes to this project will be documented in this file.

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

--- 