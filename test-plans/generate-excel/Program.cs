using System.Text.RegularExpressions;
using ClosedXML.Excel;

var testPlanDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
var mdFiles = new[]
{
    "01-cashier-booking-test-plan.md",
    "02-checkin-staff-test-plan.md",
    "03-checkout-staff-test-plan.md"
};

foreach (var mdFile in mdFiles)
{
    var mdPath = Path.Combine(testPlanDir, mdFile);
    if (!File.Exists(mdPath))
    {
        Console.WriteLine($"SKIP: {mdFile} not found at {mdPath}");
        continue;
    }

    Console.WriteLine($"Parsing {mdFile}...");
    var plan = TestPlanParser.Parse(File.ReadAllLines(mdPath));

    var xlsxName = Path.ChangeExtension(mdFile, ".xlsx");
    var xlsxPath = Path.Combine(testPlanDir, xlsxName);

    var totalTc = plan.Sections.Sum(s => s.TestCases.Count);
    Console.WriteLine($"  {plan.Sections.Count} sections, {totalTc} test cases");

    Console.WriteLine($"Generating {xlsxName}...");
    ExcelGenerator.Generate(plan, xlsxPath);
    Console.WriteLine($"  -> {xlsxPath}");
}

Console.WriteLine("Done.");

// ── Models ──────────────────────────────────────────────────────────────────

record TestPlan(
    string Title,
    string Role,
    string AccessPolicy,
    string PrimaryPages,
    List<string> Prerequisites,
    List<TestSection> Sections);

record TestSection(string Name, string Route, List<TestCase> TestCases);

record TestCase(
    string Id,
    string Name,
    string SectionName,
    string? Precondition,
    List<string> Steps,
    List<string> ExpectedResults);

// ── Parser ──────────────────────────────────────────────────────────────────

static class TestPlanParser
{
    public static TestPlan Parse(string[] lines)
    {
        var title = "";
        var role = "";
        var accessPolicy = "";
        var primaryPages = "";
        var prerequisites = new List<string>();
        var sections = new List<TestSection>();

        var i = 0;

        // Title
        while (i < lines.Length)
        {
            var line = lines[i].Trim();
            if (line.StartsWith("# ") && !line.StartsWith("## "))
            {
                title = line[2..].Trim();
                i++;
                break;
            }
            i++;
        }

        // Role Description block
        while (i < lines.Length)
        {
            var line = lines[i].Trim();
            if (line.StartsWith("**Role:**"))
                role = line.Replace("**Role:**", "").Trim();
            else if (line.StartsWith("**Access Policy:**"))
                accessPolicy = line.Replace("**Access Policy:**", "").Trim();
            else if (line.StartsWith("**Primary Pages:**"))
                primaryPages = line.Replace("**Primary Pages:**", "").Trim();
            else if (line == "## Prerequisites")
            {
                i++;
                break;
            }
            i++;
        }

        // Prerequisites
        while (i < lines.Length)
        {
            var line = lines[i].Trim();
            if (line.StartsWith("---"))
            {
                i++;
                break;
            }
            if (line.StartsWith("- [ ]"))
                prerequisites.Add(line[5..].Trim());
            else if (line.StartsWith("- "))
                prerequisites.Add(line[2..].Trim());
            i++;
        }

        // Sections and Test Cases
        TestSection? currentSection = null;

        while (i < lines.Length)
        {
            var line = lines[i].Trim();

            // Section header: ## Section N: Name (route)
            if (Regex.IsMatch(line, @"^## Section \d+:"))
            {
                if (currentSection != null)
                    sections.Add(currentSection);

                var sectionMatch = Regex.Match(line, @"^## Section \d+:\s*(.+)$");
                var sectionFull = sectionMatch.Success ? sectionMatch.Groups[1].Value.Trim() : line[3..].Trim();

                // Extract route from backticks or parentheses
                var routeMatch = Regex.Match(sectionFull, @"\(`([^`]+)`\)");
                var route = routeMatch.Success ? routeMatch.Groups[1].Value : "";
                var sectionName = routeMatch.Success
                    ? sectionFull[..sectionFull.IndexOf('(')].Trim()
                    : sectionFull;

                currentSection = new TestSection(sectionName, route, new List<TestCase>());
                i++;
                continue;
            }

            // Quick Pass/Fail section - stop parsing
            if (line.StartsWith("## Quick Pass/Fail"))
                break;

            // Test Case header: ### TC-X.Y: Name
            if (Regex.IsMatch(line, @"^### TC-\d+\.\d+:"))
            {
                var tcMatch = Regex.Match(line, @"^### (TC-\d+\.\d+):\s*(.+)$");
                if (tcMatch.Success && currentSection != null)
                {
                    var tcId = tcMatch.Groups[1].Value;
                    var tcName = tcMatch.Groups[2].Value.Trim();
                    i++;

                    string? precondition = null;
                    var steps = new List<string>();
                    var expected = new List<string>();

                    // Parse test case body
                    var parsingMode = "none"; // precondition, steps, expected, setup
                    while (i < lines.Length)
                    {
                        var bodyLine = lines[i].Trim();

                        // Stop at next TC or section
                        if (bodyLine.StartsWith("### TC-") || bodyLine.StartsWith("## "))
                            break;
                        if (bodyLine.StartsWith("---"))
                        {
                            i++;
                            break;
                        }

                        if (bodyLine.StartsWith("**Precondition:**"))
                        {
                            precondition = bodyLine.Replace("**Precondition:**", "").Trim();
                            parsingMode = "none";
                            i++;
                            continue;
                        }
                        if (bodyLine.StartsWith("**Setup:**"))
                        {
                            precondition = bodyLine.Replace("**Setup:**", "").Trim();
                            parsingMode = "none";
                            i++;
                            continue;
                        }
                        if (bodyLine == "**Steps:**")
                        {
                            parsingMode = "steps";
                            i++;
                            continue;
                        }
                        if (bodyLine == "**Expected:**")
                        {
                            parsingMode = "expected";
                            i++;
                            continue;
                        }

                        if (parsingMode == "steps")
                        {
                            if (Regex.IsMatch(bodyLine, @"^\d+\."))
                            {
                                steps.Add(bodyLine);
                            }
                            else if (bodyLine.StartsWith("- ") || bodyLine.StartsWith("  "))
                            {
                                // Sub-step or continuation
                                if (steps.Count > 0)
                                    steps[^1] += "\n" + bodyLine;
                                else
                                    steps.Add(bodyLine);
                            }
                        }
                        else if (parsingMode == "expected")
                        {
                            if (bodyLine.StartsWith("- "))
                                expected.Add(bodyLine[2..].Trim());
                            else if (bodyLine.StartsWith("  ") && expected.Count > 0)
                                expected[^1] += "\n" + bodyLine.Trim();
                        }

                        i++;
                    }

                    currentSection.TestCases.Add(new TestCase(
                        tcId, tcName, currentSection.Name, precondition, steps, expected));
                    continue;
                }
            }

            i++;
        }

        if (currentSection != null)
            sections.Add(currentSection);

        return new TestPlan(title, role, accessPolicy, primaryPages, prerequisites, sections);
    }
}

