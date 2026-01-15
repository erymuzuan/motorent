# Dynamic Pricing QA Test Suite

## Overview
This test suite covers the Dynamic Pricing feature for MotoRent, including regional presets, pricing calendar, and rental workflow integration.

## Test Cases

| Test ID | Name | Priority | Dependency |
|---------|------|----------|------------|
| DP-001 | [Enable Dynamic Pricing Feature](01-enable-feature.md) | Critical | None |
| DP-002 | [Apply Regional Preset](02-apply-regional-preset.md) | High | DP-001 |
| DP-003 | [Pricing Calendar View](03-pricing-calendar.md) | High | DP-001, DP-002 |
| DP-004 | [Rental Check-in with Dynamic Pricing](04-rental-checkin-pricing.md) | Critical | DP-001, DP-002 |
| DP-005 | [Invoice Dynamic Pricing Display](05-invoice-pricing-display.md) | High | DP-004 |

## Execution Order
1. **DP-001** - Must pass first (enables the feature)
2. **DP-002** - Creates pricing rules for testing
3. **DP-003** - Can run after DP-002
4. **DP-004** - Requires rules from DP-002
5. **DP-005** - Requires rental from DP-004

## Test Environment Requirements
- MotoRent application running
- At least one organization configured
- At least one shop created
- At least one vehicle available
- At least one renter in system
- OrgAdmin or ShopManager access

## Regional Presets Reference

| Preset | Region | Rules | Primary Demographics |
|--------|--------|-------|---------------------|
| Andaman Coast | Phuket, Krabi | 58 | Russian, European, Chinese |
| Gulf Coast | Koh Samui | 14 | Backpackers, Party tourists |
| Southern Border | Hat Yai | 32 | Malaysian, Singaporean |
| Northern | Chiang Mai | 14 | Chinese, Japanese, Digital nomads |
| Eastern | Pattaya | 14 | Russian, Chinese |
| Central | Bangkok | 11 | Chinese, Business travelers |
| Western | Hua Hin | 9 | Domestic Thai |
| Isaan | Udon Thani | 10 | Domestic Thai |

## Test Data Suggestions

### High Season Dates (High Multipliers)
- December 20 - January 10 (Christmas/New Year)
- January 1-14 (Russian New Year)
- January 28 - February 4 (Chinese New Year)

### Low Season Dates (Low Multipliers)
- June 1 - August 31 (Southwest Monsoon)
- May 15 - October 14 (General low season)

### Event Dates
- April 13-17 (Songkran)
- November 5-6 (Loy Krathong)

## Localization
All test cases are available in:
- English (.md)
- Thai (.th.md)

## Pass/Fail Reporting
Use the checkboxes in each test case to track pass/fail status.

## Notes
- Run tests in a test environment, not production
- Reset test data between full test runs if needed
- Screenshots are recommended for bug reports
