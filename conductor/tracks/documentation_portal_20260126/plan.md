# Implementation Plan: MotoRent Documentation Portal (/learn)

This plan covers the audit of existing user guides, the creation of the static documentation portal, and the integration of Gemini-powered natural language search.

## Phase 1: Documentation Audit & Content Update [checkpoint: ]

### Task 1: Source Code Audit vs. Documentation (COMPLETED)
- [x] Task: Conduct a systematic review of the `src/` directory to identify feature/documentation gaps.
    - [x] Compare `user.guides/` content with current implementation of: Rental Workflow, Document Templates, OCR, and Multi-tenant settings.
    - [x] Identify outdated screenshots or procedural steps.
    - [x] Create a list of required updates for each `.md` file.

### Task 2: Update and Refine User Guides (COMPLETED) f59d3de
- [x] Task: Update the Markdown files in `user.guides/` and `USER_GUIDES/` to reflect current system state. f59d3de
    - [x] Update `01-orgadmin-quickstart.md`, `02-staff-quickstart.md`, etc.
    - [x] Ensure consistent formatting and categorization tags in front-matter (if applicable).
    - [x] Add missing sections for recently implemented features (e.g., Document Template Designer).

- [x] Task: Conductor - User Manual Verification 'Phase 1: Documentation Audit & Content Update' (Protocol in workflow.md) f59d3de

## Phase 2: Markdown Rendering Engine & Portal UI [checkpoint: f59d3de]

### Task 1: Markdown Retrieval and Rendering Logic
- [ ] Task: Implement a service to fetch and render `.md` files in Blazor.
    - [ ] Write unit tests for the `MarkdownService` (fetching from `wwwroot` and rendering via Markdig).
    - [ ] Configure the build script/project file to copy `user.guides/*.md` to `wwwroot/user.guides/`.
    - [ ] Create a `manifest.json` generation task to list available documentation for the sidebar.

### Task 2: Documentation Portal Layout (/learn)
- [ ] Task: Build the Blazor UI for the documentation portal.
    - [ ] Write unit tests for the documentation navigation component.
    - [ ] Implement the `Learn.razor` page that dynamically loads `.md` content based on the URL route.
    - [ ] Implement responsive CSS to ensure readability on mobile.
    - [ ] Add breadcrumbs and "Next/Previous" navigation buttons.

- [ ] Task: Conductor - User Manual Verification 'Phase 2: Static Rendering Engine & Portal UI' (Protocol in workflow.md)

## Phase 3: Gemini Search & Chat Integration [checkpoint: ]

### Task 1: Knowledge Base Indexing for RAG
- [ ] Task: Prepare the documentation content for Gemini context injection.
    - [ ] Write unit tests for the document chunking and indexing logic.
    - [ ] Implement a service to flatten documentation into searchable text chunks suitable for Gemini prompts.

### Task 2: Natural Language Search & AI Chat
- [ ] Task: Implement the search bar and chat interface.
    - [ ] Write unit tests for the `DocumentationSearchService` (mocking Gemini API).
    - [ ] Implement a search bar that sends queries to Gemini for natural language processing.
    - [ ] Build the `DocumentationChat` component for interactive Q&A.
    - [ ] Implement logic to display "Source Links" alongside AI responses.

- [ ] Task: Conductor - User Manual Verification 'Phase 3: Gemini Search & Chat Integration' (Protocol in workflow.md)

## Phase 4: Final Integration & SEO [checkpoint: ]

### Task 1: Navigation & Sitemap Integration
- [ ] Task: Link the `/learn` page to the main navigation (footer/header).
    - [ ] Update `NavMenu.razor` or the public landing page to include "Learn".
    - [ ] Ensure SEO meta tags (title, description) are dynamically set for each documentation page.

- [ ] Task: Conductor - User Manual Verification 'Phase 4: Final Integration & SEO' (Protocol in workflow.md)
