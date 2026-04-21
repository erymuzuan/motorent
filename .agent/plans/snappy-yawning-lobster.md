# Plan: Generate Excel Test Plan Workbooks from Markdown

## Context
The 3 manual test plan markdown files in `test-plans/` need to be converted to Excel (.xlsx) workbooks for actionable, trackable test execution. Each workbook should have structured columns for status tracking, tester assignment, dates, and notes.

## Approach
Create a small standalone .NET 10 console tool that parses the markdown test plans and generates formatted Excel workbooks using ClosedXML.

## Files to Create

### 1. `test-plans/generate-excel/GenerateExcel.csproj`
Minimal .NET 10 console project with ClosedXML dependency.

### 2. `test-plans/generate-excel/Program.cs`
Main program that:
- Reads the 3 markdown files from `../01-*.md`, `../02-*.md`, `../03-*.md`
- Parses each file extracting:
  - Title, Role, Prerequisites
  - Sections (name, route)
  - Test cases (TC ID, name, precondition, steps, expected results)
- Generates one `.xlsx` file per test plan in the `test-plans/` folder

### 3. Output Files (generated, not committed)
- `test-plans/01-cashier-booking-test-plan.xlsx`
- `test-plans/02-checkin-staff-test-plan.xlsx`
- `test-plans/03-checkout-staff-test-plan.xlsx`

## Excel Workbook Structure (per file)

### Sheet 1: "Summary"
- Title row (merged, bold): Test plan name
- Role, Access Policy, Primary Pages
- Prerequisites checklist
- Section-by-section progress table:
  | Section | Total | Passed | Failed | Blocked | Not Tested | % Complete |
- Overall progress bar (conditional formatting)

### Sheet 2: "Test Cases"
Flat table with columns:
| Column | Width | Description |
|--------|-------|-------------|
| TC ID | 10 | e.g., TC-1.1 |
| Section | 30 | Section name |
| Test Case | 40 | Test case name |
| Precondition | 40 | Precondition text (if any) |
| Steps | 60 | Numbered steps (newline-separated) |
| Expected Result | 60 | Expected outcomes (newline-separated) |
| Status | 12 | Dropdown: Not Tested / Pass / Fail / Blocked / Skipped |
| Tester | 15 | Free text |
| Test Date | 12 | Date format |
| Notes | 40 | Free text |
| Priority | 10 | Auto: "Core" for happy paths, "Edge" for edge cases |

### Formatting
- Frozen header row + auto-filter
- Status column: data validation dropdown (Not Tested, Pass, Fail, Blocked, Skipped)
- Conditional formatting on Status: green=Pass, red=Fail, yellow=Blocked, gray=Skipped
- Row banding for readability
- Word wrap on Steps and Expected columns
- Column auto-fit with max widths

## Markdown Parser Logic
The markdown structure is consistent across all 3 files:
- `# Title` -> workbook title
- `## Role Description` -> metadata block
- `## Prerequisites` -> checklist items (`- [ ] text`)
- `## Section N: Name` -> section grouping
- `### TC-X.Y: Name` -> test case start
- `**Precondition:**` -> optional precondition line
- `**Steps:**` followed by `1. 2. 3.` -> steps
- `**Expected:**` followed by `- bullet` -> expected results

## How to Run
```powershell
cd test-plans/generate-excel
dotnet run
# Outputs .xlsx files to test-plans/
```

Can be re-run anytime the markdown test plans are updated.

## Key Dependencies
- **ClosedXML** (MIT license) - Excel generation, already compatible with .NET 10

## Verification
1. Run `dotnet build` on the generator project - should compile
2. Run `dotnet run` - should produce 3 .xlsx files
3. Open each .xlsx in Excel and verify:
   - Summary sheet has correct counts
   - Test Cases sheet has all TCs from the markdown
   - Status dropdown works
   - Conditional formatting applies
   - Columns are readable width
