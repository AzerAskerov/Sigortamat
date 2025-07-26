/*
 * Script: add_renewal_window_to_users.sql
 * Purpose: Adds RenewalWindowStart and RenewalWindowEnd columns to Users table.
 * Run only once after deploying the code changes.
 */

ALTER TABLE Users
    ADD RenewalWindowStart DATE NULL,
        RenewalWindowEnd   DATE NULL;

PRINT 'Columns RenewalWindowStart and RenewalWindowEnd added to Users.'; 