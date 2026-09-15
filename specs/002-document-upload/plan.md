# Implementation Plan: Document Upload and Management

**Branch**: `002-document-upload` | **Date**: 2026-09-15 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/002-document-upload/spec.md`

## Summary

Add secure document upload, discovery, sharing, lifecycle management, and audit behavior to the existing ContosoDashboard Blazor Server application. The implementation extends the current EF Core model and scoped service layer with integer-keyed document metadata, local filesystem storage outside `wwwroot`, a fail-closed malware scanner boundary, service-level authorization, an authorized content route, existing notification integration, and a 12-month audit trail. Multi-file uploads are processed independently.

## Technical Context

**Language/Version**: C# on .NET 9.0, nullable reference types enabled  
**Primary Dependencies**: ASP.NET Core Blazor Server, Razor Pages, Entity Framework Core SQL Server 9.0.0, cookie authentication, existing scoped service layer  
**Storage**: SQL Server/LocalDB for metadata and audit events; configurable local filesystem root under `AppData/uploads`, outside `wwwroot`; no cloud storage dependency  
**Testing**: `dotnet build` plus focused service/UI smoke checks; no test project currently exists  
**Target Platform**: Windows development and local/intranet web hosting  
**Project Type**: Single-project web application  
**Performance Goals**: Upload valid files up to 25 MB within 30 seconds; list/search up to 500 documents within 2 seconds; PDF/image preview within 3 seconds  
**Constraints**: Offline-capable core workflow; fail closed if malware scanning is unavailable or unsafe; generated storage keys only; files outside `wwwroot`; preserve mock authentication; process files independently; retain audit events 12 months  
**Scale/Scope**: Existing employee dashboard; three prioritized user stories; project/task-linked documents; personal, project-team, and specific-user access; existing notification center

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Training-first scope: PASS.** The plan extends the existing `ContosoDashboard` project, remains local/offline-capable, introduces no cloud SDK, and documents the mock-authentication and scanner limitations.
- **Layered and cloud-ready architecture: PASS.** `DocumentService` owns business rules; `IFileStorageService` and `IMalwareScanner` isolate infrastructure; EF Core owns metadata; Razor components own presentation; `Program.cs` owns DI and the authorized content route.
- **Secure by default: PASS.** Files are outside `wwwroot`; storage keys are generated; service and content-route checks enforce access; scanner failure rejects uploads; persistence failures clean up files; audit events cover security-sensitive actions.
- **Spec-driven incremental delivery: PASS.** The plan maps the P1 upload slice, P2 discovery slice, and P3 sharing/lifecycle slice to concrete paths, contracts, and executable smoke checks. No automated test coverage is claimed where no test project exists.
- **Simplicity and traceability: PASS.** No second application, repository abstraction, or speculative cloud integration is introduced. The data model, contract, quickstart, and research decisions trace to the approved requirements.

**Post-design gate**: The design remains compliant. No complexity exception is required; `Complexity Tracking` is intentionally empty.

## Research Summary

See [research.md](research.md). Key decisions are:

1. Use the current `net9.0`/EF Core 9 project contract rather than stale .NET 8 documentation.
2. Add `Document`, `DocumentShare`, and `DocumentAuditEvent` entities with integer keys and explicit project/user relationships.
3. Use `IFileStorageService` with relative generated keys outside `wwwroot`.
4. Use `IMalwareScanner` and fail closed when no safe scan result exists.
5. Process each file independently and return per-file outcomes.
6. Reuse service-level authorization and existing notifications; expose only an authorized content route rather than a second API application.

## Project Structure

### Documentation (this feature)

```text
specs/002-document-upload/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── document-management.yaml
└── checklists/
    └── requirements.md
