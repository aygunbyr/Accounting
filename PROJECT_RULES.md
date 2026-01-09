# Project Rules & Standards

This document defines the coding standards, architectural patterns, and best practices for the **Accounting** project. All contributions must adhere to these rules to maintain consistency and quality.

## 1. Architecture & Design
- **Structure**: Follow **Clean Architecture** principles.
  - `Domain`: Enterprise logic, Entities, Value Objects. No dependencies.
  - `Application`: Business logic, CQRS Handlers, Interfaces. Depends on `Domain`.
  - `Infrastructure`: Implementation of interfaces (Db, External Services). Depends on `Application`.
  - `Api`: Entry point, Controllers. Depends on `Application` and `Infrastructure`.
- **CQRS**: Use **MediatR** for all business operations.
  - **Commands**: Modify state. Return `Task<T>` or `Task`. Suffix: `Command`.
  - **Queries**: Read state. Return simple DTOs. Suffix: `Query`.
  - **Handlers**: Logic resides here. Suffix: `Handler`.

## 2. Coding Standards (C# 12)
- **File-scoped Namespaces**: Use `namespace Accounting.Application.Features;` (no indentation).
- **Primary Constructors**: Use primary constructors for dependency injection in classes (Handlers, Controllers).
  ```csharp
  // YES
  public class CreateInvoiceHandler(IAppDbContext db) : IRequestHandler<...> { ... }
  ```
- **Implicit Usings**: Enabled. Avoid cluttering files with common System imports.
- **DTOs**: Use `record` types for DTOs. Immutable by default.

## 3. Domain Patterns
- **Money Value Object**:
  - **NEVER** use raw `decimal` formatting manually.
  - Use `Accounting.Application.Common.Utils.Money` static helper.
  - `Money.R2(val)` / `Money.R4(val)` for rounding.
  - `Money.S2(val)` / `Money.S4(val)` for string output.
  - **Rounding Policy**: `MidpointRounding.AwayFromZero` (Example: 2.5 -> 3, -2.5 -> -3).
- **Entities**:
  - Keep entities **Rich** where possible (methods for logic), but public setters are currently permitted for practical CRUD simplification in this project.
  - **Soft Delete**: Entities implementing `ISoftDelete` must set `IsDeleted = true` instead of physical deletion.

## 4. Application Patterns
- **Database Access**:
  - Use `IAppDbContext` abstraction. Do not access `DbContext` direct methods not in the interface.
  - **AsNoTracking**: Use `.AsNoTracking()` for all Read/Query operations.
- **Exceptions**:
  - **NotFound**: throw `new Accounting.Application.Common.Errors.NotFoundException("EntityName", id)`. Does NOT throw `KeyNotFoundException`.
  - **Concurrency**: throw `new ConcurrencyConflictException(...)` when `RowVersion` mismatches.
  - **Validation**: handled by FluentValidation pipeline.
- **Pagination**:
  - Use `Accounting.Application.Common.Constants.PaginationConstants`.
  - Always normalize inputs:
    ```csharp
    var page = PaginationConstants.NormalizePage(request.Page);
    var size = PaginationConstants.NormalizePageSize(request.PageSize);
    ```
- **Concurrency Control**:
  - Use Optimistic Concurrency with `RowVersion` (byte[]).
  - Use the cross-platform retry pattern (not SQL locking hints).
  - In `Update` handlers, explicitly check `OriginalValue` of RowVersion.

## 5. API Rules
- **Response Format**: Methods return DTOs or `Unit`.
- **Status Codes**:
  - `200 OK`: Successful synchronous command/query.
  - `404 Not Found`: Entity missing (handled by middleware via `NotFoundException`).
  - `409 Conflict`: Concurrency or business rule violation.
  - `400 Bad Request`: Validation failure.

## 6. Specific Business Rules
- **Positive Values**: Financial values (Qty, Price, Total) in DB must ALWAYS be **POSITIVE**.
  - Direction (Refund/Return) is determined by `InvoiceType`, NOT by the sign of the value.
- **Stock Movement**: Linked to Invoices, but managed via Domain Events or Service orchestration (ensure consistency).

## 7. Migration & Database
- **Schema**: Use `SnakeCase` naming for tables/columns (or preserve existing convention if Pascal).
- **UTC**: All `DateTime` fields must be UTC (`DateTime.UtcNow`). suffix `AtUtc` (e.g., `CreatedAtUtc`).

## 8. Project Scope & Vision
- **Core Domain**: Pre-Accounting (Ön Muhasebe) and Stock Management.
- **Reference Model**: Features and UX should take inspiration from **"Mikro Paraşüt"** SaaS application.
- **Goal**: Provide a tailored, efficient backend that replaces Excel for SMEs (KOBİ), without over-engineering enterprise ERP features unless requested.
