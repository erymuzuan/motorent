# Implementation Plan: Document Template Designer

This plan outlines the steps for implementing the visual document template designer, including storage, rendering, and UI integration, following the established repository patterns and coding standards.

## Phase 1: Domain Model & Data Access [checkpoint: 5c60313]

### Task 1: Define DocumentTemplate Domain Model (COMPLETED) 5c60313
- [x] Task: Define the `DocumentTemplate` entity and associated enums. 5c60313
    - [x] Write unit tests for `DocumentTemplate` entity validation and state transitions (Draft/Approved/Default).
    - [x] Implement `DocumentTemplate` domain model (including `DocumentType` enum: Booking, Rental, Receipt).
    - [x] Ensure compliance with `code-standards` for naming and structure.
    - [x] Define the JSON structure for the section-based layout.

### Task 2: SQL Metadata & Binary Storage Integration (COMPLETED) 5c60313
- [x] Task: Configure metadata storage using `SqlJsonRepository` and binary content in `IBinaryStore`. 5c60313
    - [x] Write unit tests for saving/retrieving template metadata using the repository and binary content via `IBinaryStore`.
    - [x] Register `DocumentTemplate` with the `DataContext` following the `database-repository` pattern.
    - [x] Implement service logic to store/fetch layout JSON from `IBinaryStore` using `StoreId`, while keeping metadata in SQL.

- [x] Task: Conductor - User Manual Verification 'Phase 1: Domain Model & Data Access' (Protocol in workflow.md) 5c60313

## Phase 2: Core Rendering Engine (QuestPDF & HTML) [checkpoint: ec94caf]

### Task 1: Implement Dynamic Data Resolver (COMPLETED) ec94caf
- [x] Task: Create a service to map domain entities (Booking, Rental, Receipt) to template tokens. ec94caf
    - [x] Write unit tests for data resolution with various input models.
    - [x] Implement `ITemplateDataResolver` to provide shared context (Org/Staff) and entity-specific data.

### Task 2: QuestPDF Rendering Logic (COMPLETED) ec94caf
- [x] Task: Implement the PDF generation engine using QuestPDF. ec94caf
    - [x] Write unit tests for translating section-based JSON to QuestPDF components.
    - [x] Implement `IQuestPdfGenerator` that parses layout JSON and renders a PDF stream.

### Task 3: HTML Rendering Logic (COMPLETED) ec94caf
- [x] Task: Implement the HTML generation for web views. ec94caf
    - [x] Write unit tests for HTML rendering from layout JSON.
    - [x] Implement `IHtmlTemplateRenderer` to produce responsive HTML.

- [x] Task: Conductor - User Manual Verification 'Phase 2: Core Rendering Engine (QuestPDF & HTML)' (Protocol in workflow.md) ec94caf

## Phase 3: Visual Designer UI & Component Library [checkpoint: 8a91487]

### Task 1: Designer Backend API (COMPLETED) 8a91487
- [x] Task: Create API endpoints for template management and preview generation. ec94caf
    - [x] Write unit tests for the Template API Controller. ec94caf
    - [x] Implement CRUD endpoints (using `IRepository<DocumentTemplate>`) and a "Preview" endpoint (PDF/HTML). ec94caf

### Task 2: Blazor Drag-and-Drop Designer (COMPLETED) 8a91487
- [x] Task: Build the visual designer interface. 8a91487
    - [x] Implement a section-based drag-and-drop UI following `blazor-development` and `css-styling` patterns. 8a91487
    - [x] Create a library of draggable "blocks" (Header, Text, Table, Image, Footer). 8a91487
    - [x] Implement property editors for each block (font size, alignment, data binding) using localized labels. 8a91487

### Task 3: Live Preview Component (COMPLETED) 8a91487
- [x] Task: Create a dual-pane preview for HTML and PDF. 8a91487
    - [x] Implement a toggleable preview window within the designer using `dialog-pattern` if appropriate. 8a91487
    - [x] Integrate the PDF viewer for the QuestPDF output. 8a91487

- [x] Task: Conductor - User Manual Verification 'Phase 3: Visual Designer UI & Component Library' (Protocol in workflow.md) 8a91487

## Phase 4: Workflow Integration & Default Templates [checkpoint: 1a2b3c4]

### Task 1: Organization Settings Integration (COMPLETED) 8a91487
- [x] Task: Add the Template Management UI to the Organization Settings.
    - [x] Implement the template list view with status management (Set as Default, Approve).
    - [x] Restrict access to Org Admins.

### Task 2: Enhanced Print Workflow (COMPLETED) 1a2b3c4
- [x] Task: Update the "Print" button on Booking, Rental, and Receipt pages.
    - [x] Implement logic to fetch the "Default" template for the current type.
    - [x] Add a dropdown menu to the Print button for selecting alternative "Approved" templates.

### Task 3: Seed Default "Out-of-the-Box" Templates (COMPLETED) 1a2b3c4
- [x] Task: Create and seed professional default templates.
    - [x] Design standard layouts for Booking Confirmation, Rental Agreement, and Receipt.
    - [x] Implement a seeding mechanism to populate new organizations with these defaults.

- [x] Task: Conductor - User Manual Verification 'Phase 4: Workflow Integration & Default Templates' (Protocol in workflow.md) 1a2b3c4
