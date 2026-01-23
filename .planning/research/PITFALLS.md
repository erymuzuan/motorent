# Domain Pitfalls

**Domain:** Document Template Editor for Rental SaaS
**Researched:** 2026-01-23
**Confidence:** MEDIUM (based on training data patterns, existing codebase analysis, and domain knowledge)

## Critical Pitfalls

Mistakes that cause rewrites or major issues.

---

### Pitfall 1: Blazor/JavaScript State Synchronization in Drag-and-Drop

**What goes wrong:** The Blazor component state and JavaScript DOM state diverge during drag operations. User drags an element, JS updates positions, but Blazor doesn't know. Or worse: Blazor re-renders mid-drag and loses the drag operation entirely.

**Why it happens:**
- Blazor Server re-renders on SignalR events, can interrupt JS operations
- JS interop is async - race conditions between Blazor state updates and JS DOM manipulation
- Touch events on mobile behave differently than mouse events
- Browser-native drag-and-drop API has quirky behavior across browsers

**Consequences:**
- Elements "snap back" unexpectedly after drop
- Drag operations lost on slow connections (SignalR latency)
- Touch drag doesn't work on mobile PWA
- Undo/redo breaks because state was never captured

**Prevention:**
1. **Use JS as source of truth during drag** - Blazor only learns final position on drop
2. **Debounce position updates** - Don't sync every pixel movement to Blazor
3. **Implement drag state machine** - IDLE -> DRAGGING -> DROPPED, prevent re-renders during DRAGGING
4. **Use `@key` directives** - Prevent Blazor from recreating elements during re-render
5. **Test on SignalR with artificial latency** - Simulate slow mobile connections

**Detection (warning signs):**
- Elements flicker or jump during drag
- Drag works in dev but fails intermittently in production
- Touch users report drag "not working"
- Console shows "interop failed" during drag operations

**Which phase should address:** Phase 1 (Designer Core) - Must be solved before building element palette

---

### Pitfall 2: WYSIWYG Preview vs. PDF Rendering Mismatch

**What goes wrong:** The template looks perfect in the browser preview, but the generated PDF has different fonts, broken layouts, missing images, or wrong page breaks.

**Why it happens:**
- Browser CSS is pixel-based; PDF is point-based (1pt = 1/72 inch)
- Web fonts don't embed in PDF unless explicitly configured
- CSS Grid/Flexbox don't translate to PDF layout engines
- Browser print styles (@media print) differ from PDF generation
- Page break logic in PDF libraries differs from CSS `page-break-*`
- Thai text requires specific TrueType font files with Thai glyph support

**Consequences:**
- Users design templates that can't be reproduced in PDF
- Legal documents (rental agreements) look unprofessional
- Thai text renders as boxes or question marks
- Hours spent designing, result unusable

**Prevention:**
1. **Render preview using same engine as PDF** - Generate PDF first, show as preview
2. **Or use CSS-to-PDF library** (like Puppeteer) - Browser renders, PDF captures exactly
3. **Limit layout to PDF-safe patterns** - No CSS Grid in template elements, use absolute positioning
4. **Embed fonts explicitly** - Include Thai fonts (TH Sarabun, Noto Sans Thai) in PDF bundle
5. **Test page breaks early** - Multi-page templates in Phase 1, not as afterthought
6. **Show "PDF Preview" button** - Users see actual PDF before saving template

**Detection (warning signs):**
- "It looks different when I print" complaints
- Thai characters display as squares in PDF
- Images missing in PDF but visible in browser
- Content overflows page boundaries in PDF

**Which phase should address:** Phase 2 (PDF Rendering) - But preview architecture must be designed in Phase 1

---

### Pitfall 3: Over-Engineering the Element Model

**What goes wrong:** Building a generic "element tree" with deep nesting, multiple inheritance, plugin system, or extensible element types before understanding what users actually need.

