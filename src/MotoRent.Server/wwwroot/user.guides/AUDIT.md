# Documentation Audit - 26 Jan 2026

## Missing Features
- [x] **Document Template Designer**: Visual editor for receipts, agreements, and confirmations. (Created 09-template-designer-guide.md)
- [x] **SaaS Onboarding**: New tenant signup flow and initial shop setup. (Created 12-saas-onboarding-guide.md)
- [x] **Agent Management**: Registering agents, tracking bookings, and processing commissions. (Created 10-agent-management-guide.md)
- [x] **Service Locations**: Managing drop-off points and specific shop locations. (Created 11-service-locations-guide.md)
- [x] **Pricing Calendar**: Visual management of dynamic pricing rules. (Referenced in 01-orgadmin-quickstart.md)

## Required Updates to Existing Guides

### 01-orgadmin-quickstart.md
- [x] Add section on Document Template Designer.
- [x] Add section on SaaS Onboarding (getting started).
- [x] Expand Pricing Rules to include the Pricing Calendar.
- [x] Add Agent Management overview.

### 02-staff-quickstart.md
- [x] Update Accident Reporting section to mention Parties and Costs tabs.

### 04-shopmanager-quickstart.md
- [x] Add Agent Management (commissions and tracking).
- [x] Add Service Locations configuration.
- [x] Expand on Vehicle Pools.

### 06-superadmin-guide.md
- [x] Ensure it covers organization management and impersonation (Verified, branding updated).

### General
- [x] Verify all screenshots are up to date (Updated product name to MotoRent).

---

## JaleOS Malaysian Market Adaptation - Mar 2026

All guides updated to reflect the JaleOS deployment targeting the Malaysian market (jaleos branch).

### Branding Changes
- [x] Replaced "MotoRent" with "JaleOS" across all guide titles, body text, and footer taglines.

### Market-Specific Changes
- [x] **Currency**: THB (Thai Baht) → MYR (Malaysian Ringgit) in all examples and references.
- [x] **Payment Methods**: PromptPay → Touch 'n Go eWallet / FPX online banking.
- [x] **Supported Currencies**: Updated multi-currency list (THB → MYR base, added SGD, removed RUB).
- [x] **Emergency Number**: 191 (Thailand) → 999 (Malaysia).
- [x] **Messaging**: Removed Line app reference, kept WhatsApp (primary in Malaysia).
- [x] **Location Examples**: Phuket/Patong → Langkawi/Penang/Pantai Cenang.
- [x] **Business Names**: "Patong Rentals Co." → "Langkawi Rides Sdn Bhd".

### Pricing Preset Changes
- [x] **01-orgadmin-quickstart.md**: "Create Thailand Preset" → "Create Malaysia Preset" with Malaysian holidays.
- [x] **04-shopmanager-quickstart.md**: Same pricing preset update.
- [x] Holidays: Songkran → Hari Raya Aidilfitri, added School Holidays (Jun-Jul).
- [x] High/Peak Season: Nov 15 - Apr 15 → Dec 1 - Feb 28.

### Tourist Guide (05)
- [x] "vacation in Thailand" → "vacation in Malaysia".
- [x] Speed limit guidance added for Malaysian roads.
- [x] Driving rules retained (left-side driving applies to both countries).

### Onboarding Guide (12)
- [x] Updated example org and shop names for Malaysian context.
- [x] Currency default changed to MYR.