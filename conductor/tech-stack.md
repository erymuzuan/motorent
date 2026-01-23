# Technology Stack: MotoRent

## Backend
- **Core Framework:** .NET 10.0 (ASP.NET Core)
- **Architecture:** Clean Architecture with distinct projects for Domain, Services, Messaging, and Workers.
- **Messaging:** RabbitMQ for asynchronous processing and event-driven communication.
- **Utilities:** Hashids.net for generating short, unique, and non-sequential IDs from numbers (ideal for obfuscating database IDs in URLs).
- **Document Generation:** QuestPDF for high-fidelity, code-based PDF generation from template layouts.

## Frontend
- **Framework:** Blazor WebAssembly (hosted on the ASP.NET Core server).
- **Client Logic:** C# shared across both client and server, ensuring consistency in domain logic and data models.

## Database
- **Type:** Relational (SQL-based)
- **Schema Management:** Handled via custom SQL scripts and migrations.

## Development & Operations
- **Source Control:** Git
- **Project Structure:** Visual Studio Solution (.sln) with multi-project organization.
