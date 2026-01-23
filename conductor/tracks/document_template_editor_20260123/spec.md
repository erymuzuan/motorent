# Specification: Document Template Designer

## Overview
This feature introduces a visual, drag-and-drop template designer within the MotoRent system. It allows administrators to create and customize templates for three core document types: Booking Confirmations, Rental Agreements, and Receipts. The designer will support dynamic data binding, section-based layouts, and dual-format output (HTML and PDF).

## Functional Requirements
- **Visual Designer:**
    - Drag-and-drop interface for building document layouts.
    - Section-based grid system (e.g., Header, Customer Info, Vehicle Details, Pricing Table, Footer).
    - Live preview toggle between HTML (web view) and PDF (QuestPDF rendering).
- **Document Types & Data Binding:**
    - **Booking Confirmation:** Primary model `Booking`.
    - **Rental Agreement:** Primary model `Rental`.
    - **Receipt:** Primary model `Payment/Receipt`.
    - **Shared Context:** Access to `Organization` branding (logo, contact info) and `Staff` details.
- **Template Management:**
    - **Admin UI:** Template management menu is located within **Organization Settings**.
    - **Storage:** Actual template content (JSON/HTML) is stored in **IBinaryStore**.
    - **Metadata:** Database stores template metadata, versioning records, `StoreId`, and status (Draft/Approved/Default).
    - **Out-of-the-box Templates:** Provide several professional, pre-defined templates for each document type that can be used immediately or as a starting point for customization.
    - **Versioning:** Historical documents retain the template version used at the time of creation.
- **Printing Workflow:**
    - Staff use the standard "Print" button on relevant entities.
    - **Default Template:** Clicking the button directly prints using the organization's designated "Default" template for that document type.
    - **Template Selection:** The print button includes a dropdown (if multiple approved templates exist) allowing staff to select from other "Approved" templates.
- **Output Generation:**
    - High-fidelity PDF generation using **QuestPDF**.
    - Responsive HTML rendering for in-browser viewing.

## Non-Functional Requirements
- **Performance:** PDF generation should be efficient to avoid blocking the UI thread.
- **Extensibility:** The component-based designer should allow for adding new "blocks" or document types in the future.
- **Consistency:** Ensure 1:1 parity (as close as possible) between the designer preview and the final generated outputs.

## Acceptance Criteria
- [ ] Org Admins can access the Template Designer from Organization Settings.
- [ ] Users can drag sections into a template and bind fields to dynamic data (e.g., `{{Customer.Name}}`).
- [ ] Saving a template persists the layout to `IBinaryStore` and updates the database reference.
- [ ] Several professional default templates are available for immediate use for all document types.
- [ ] Staff can print a document using the "Default" template with one click.
- [ ] Staff can select alternative "Approved" templates from a dropdown on the print button.
- [ ] Generating a PDF uses the customized template and QuestPDF engine.

## Out of Scope
- Support for complex custom CSS/JS injection by end-users.
- Third-party e-signature integration (to be handled in a separate track).
- Bulk printing/generation of historical documents.
