# Implementation Plan: Document Template Designer

This plan outlines the steps for implementing the visual document template designer, including storage, rendering, and UI integration, following the established repository patterns and coding standards.

## Phase 1: Domain Model & Data Access [checkpoint: pending]

### Task 1: Define DocumentTemplate Domain Model (PENDING)
- [ ] Task: Define the `DocumentTemplate` entity and associated enums.
    - [ ] Write unit tests for `DocumentTemplate` entity validation and state transitions (Draft/Approved/Default).
    - [ ] Implement `DocumentTemplate` domain model (including `DocumentType` enum: Booking, Rental, Receipt).
    - [ ] Ensure compliance with `code-standards` for naming and structure.
    - [ ] Define the JSON structure for the section-based layout.

### Task 2: SQL Metadata & Binary Storage Integration (PENDING)
- [ ] Task: Configure metadata storage using `SqlJsonRepository` and binary content in `IBinaryStore`.
    - [ ] Write unit tests for saving/retrieving template metadata using the repository and binary content via `IBinaryStore`.
    - [ ] Register `DocumentTemplate` with the `DataContext` following the `database-repository` pattern.
    - [ ] Implement service logic to store/fetch layout JSON from `IBinaryStore` using `StoreId`, while keeping metadata in SQL.

- [ ] Task: Conductor - User Manual Verification 'Phase 1: Domain Model & Data Access' (Protocol in workflow.md)

## Phase 2: Core Rendering Engine (QuestPDF & HTML) [checkpoint: pending]

### Task 1: Implement Dynamic Data Resolver (PENDING)
- [ ] Task: Create a service to map domain entities (Booking, Rental, Receipt) to template tokens.
    - [ ] Write unit tests for data resolution with various input models.
    - [ ] Implement `ITemplateDataResolver` to provide shared context (Org/Staff) and entity-specific data.

### Task 2: QuestPDF Rendering Logic (PENDING)
- [ ] Task: Implement the PDF generation engine using QuestPDF.
    - [ ] Write unit tests for translating section-based JSON to QuestPDF components.
    - [ ] Implement `IQuestPdfGenerator` that parses layout JSON and renders a PDF stream.

### Task 3: HTML Rendering Logic (PENDING)
- [ ] Task: Implement the HTML generation for web views.
    - [ ] Write unit tests for HTML rendering from layout JSON.
    - [ ] Implement `IHtmlTemplateRenderer` to produce responsive HTML.

- [ ] Task: Conductor - User Manual Verification 'Phase 2: Core Rendering Engine (QuestPDF & HTML)' (Protocol in workflow.md)

## Phase 3: Visual Designer UI & Component Library [checkpoint: pending]

### Task 1: Designer Backend API (PENDING)
- [ ] Task: Create API endpoints for template management and preview generation.
    - [ ] Write unit tests for the Template API Controller.
    - [ ] Implement CRUD endpoints (using `IRepository<DocumentTemplate>`) and a "Preview" endpoint (PDF/HTML).

### Task 2: Blazor Drag-and-Drop Designer (PENDING)
- [ ] Task: Build the visual designer interface.
    - [ ] Implement a section-based drag-and-drop UI following `blazor-development` and `css-styling` patterns.
    - [ ] Create a library of draggable "blocks" (Header, Text, Table, Image, Footer).
    - [ ] Implement property editors for each block (font size, alignment, data binding) using localized labels.

### Task 3: Live Preview Component (PENDING)
- [ ] Task: Create a dual-pane preview for HTML and PDF.
    - [ ] Implement a toggleable preview window within the designer using `dialog-pattern` if appropriate.
    - [ ] Integrate the PDF viewer for the QuestPDF output.

- [ ] Task: Conductor - User Manual Verification 'Phase 3: Visual Designer UI & Component Library' (Protocol in workflow.md)

## Phase 4: Workflow Integration & Default Templates [checkpoint: pending]

### Task 1: Organization Settings Integration (PENDING)
- [ ] Task: Add the Template Management UI to the Organization Settings.
    - [ ] Implement the template list view with status management (Set as Default, Approve).
    - [ ] Restrict access to Org Admins.

### Task 2: Enhanced Print Workflow (PENDING)
- [ ] Task: Update the "Print" button on Booking, Rental, and Receipt pages.
    - [ ] Implement logic to fetch the "Default" template for the current type.
    - [ ] Add a dropdown menu to the Print button for selecting alternative "Approved" templates.

### Task 3: Seed Default "Out-of-the-Box" Templates (PENDING)
- [ ] Task: Create and seed professional default templates.
    - [ ] Design standard layouts for Booking Confirmation, Rental Agreement, and Receipt.
    - [ ] Implement a seeding mechanism to populate new organizations with these defaults.

- [ ] Task: Conductor - User Manual Verification 'Phase 4: Workflow Integration & Default Templates' (Protocol in workflow.md)
