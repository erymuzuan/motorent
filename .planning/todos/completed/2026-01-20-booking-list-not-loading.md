---
created: 2026-01-20T12:45
title: Recent bookings list not showing in deposit dialog
area: ui
files:
  - src/MotoRent.Client/Pages/Staff/TillBookingDepositDialog.razor:60-85
  - src/MotoRent.Services/BookingService.cs
---

## Problem

The "Recent bookings with balance due" list is not displaying any bookings when the dialog opens. User has to search manually to find bookings.

Possible causes:
1. Shop filter is too restrictive - `preferredShopId: ShopId` might exclude bookings from other shops
2. Date range too narrow - only checking last 30 days
3. `CanReceivePayment` filter may be excluding valid bookings
4. Query might need `null` for some filters instead of specific values

## Solution

1. Debug `LoadRecentBookingsAsync` to check:
   - What `ShopId` is being passed (is it the right shop?)
   - What bookings exist in the database
   - What the query returns before filtering
2. Consider removing or relaxing shop filter for bookings (customers may book at any shop)
3. Add logging to trace the query
4. Potentially show "all shops" bookings with a shop indicator badge
5. Check if BookingStatus and BalanceDue > 0 are returning expected results
