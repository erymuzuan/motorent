# Cashier Till Guide

This guide covers the cashier till system for managing cash drawer operations, payments, payouts, and shift reconciliation.

## Overview

The till system is the central hub for all financial transactions during your shift. Before processing any rentals, you must open a till session. All payments, refunds, and payouts are tracked through your till.

**Key Concepts:**
- **Till Session** - Your personal cash drawer session for the day
- **Opening Float** - Starting cash amount in your drawer
- **Cash In** - Money received (rental payments, deposits)
- **Cash Out** - Money paid out (refunds, fuel, commissions)
- **Reconciliation** - Balancing your drawer at end of shift

## Getting Started

### Opening Your Till

You must open a till before processing any check-ins or check-outs.

1. Click the **Open Till** button in the header (or navigate to `/staff/till`)
2. Select the **Shop** you're working at (if you have access to multiple shops)
3. Enter your **Opening Float** amount (the cash you're starting with)
4. Add any **Notes** (optional)
5. Click **Open Session**

**Important Rules:**
- You can only have one open till per shop per day
- You must close your current session before opening a new one
- The shop is locked to your till - all transactions go through it

### Till Dashboard

Once your till is open, the dashboard shows:

| Section | Description |
|---------|-------------|
| **Session Info** | Your name, shop, start time |
| **Cash In** | Total money received this session |
| **Cash Out** | Total money paid out this session |
| **Expected Balance** | Opening float + Cash In - Cash Out |
| **Quick Actions** | Payout, Drop, Top Up, Close |
| **Recent Transactions** | Last 10 transactions |

## Recording Transactions

### Automatic Transactions

These are recorded automatically when you process rentals:

| Transaction Type | When It Happens |
|-----------------|-----------------|
| **Rental Payment** | Customer pays for rental at check-in |
| **Security Deposit** | Customer pays cash deposit |
| **Card Payment** | Customer pays by card (tracked, doesn't affect cash) |
| **PromptPay** | Customer pays via PromptPay (tracked, doesn't affect cash) |
| **Deposit Refund** | Cash refund at check-out |
| **Damage Charge** | Customer pays for damage |

### Manual Payouts

Use the **Record Payout** button for cash going out of your drawer.

#### Fuel Reimbursement
When you reimburse customers for fuel:
1. Click **Record Payout**
2. Select **Fuel Reimbursement**
3. Enter the amount
4. Enter the customer/recipient name
5. Enter receipt number (if any)
6. Add notes (e.g., "Filled tank for rental #123")
7. Click **Save**

#### Agent Commission
When paying commissions to agents who brought customers:
1. Click **Record Payout**
2. Select **Agent Commission**
3. Enter the amount
4. Enter the agent's name
5. Reference the booking or rental
6. Click **Save**

#### Petty Cash
For miscellaneous expenses:
1. Click **Record Payout**
2. Select **Petty Cash**
3. Enter the amount
4. Describe what it was for (e.g., "Office supplies")
5. Enter receipt number
6. Click **Save**

**Always keep receipts for payouts!**

## Receiving Payments from Till

The Till page has a **Quick Payments** section for directly receiving payments without going through the full check-in flow.

### Quick Payment Buttons

| Button | Use For |
|--------|---------|
| **Rental Payment** | Additional payments for existing rentals |
| **Deposit** | Security deposits for active rentals |
| **Booking Deposit** | Deposits for advance bookings |

### Recording a Rental Payment

For when a customer needs to pay more on an existing rental:

1. On your Till page, click **Rental Payment**
2. Search for the rental by ID or customer name
3. Select the rental from the results
4. Enter the payment amount
5. Select payment method (Cash, Card, PromptPay, Bank Transfer)
6. Click **Record Payment**
7. Payment appears in your transactions

### Recording a Security Deposit

For collecting a deposit on an active rental:

1. Click **Deposit**
2. Search for the rental by ID
3. Verify the rental details
4. Enter deposit amount and type (Cash or Card Pre-auth)
5. Click **Record Deposit**

### Recording a Booking Deposit

When a customer pays deposit for an advance booking:

1. Click **Booking Deposit**
2. Enter the booking reference (6-character code like "ABC123")
3. Or search by customer name
4. Verify booking details (dates, deposit required)
5. Enter the deposit amount (cannot exceed balance due)
6. Select payment method
7. Click **Record Payment**
8. A receipt is automatically generated for the customer

**Note**: The booking's payment status updates automatically:
- **Unpaid** → **Partially Paid** → **Fully Paid**

### Cash Drop

When you have too much cash in your drawer and need to move it to the safe:

1. Click **Cash Drop**
2. Enter the amount being dropped
3. Add notes (e.g., "Afternoon drop to safe")
4. Click **Confirm**

This reduces your expected balance but is tracked separately.

### Top Up

If you need more cash in your drawer (e.g., for making change):

1. Click **Top Up**
2. Enter the amount being added
3. Add notes (e.g., "Change from safe")
4. Click **Confirm**

This increases your expected balance.

## Processing Payments at Check-In

When processing a new rental:

1. **Open your till first** - Check-in won't work without an active till
2. Complete the check-in steps (customer, vehicle, dates, etc.)
3. At the payment step:
   - **Cash**: Entered into your till automatically
   - **Card**: Tracked but doesn't affect your cash balance
   - **PromptPay/Bank Transfer**: Tracked as electronic payment
4. After check-in, a receipt is generated and can be printed

### Split Payments

Customers can pay with multiple methods:
- Part cash, part card
- Different currencies for cash (with exchange rate)

The system tracks each payment separately.

### Multi-Currency Cash

For foreign currency cash payments:
1. Select the currency (USD, EUR, GBP, etc.)
2. Enter the amount in that currency
3. The exchange rate converts it to THB
4. Your till tracks the THB equivalent

Supported currencies: THB, USD, EUR, GBP, CNY, JPY, AUD, RUB

## Processing Refunds at Check-Out

When a customer returns a vehicle:

1. The system calculates:
   - Deposit held
   - Any additional charges (extra days, damage, etc.)
   - Refund amount (deposit minus charges)
2. If cash refund:
   - Automatically recorded as **Deposit Refund** in your till
   - Reduces your expected balance
3. Print the settlement receipt

## Receipts

Receipts are automatically generated for:

| Receipt Type | When Generated |
|--------------|----------------|
| **Check-In Receipt** | After successful check-in |
| **Settlement Receipt** | After check-out |
| **Booking Deposit Receipt** | When booking deposit is paid |

### Receipt Features
- **Print**: Click the Print button to open print dialog
- **Reprint**: Find receipt in `/finance/receipts` to reprint
- **Void**: Managers can void receipts with a reason

### Receipt Number Format
`RCP-YYMMDD-XXXXX` (e.g., RCP-260117-00042)

## Closing Your Till

At the end of your shift:

1. Click **Close Shift** on your till dashboard
2. Count your physical cash
3. Enter the **Actual Cash** amount
4. The system shows:
   - **Expected**: What should be in drawer
   - **Actual**: What you counted
   - **Variance**: Difference (short or over)
5. If there's a variance:
   - Add notes explaining the difference
   - Acknowledge the variance
6. Click **Close Session**

### Variance Handling

| Variance | Status | Action |
|----------|--------|--------|
| None (0) | Closed | Session closes normally |
| Short (-) | Closed with Variance | Manager will review |
| Over (+) | Closed with Variance | Manager will review |

**Tips for Accurate Reconciliation:**
- Count cash carefully, twice if needed
- Check for stuck bills or coins
- Review transactions if variance is unexpected
- Report any discrepancies immediately

## Viewing History

### Transaction History
On your till page, click **View History** to see:
- All transactions for current session
- Filter by type (Cash In, Cash Out)
- Search by description

### Past Sessions
Navigate to `/staff/till` and click **Session History** to see:
- Previous sessions with status
- Total cash in/out per session
- Variance amounts
- Verification status

## Daily Workflow

### Start of Shift
1. [ ] Log in to the system
2. [ ] Click **Open Till** in the header
3. [ ] Select your shop
4. [ ] Count and enter your opening float
5. [ ] Verify the float matches what's in the drawer

### During Your Shift
1. [ ] Process check-ins (payments auto-recorded)
2. [ ] Process check-outs (refunds auto-recorded)
3. [ ] Record any payouts with receipts
4. [ ] Do cash drops if drawer gets too full
5. [ ] Keep transaction receipts organized

### End of Shift
1. [ ] Review your transaction list
2. [ ] Count your physical cash
3. [ ] Click **Close Shift**
4. [ ] Enter actual cash amount
5. [ ] Explain any variance
6. [ ] Close the session
7. [ ] Secure the cash

## Troubleshooting

### "No Active Till" Error
You tried to process a rental without an open till.
- **Solution**: Open your till first, then retry

### Cannot Open Till
You may already have an open session for that shop.
- **Solution**: Close your existing session first

### Variance at Close
Your counted cash doesn't match expected.
- **Solution**:
  1. Recount the cash
  2. Review transactions for errors
  3. Check for missed payouts
  4. Note the variance and close

### Receipt Won't Print
- **Solution**:
  1. Check printer connection
  2. Try the reprint option from `/finance/receipts`
  3. Contact your manager if issue persists

## Best Practices

1. **Always open till before starting work** - Don't process rentals without it
2. **Get receipts for all payouts** - Keep them organized
3. **Do regular cash drops** - Don't keep too much in drawer
4. **Count carefully at close** - Take your time
5. **Report issues immediately** - Don't wait until end of day
6. **Keep notes** - Document anything unusual

## Quick Reference

| Action | Location |
|--------|----------|
| Open Till | Header button or `/staff/till` |
| View Till | `/staff/till` |
| Record Payment | Till page > Rental Payment |
| Record Deposit | Till page > Deposit |
| Booking Deposit | Till page > Booking Deposit |
| Record Payout | Till page > Fuel/Agent/Petty Cash |
| Cash Drop | Till page > Cash Drop |
| Close Till | Till page > Close Shift |
| View Receipts | `/finance/receipts` |
| Session History | Till page > View History |

## Need Help?

- **Till Issues**: Contact your Shop Manager
- **System Errors**: Contact IT Support
- **Variance Questions**: Discuss with Manager before closing

---

*MotoRent - Vehicle Rental Management System*