**Why it happens:**
- Developer sees "similar to HTML/React" and builds DOM-like tree
- Premature abstraction: "What if they want tables inside tables inside columns?"
- Fear of "what if we need to add more element types later?"
- Copying enterprise document builder patterns without the same requirements

**Consequences:**
- Complex serialization/deserialization with JSON polymorphism edge cases
- Performance issues rendering deeply nested elements
- Users confused by too many options
- Months of work on features no one uses
- Maintenance burden for abstract base classes

**Prevention:**
1. **Start with flat list + containers only** - Elements at root level, only Container/TwoColumn can have children
2. **No inheritance beyond Element base** - Composition over inheritance
3. **Maximum 2 levels of nesting** - Template -> Container -> Elements (no deeper)
4. **Hardcode v1 element types** - Text, Image, TwoColumns, Divider, Signature, Date, Repeater - that's it
5. **Defer plugin/extension system** - If needed, add in v2 when usage patterns are clear
6. **JSON serialization without $type** - Use discriminated union pattern with explicit `Type` property

**Detection (warning signs):**
- Template JSON is deeply nested (3+ levels)
- "ElementFactory" or "ElementRegistry" classes appearing
- Abstract base classes with 5+ virtual methods
- Developers debating element inheritance hierarchies

**Which phase should address:** Phase 1 (Designer Core) - Define element model before building UI

---

### Pitfall 4: Thai Text Rendering Failures

**What goes wrong:** Thai text displays correctly in browser but fails in PDF: missing tone marks (mai ek, mai tho), incorrect vowel positioning, or complete character substitution (boxes/question marks).

**Why it happens:**
- Thai is a complex script with combining characters (vowels, tone marks above/below)
- Standard PDF fonts don't include Thai glyphs
- Font subsetting breaks combining character sequences
- PDF library may not support OpenType Thai shaping
- UTF-8 encoding issues in data binding

**Consequences:**
- Rental agreements legally questionable if text is unreadable
- Professional appearance destroyed for Thai market
- Users resort to English-only, limiting market appeal

**Prevention:**
1. **Use TH Sarabun PSK or Noto Sans Thai** - Free, comprehensive Thai glyphs
2. **Embed full font, not subset** - Subsetters often break Thai combining characters
3. **Test specific Thai patterns:** "กรุงเทพมหานคร", "ไม่มี", "เงินฝาก" - cover all vowel/tone positions
4. **Use PDF library with OpenType shaping** - QuestPDF, iText7 (with Asian font pack), Puppeteer
5. **Verify encoding chain** - Database (UTF-8) -> Entity (string) -> JSON (UTF-8) -> PDF (embedded font)

**Detection (warning signs):**
- Thai tone marks (่ ้ ๊ ๋) appearing in wrong positions
- Vowels rendering after consonants instead of above/below
- Square boxes (tofu) in PDF output
- Different Thai text length in browser vs PDF

**Which phase should address:** Phase 2 (PDF Rendering) - Must validate Thai before declaring PDF complete

---

### Pitfall 5: Data Binding Null Reference Cascade

**What goes wrong:** Template references `{{Rental.Vehicle.Owner.Name}}` but Vehicle has no Owner. Entire document generation fails, or outputs "[null]" or crashes.

**Why it happens:**
- Deeply nested property chains without null checks at each level
- Optional relationships in data model (not every Rental has a Booking)
- Test data always complete, production data has gaps
- Template designer doesn't know which fields are optional

**Consequences:**
- Staff can't print agreement because one customer missing data
- Entire batch of receipts fails due to one bad record
- Users see cryptic error messages or blank documents
- Support burden for "why won't it print?"

**Prevention:**
1. **Null-safe property access in binding engine** - Return empty string for any null in chain
2. **Show field optionality in picker** - "Vehicle.Owner.Name (optional)" vs "Rental.RenterName (required)"
3. **Template validation on save** - Warn about bindings to optional chains
4. **Default values per field** - "[Not Specified]" rather than empty
5. **Graceful degradation** - Generate document with placeholders rather than failing entirely
6. **Test with minimal data** - Create test models with only required fields populated

