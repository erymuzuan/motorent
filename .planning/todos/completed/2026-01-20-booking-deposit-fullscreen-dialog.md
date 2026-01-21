---
created: 2026-01-20T12:45
title: Booking deposit dialog needs fullscreen mobile UX
area: ui
files:
  - src/MotoRent.Client/Pages/Staff/TillBookingDepositDialog.razor
  - src/MotoRent.Client/Components/Till/DenominationEntryPanel.razor
---

## Problem

The Booking Deposit Dialog is too cramped on mobile devices:
1. Denomination entry panel takes too much vertical space
2. Hard to see booking details and payment summary simultaneously
3. Dialog doesn't utilize full screen on mobile
4. User must scroll extensively to complete payment

Screenshot shows denomination list taking majority of dialog space, leaving little room for payment summary and action buttons.

## Solution

1. Make dialog fullscreen on mobile (use Tabler fullscreen modal or offcanvas pattern)
2. Make denomination panel collapsible/expandable:
   - Show total and "Edit denominations" button when collapsed
   - Expand to show full denomination grid when editing
3. Consider wizard-style flow for mobile:
   - Step 1: Select booking
   - Step 2: Enter payment (currency + denominations)
   - Step 3: Confirm and record
4. Keep booking summary sticky at top
5. Keep action buttons sticky at bottom