// ── Excel Generator ─────────────────────────────────────────────────────────

static class ExcelGenerator
{
    public static void Generate(TestPlan plan, string outputPath)
    {
        using var wb = new XLWorkbook();

        CreateSummarySheet(wb, plan);
        CreateTestCasesSheet(wb, plan);

        wb.SaveAs(outputPath);
    }

    static void CreateSummarySheet(XLWorkbook wb, TestPlan plan)
    {
        var ws = wb.Worksheets.Add("Summary");

        // Title
        ws.Cell(1, 1).Value = plan.Title;
        ws.Range(1, 1, 1, 7).Merge();
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 16;
        ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        // Metadata
        var row = 3;
        SetLabelValue(ws, row, "Role:", plan.Role); row++;
        SetLabelValue(ws, row, "Access Policy:", plan.AccessPolicy); row++;
        SetLabelValue(ws, row, "Primary Pages:", plan.PrimaryPages); row++;

        // Prerequisites
        row += 1;
        ws.Cell(row, 1).Value = "Prerequisites";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 12;
        row++;

        foreach (var prereq in plan.Prerequisites)
        {
            ws.Cell(row, 1).Value = "\u2610"; // checkbox unicode
            ws.Cell(row, 2).Value = prereq;
            ws.Range(row, 2, row, 7).Merge();
            row++;
        }

        // Section progress table
        row += 1;
        ws.Cell(row, 1).Value = "Section Progress";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 12;
        row++;

        var headerRow = row;
        var headers = new[] { "Section", "Total", "Passed", "Failed", "Blocked", "Not Tested", "% Complete" };
        for (var c = 0; c < headers.Length; c++)
        {
            ws.Cell(row, c + 1).Value = headers[c];
            ws.Cell(row, c + 1).Style.Font.Bold = true;
            ws.Cell(row, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#206bc4");
            ws.Cell(row, c + 1).Style.Font.FontColor = XLColor.White;
            ws.Cell(row, c + 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }
        row++;

        var tcSheet = "Test Cases";
        var firstDataRow = row;
        foreach (var section in plan.Sections)
        {
            var tcCount = section.TestCases.Count;
            ws.Cell(row, 1).Value = section.Name;
            ws.Cell(row, 2).Value = tcCount;

            // COUNTIF formulas referencing Test Cases sheet Status column (G)
            // Find row range for this section in the test cases sheet
            // We'll use section name matching in COUNTIFS
            var passFormula = $"COUNTIFS('{tcSheet}'!B:B,\"{EscapeForFormula(section.Name)}\",'{tcSheet}'!G:G,\"Pass\")";
            var failFormula = $"COUNTIFS('{tcSheet}'!B:B,\"{EscapeForFormula(section.Name)}\",'{tcSheet}'!G:G,\"Fail\")";
            var blockedFormula = $"COUNTIFS('{tcSheet}'!B:B,\"{EscapeForFormula(section.Name)}\",'{tcSheet}'!G:G,\"Blocked\")";
            var notTestedFormula = $"COUNTIFS('{tcSheet}'!B:B,\"{EscapeForFormula(section.Name)}\",'{tcSheet}'!G:G,\"Not Tested\")";

            ws.Cell(row, 3).FormulaA1 = passFormula;
            ws.Cell(row, 4).FormulaA1 = failFormula;
            ws.Cell(row, 5).FormulaA1 = blockedFormula;
            ws.Cell(row, 6).FormulaA1 = notTestedFormula;

            // % Complete = (Pass + Fail + Blocked + Skipped) / Total
            var totalCell = ws.Cell(row, 2).Address;
            var pctFormula = $"IF({totalCell}=0,0,(COUNTIFS('{tcSheet}'!B:B,\"{EscapeForFormula(section.Name)}\",'{tcSheet}'!G:G,\"<>Not Tested\")/{totalCell}))";
            ws.Cell(row, 7).FormulaA1 = pctFormula;
            ws.Cell(row, 7).Style.NumberFormat.Format = "0%";

            // Alternating row color
            if ((row - firstDataRow) % 2 == 1)
            {
                for (var c = 1; c <= 7; c++)
                    ws.Cell(row, c).Style.Fill.BackgroundColor = XLColor.FromHtml("#f4f6fa");
            }
            for (var c = 1; c <= 7; c++)
                ws.Cell(row, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            row++;
        }

        // Totals row
        ws.Cell(row, 1).Value = "TOTAL";
        ws.Cell(row, 1).Style.Font.Bold = true;
        var lastDataRow = row - 1;
        for (var c = 2; c <= 6; c++)
        {
            ws.Cell(row, c).FormulaA1 = $"SUM({ws.Cell(firstDataRow, c).Address}:{ws.Cell(lastDataRow, c).Address})";
            ws.Cell(row, c).Style.Font.Bold = true;
        }
        ws.Cell(row, 7).FormulaA1 = $"IF(B{row}=0,0,(B{row}-F{row})/B{row})";
        ws.Cell(row, 7).Style.NumberFormat.Format = "0%";
        ws.Cell(row, 7).Style.Font.Bold = true;

        for (var c = 1; c <= 7; c++)
        {
            ws.Cell(row, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Cell(row, c).Style.Fill.BackgroundColor = XLColor.FromHtml("#e6ecf5");
        }

        // Conditional formatting on % Complete column
        var pctRange = ws.Range(firstDataRow, 7, row, 7);
        pctRange.AddConditionalFormat().WhenEqualOrGreaterThan(0.8).Fill.BackgroundColor = XLColor.FromHtml("#d5f0d5");
        pctRange.AddConditionalFormat().WhenBetween(0.5, 0.79).Fill.BackgroundColor = XLColor.FromHtml("#fff3cd");
        pctRange.AddConditionalFormat().WhenLessThan(0.5).Fill.BackgroundColor = XLColor.FromHtml("#f8d7da");

        // Column widths
        ws.Column(1).Width = 40;
        ws.Column(2).Width = 10;
        ws.Column(3).Width = 10;
        ws.Column(4).Width = 10;
        ws.Column(5).Width = 10;
        ws.Column(6).Width = 12;
        ws.Column(7).Width = 12;

        ws.SheetView.FreezeRows(headerRow);
    }

    static void CreateTestCasesSheet(XLWorkbook wb, TestPlan plan)
    {
        var ws = wb.Worksheets.Add("Test Cases");

        // Headers
        var headers = new[] { "TC ID", "Section", "Test Case", "Precondition", "Steps", "Expected Result", "Status", "Tester", "Test Date", "Notes", "Priority" };
        var widths = new[] { 10, 30, 40, 40, 60, 60, 14, 15, 12, 40, 10 };

        for (var c = 0; c < headers.Length; c++)
        {
            ws.Cell(1, c + 1).Value = headers[c];
            ws.Cell(1, c + 1).Style.Font.Bold = true;
            ws.Cell(1, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#206bc4");
            ws.Cell(1, c + 1).Style.Font.FontColor = XLColor.White;
            ws.Cell(1, c + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(1, c + 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Column(c + 1).Width = widths[c];
        }

        var row = 2;
        foreach (var section in plan.Sections)
        {
            var isEdgeSection = section.Name.Contains("Edge Case", StringComparison.OrdinalIgnoreCase)
                || section.Name.Contains("Error", StringComparison.OrdinalIgnoreCase);

            foreach (var tc in section.TestCases)
            {
                ws.Cell(row, 1).Value = tc.Id;
                ws.Cell(row, 2).Value = tc.SectionName;
                ws.Cell(row, 3).Value = tc.Name;
                ws.Cell(row, 4).Value = tc.Precondition ?? "";
                ws.Cell(row, 5).Value = string.Join("\n", tc.Steps);
                ws.Cell(row, 6).Value = string.Join("\n", tc.ExpectedResults);
                ws.Cell(row, 7).Value = "Not Tested";
                ws.Cell(row, 8).Value = "";
                ws.Cell(row, 9).Value = "";
                ws.Cell(row, 9).Style.NumberFormat.Format = "yyyy-mm-dd";
                ws.Cell(row, 10).Value = "";
                ws.Cell(row, 11).Value = isEdgeSection ? "Edge" : "Core";

                // Word wrap for Steps and Expected
                ws.Cell(row, 4).Style.Alignment.WrapText = true;
                ws.Cell(row, 5).Style.Alignment.WrapText = true;
                ws.Cell(row, 6).Style.Alignment.WrapText = true;

                // Top alignment
                for (var c = 1; c <= 11; c++)
                {
                    ws.Cell(row, c).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                    ws.Cell(row, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    ws.Cell(row, c).Style.Border.OutsideBorderColor = XLColor.FromHtml("#dee2e6");
                }

                // Row banding
                if ((row - 2) % 2 == 1)
                {
                    for (var c = 1; c <= 11; c++)
                        ws.Cell(row, c).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8f9fc");
                }

                row++;
            }
        }

        var lastRow = row - 1;

        // Data validation on Status column (G)
        var statusRange = ws.Range(2, 7, lastRow, 7);
        statusRange.CreateDataValidation().List("\"Not Tested,Pass,Fail,Blocked,Skipped\"");

        // Conditional formatting on Status column
        var statusCf = ws.Range(2, 7, lastRow, 7);
        var passStyle = statusCf.AddConditionalFormat().WhenEquals("\"Pass\"");
        passStyle.Fill.BackgroundColor = XLColor.FromHtml("#d5f0d5");
        passStyle.Font.FontColor = XLColor.FromHtml("#1a7a1a");

        var failStyle = statusCf.AddConditionalFormat().WhenEquals("\"Fail\"");
        failStyle.Fill.BackgroundColor = XLColor.FromHtml("#f8d7da");
        failStyle.Font.FontColor = XLColor.FromHtml("#842029");

        var blockedStyle = statusCf.AddConditionalFormat().WhenEquals("\"Blocked\"");
        blockedStyle.Fill.BackgroundColor = XLColor.FromHtml("#fff3cd");
        blockedStyle.Font.FontColor = XLColor.FromHtml("#664d03");

        var skippedStyle = statusCf.AddConditionalFormat().WhenEquals("\"Skipped\"");
        skippedStyle.Fill.BackgroundColor = XLColor.FromHtml("#e2e3e5");
        skippedStyle.Font.FontColor = XLColor.FromHtml("#41464b");

        // Priority column conditional formatting
        var priorityRange = ws.Range(2, 11, lastRow, 11);
        priorityRange.AddConditionalFormat().WhenEquals("\"Core\"").Fill.BackgroundColor = XLColor.FromHtml("#cfe2ff");
        priorityRange.AddConditionalFormat().WhenEquals("\"Edge\"").Fill.BackgroundColor = XLColor.FromHtml("#e2e3e5");

        // Freeze header row + auto-filter
        ws.SheetView.FreezeRows(1);
        ws.RangeUsed()?.SetAutoFilter();
    }

    static void SetLabelValue(IXLWorksheet ws, int row, string label, string value)
    {
        ws.Cell(row, 1).Value = label;
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = value;
        ws.Range(row, 2, row, 7).Merge();
    }

    static string EscapeForFormula(string value)
    {
        return value.Replace("\"", "\"\"");
    }
}