**Detection (warning signs):**
- "Object reference not set" errors during rendering
- Documents print for some rentals but not others
- Template works in preview but fails with real data
- Empty spaces where data should appear

**Which phase should address:** Phase 3 (Data Binding) - Core requirement of binding engine

---

## Moderate Pitfalls

Mistakes that cause delays or technical debt.

---

### Pitfall 6: Template Storage Without Tenant Isolation Enforcement

**What goes wrong:** Template ID alone used for queries, allowing cross-tenant access if IDs guessable or URL manipulated.

**Prevention:**
1. **Always filter by AccountNo** - `WHERE AccountNo = @tenant AND TemplateId = @id`
2. **Don't expose raw TemplateId in URLs** - Use WebId (GUID) or Hashids
3. **Repository pattern enforces schema** - `[{AccountNo}].[DocumentTemplate]` automatic per existing pattern
4. **Unit test tenant isolation** - Attempt cross-tenant access, verify 404/403

**Which phase should address:** Phase 4 (Template Management) - Storage layer design

---

### Pitfall 7: Print Workflow Breaking for Staff

**What goes wrong:** New template system requires staff to learn new flow. Existing print buttons stop working. Staff under time pressure abandon new feature.

**Prevention:**
1. **Drop-in replacement first** - Enhanced print button, same location, same primary action
2. **Default template auto-selected** - Staff clicks Print, gets default, done
3. **Optional template selection** - Dropdown secondary to primary print action
4. **Parallel operation period** - Old hardcoded templates still available during rollout
5. **Staff testing before launch** - Validate with actual rental desk users

**Which phase should address:** Phase 6 (Integration) - Critical for adoption

---

### Pitfall 8: Canvas Positioning Without Pixel/Point Conversion

**What goes wrong:** Designer canvas uses CSS pixels (1px), PDF uses points (1pt = 1.333px at 96dpi). Element positions don't match between preview and output.

**Prevention:**
1. **Internal model uses points** - Canvas scales to pixels for display only
2. **Explicit DPI configuration** - 72dpi for PDF-native, 96dpi for web preview
3. **Round-trip test** - Save template, load, verify positions unchanged
4. **Display measurements in real units** - "1 inch", "25mm", not "96px"

**Which phase should address:** Phase 1 (Designer Core) - Coordinate system design

---

### Pitfall 9: Repeater Performance with Large Collections

**What goes wrong:** Template has repeater for line items. Invoice with 200 items takes 30 seconds to render or times out.

**Prevention:**
1. **Limit repeater item count** - Cap at 50 items, paginate larger collections
2. **Lazy render in preview** - Show first 10, "[... and 190 more items]"
3. **Background PDF generation** - Generate async, notify when ready for large documents
4. **Virtualization in designer preview** - Don't render off-screen repeater items

**Which phase should address:** Phase 3 (Data Binding) or Phase 5 (Repeater Elements)

---

### Pitfall 10: Image Embedding Without Size Limits

**What goes wrong:** User uploads 5MB logo, template JSON becomes 7MB. Every load/save takes 10+ seconds. PDF generation fails on memory limits.

**Prevention:**
1. **Max image size 500KB** - Compress on upload
2. **Store images as references (S3 URL)** - Don't embed base64 in template JSON
3. **Image dimensions limit** - Max 2000x2000px, resize on upload
4. **Lazy image loading in designer** - Show placeholder until scrolled into view

**Which phase should address:** Phase 1 (Designer Core) - Image element design

---

## Minor Pitfalls

Mistakes that cause annoyance but are fixable.

---

### Pitfall 11: Undo/Redo Stack Memory Leak

**What goes wrong:** Every change stores full template state. After 50 edits, browser consuming 500MB memory.

**Prevention:**
1. **Store diffs, not full state** - Calculate delta between states
2. **Limit undo depth** - Max 20 operations
3. **Compress undo stack** - Coalesce rapid position changes into single entry

