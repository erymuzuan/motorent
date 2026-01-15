# Automated Maintenance Alerts - Manual Verification Guide

This guide outlines the steps for QA to manually verify the implementation of the Automated Maintenance Alerts system.

## Pre-requisites
1. **Database Update:** Ensure the SQL schema is applied: `database/012-maintenance-alerts-schema.sql`.
2. **Environment:** The web application and scheduler must be configured to point to the same database.
3. **Data Setup:** At least one vehicle should have a `MaintenanceSchedule` that is either "Due Soon" or "Overdue" (based on date or mileage).

---

## Test Case 1: Backend Alert Generation (Scheduler)
**Objective:** Verify that the background task correctly identifies due maintenance and generates alerts.

1. **Execute the Scheduler Runner:**
   Open a terminal and run:
   ```powershell
   dotnet run --project src\MotoRent.Scheduler\MotoRent.Scheduler.csproj -- /r:MaintenanceAlertRunner
   ```
2. **Verify Output:**
   - Confirm the console output shows: `Starting automated maintenance alert generation...`
   - Confirm the final output shows: `Alert generation completed. X new alerts created.` (where X > 0 if due items exist).

---

## Test Case 2: Dashboard Widget Visibility
**Objective:** Verify that maintenance alerts are visible to staff on the home dashboard.

1. **Login:** Log in as a Staff, Shop Manager, or Org Admin.
2. **View Dashboard:** Navigate to the Home page (`/`).
3. **Verify Widget:**
   - Confirm the "Maintenance Alerts" widget is present in the main content area.
   - If alerts exist, confirm they are listed with the correct vehicle license plate and service type.
   - Confirm the "View All" button is present.

---

## Test Case 3: Maintenance Alerts Page & Navigation
**Objective:** Verify the dedicated alerts management page.

1. **Navigation:** Open the "Fleet" menu in the navigation bar.
2. **Click Link:** Click on "Maintenance Alerts".
3. **Verify Page:**
   - URL should be `/maintenance/alerts`.
   - Title should be "Maintenance Alerts".
   - Table should display columns: Vehicle, Service, Status, Triggered, and Actions.
   - Status badges should be colored (Red for Overdue, Yellow/Orange for Due Soon).

---

## Test Case 4: Manual Trigger (Admin Only)
**Objective:** Verify that administrators can manually trigger a check from the UI.

1. **Login:** Log in as an **Org Admin**.
2. **Navigate:** Go to `/maintenance/alerts`.
3. **Action:** Click the "Trigger Manual Check" button in the header.
4. **Verify:**
   - A success toast should appear saying: "Manual maintenance check triggered. X new alerts created."
   - The list should refresh automatically.

---

## Test Case 5: Mark Alert as Read
**Objective:** Verify that alerts can be acknowledged without being fully resolved.

1. **Action:** Click the "Mark as Read" (eye) icon for any alert in the list.
2. **Verify:**
   - A toast message should appear: "Alert marked as read."
   - The alert should be removed from the current "Active" list.

---

## Test Case 6: Resolve Alert
**Objective:** Verify that alerts can be resolved with notes.

1. **Action:** Click the "Resolve" (check) icon for any alert.
2. **Input:** In the prompt dialog, enter resolution notes (e.g., "Oil changed by Mechanic A").
3. **Verify:**
   - A toast message should appear: "Alert resolved successfully."
   - The alert should be removed from the list.
   - (Optional) Verify in the database that the alert record now has `ResolvedDate`, `ResolvedBy`, and `Notes` populated.