```

### Source Code (repository root)

```text
ContosoDashboard/
├── Data/
│   └── ApplicationDbContext.cs                 # Document DbSets, relationships, indexes
├── Models/
│   ├── Document.cs                             # Document metadata and lifecycle
│   ├── DocumentShare.cs                        # User/project-team grants
│   ├── DocumentAuditEvent.cs                   # 12-month audit records
│   ├── Notification.cs                         # Document notification types
│   ├── Project.cs                              # Existing project access surface
│   ├── ProjectMember.cs                        # Existing team membership
│   ├── TaskItem.cs                             # Existing task association
│   └── User.cs                                 # Existing ownership and roles
├── Pages/
│   ├── Documents.razor                         # Upload, list, search, filter, share actions
│   ├── ProjectDetails.razor                    # Project document section
│   ├── Tasks.razor                             # Task document entry point
│   └── Index.razor                             # Recent documents and count
├── Services/
│   ├── IDocumentService.cs                     # Document use-case contract
│   ├── DocumentService.cs                      # Validation, authorization, orchestration
│   ├── IFileStorageService.cs                  # Storage abstraction
│   ├── LocalFileStorageService.cs              # Local storage outside wwwroot
│   ├── IMalwareScanner.cs                      # Fail-closed scan abstraction
│   ├── ConfiguredMalwareScanner.cs             # Configured local scanner adapter
│   └── DocumentAuthorizationService.cs         # Shared resource checks
├── Shared/
│   └── NavMenu.razor                           # Document navigation
├── Program.cs                                  # DI and authorized content route
├── appsettings.json                            # Storage/scanner configuration
└── appsettings.Development.json                # Local training configuration
```

**Structure Decision**: Extend the existing single-project Blazor Server application. Keep document business logic in the service layer, persist relational metadata with EF Core, store file bytes through an infrastructure interface, and use `Program.cs` only for composition and the protected content-stream route.

## Data and Authorization Design

- `DocumentId`, `DocumentShareId`, and `DocumentAuditEventId` are identity `int` keys.
- `Category` is text with a fixed allow-list; MIME type is capped at 255 characters.
- `StorageKey` is a normalized relative path such as `{userId}/{projectId-or-personal}/{guid}.{approvedExtension}`. It is never derived directly from the original filename.
- Visibility checks cover owner, administrator, project manager, project member, active user share, and active project-team share. Team-lead management additionally requires a `ProjectMember.Role` value of `TeamLead` for that project.
- Task associations must point to a task whose project matches the document project. Task visibility reuses the existing task/project access boundary.
- Upload order is validate -> scan -> generate key -> write file -> insert metadata -> audit/notify. Any post-write metadata failure deletes the file; any per-file failure leaves no accessible record.
- Replacement writes and scans the new file before swapping the stored key, then removes the old file after metadata success. Delete removes metadata access and backing file with an auditable result.

## Contract and Integration Design

The logical operations are documented in [contracts/document-management.yaml](contracts/document-management.yaml). In the Blazor Server implementation:

- `IDocumentService` supplies list/search/filter, upload, content authorization, metadata update, replacement, deletion, sharing, and audit operations to Razor components.
- `GET /documents/{documentId}/content` is an authorized minimal endpoint or equivalent page handler that resolves metadata through the service before streaming inline or as an attachment. It never exposes `StorageKey`.
- Existing `INotificationService` receives document-share and project-document notifications. Add document-specific notification enum values only where existing values cannot express the event.
- `Index.razor`, `ProjectDetails.razor`, and `Tasks.razor` consume service projections rather than loading document entities directly.

## Validation Strategy

Because the repository has no test project:

1. Run `dotnet build .\ContosoDashboard\ContosoDashboard.csproj` after each implementation slice.
2. Run the [quickstart smoke-test sequence](quickstart.md), including scanner failure, mixed batch results, direct content denial, project-role boundaries, cleanup, notification, and 12-month audit behavior.
3. Inspect storage root and database records after successful and failed operations to prove no orphan files or incomplete records remain.
4. Measure list/search and preview timings against the specification thresholds using up to 500 seeded or generated documents.
5. Before release, add a dedicated test project for `DocumentService`, storage cleanup, authorization matrices, and content-route responses; its absence is a known current repository gap.

## Complexity Tracking

No constitutional violations require special justification.