**Which phase should address:** Phase 1 (Designer Core)

---

### Pitfall 12: Mobile Touch Designer Unusable

**What goes wrong:** Designer built for mouse. Touch targets too small, pinch-zoom conflicts with element resize.

**Prevention:**
1. **Minimum touch target 44x44px** - Apple HIG guideline
2. **Separate resize handles from drag** - Long-press vs tap
3. **Test on actual tablet** - iPad/Android tablet, not just Chrome DevTools

**Which phase should address:** Phase 1 (Designer Core) - If mobile OrgAdmin use case exists

---

### Pitfall 13: Localization Hardcoded in Element Labels

**What goes wrong:** Element labels like "Two Columns" hardcoded in English. Thai OrgAdmins confused by designer UI.

**Prevention:**
1. **All UI strings in resx files** - Per existing MotoRent pattern
2. **Element type enum** - Localize display name, not internal identifier
3. **Property labels localized** - "Width" -> "ความกว้าง"

**Which phase should address:** Phase 1 (Designer Core) - Follow existing LocalizedComponentBase pattern

---

### Pitfall 14: Missing Template Preview Data

**What goes wrong:** Template preview shows "{{Rental.StartDate}}" text instead of actual sample data.

**Prevention:**
1. **Sample data models** - AgreementModel.CreateSample() with realistic fake data
2. **Use Thai names in samples** - "สมชาย ใจดี", not "John Smith"
3. **Toggle mode** - "Show placeholders" vs "Show sample data"

**Which phase should address:** Phase 3 (Data Binding)

---

### Pitfall 15: No Template Validation Before Save

**What goes wrong:** User saves template with broken bindings, discovers problem when trying to print real document.

**Prevention:**
1. **Validate on save** - Check all bindings resolve
2. **Warning vs error** - Warn for optional fields, error for required
3. **Generate preview as validation** - If preview fails, save blocked

**Which phase should address:** Phase 4 (Template Management)

---

## Phase-Specific Warnings

| Phase Topic | Likely Pitfall | Mitigation |
|-------------|----------------|------------|
| Designer Core | State sync (Pitfall 1) | JS source of truth during drag, Blazor on drop |
| Designer Core | Over-engineering (Pitfall 3) | Flat element list, max 2 nesting levels |
| Designer Core | Canvas coordinates (Pitfall 8) | Use points internally, scale to pixels for display |
| PDF Rendering | WYSIWYG mismatch (Pitfall 2) | Same engine for preview and output, or Puppeteer |
| PDF Rendering | Thai text (Pitfall 4) | TH Sarabun font embedded, test combining characters |
| Data Binding | Null cascade (Pitfall 5) | Null-safe property access, graceful degradation |
| Data Binding | Repeater performance (Pitfall 9) | Limit item count, background generation for large docs |
| Template Management | Tenant isolation (Pitfall 6) | AccountNo in all queries, repository pattern |
| Integration | Staff workflow (Pitfall 7) | Drop-in replacement, default template auto-select |
| Default Templates | Missing preview data (Pitfall 14) | Sample data models with Thai content |

---

## Summary for Roadmap

**Phase 1 must solve:**
- Drag-and-drop state synchronization (Critical)
- Element model simplicity (Critical)
- Coordinate system (points vs pixels)
- Touch targets if mobile supported

**Phase 2 must solve:**
- PDF-browser rendering parity (Critical)
- Thai font embedding (Critical)

**Phase 3 must solve:**
- Null-safe binding engine (Critical)
- Repeater performance limits

**Phase 4 must solve:**
- Tenant isolation enforcement
- Template validation

**Phase 6 must solve:**
- Staff workflow preservation (adoption blocker)

---

*Research based on: Existing MotoRent codebase patterns, document editor domain knowledge, Blazor Server JS interop patterns, PDF generation domain knowledge, Thai typography requirements. Confidence: MEDIUM - limited external verification available.*
