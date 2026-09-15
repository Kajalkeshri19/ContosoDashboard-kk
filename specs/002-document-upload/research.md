# Research: Document Upload and Management

**Feature**: `002-document-upload`  
**Date**: 2026-09-15

## Decision 1: Keep the feature in the existing Blazor Server application

- **Decision**: Implement document workflows as EF Core models, scoped services, Razor components, and one authorized content-delivery route in the existing `ContosoDashboard` project.
- **Rationale**: The repository has no separate frontend/backend projects and no existing JSON API or upload endpoint. `Program.cs` registers Blazor Server, Razor Pages, scoped services, authentication, and authorization in one application. A second application boundary would violate the constitution's simplicity and training-first principles.
- **Alternatives considered**: A separate REST API plus frontend was rejected because it adds deployment and authentication complexity without an existing repository pattern that needs it.

## Decision 2: Use the repository's current net9.0 and EF Core 9 stack

- **Decision**: Plan against `ContosoDashboard/ContosoDashboard.csproj`, which targets `net9.0` and references EF Core SQL Server 9.0.0.
- **Rationale**: The project file is the authoritative build contract. Older README and feature documents mention .NET 8, but the current project and generated build artifacts target .NET 9.
- **Alternatives considered**: Retaining .NET 8 in the plan was rejected because it would describe a framework the current project no longer targets.

## Decision 3: Add integer-keyed metadata entities with explicit access relationships

- **Decision**: Add `Document`, `DocumentShare`, and `DocumentAuditEvent` entities with integer primary keys, string category values, nullable `ProjectId`/`TaskId`, uploader ownership, and explicit share targets for a user or project team.
- **Rationale**: Existing `User`, `Project`, `TaskItem`, and `Notification` entities all use integer keys and navigation properties. `ProjectMember.Role` already identifies project team-lead membership. Explicit relationships make authorization queries and audit reporting testable.
- **Alternatives considered**: Storing all document metadata as JSON was rejected because it weakens relational filtering, indexing, and referential integrity. An enum category was rejected by the approved specification, which requires text categories.

## Decision 4: Isolate storage behind `IFileStorageService`

- **Decision**: Store files under a configurable `AppData/uploads` root outside `wwwroot`; persist only a normalized relative storage key generated from user/project scope, a GUID, and an approved extension.
- **Rationale**: This prevents direct static-file exposure and path traversal while preserving a future Azure Blob implementation boundary. Storage is written before metadata, and failed metadata persistence triggers deletion of the newly written file.
- **Alternatives considered**: Writing uploads into `wwwroot` was rejected because it bypasses authorization. Persisting absolute paths was rejected because it is machine-specific and unsafe to expose.

## Decision 5: Fail closed through an `IMalwareScanner` abstraction

- **Decision**: Add `IMalwareScanner.ScanAsync` to the document service boundary. Uploads proceed only after a successful safe result; unavailable, errored, or unsafe scans reject the file.
- **Rationale**: This is the accepted clarification and the only behavior consistent with the specification's security requirement. The scanner implementation is deployment-provided and must be registered in the training application; no cloud scanner SDK is introduced.
- **Alternatives considered**: Warning-only uploads were rejected because they allow unscanned files into shared storage. Personal-only bypasses were rejected because they create inconsistent security behavior and an easy escalation path.

## Decision 6: Process multi-file uploads independently

- **Decision**: `IDocumentService.UploadAsync` returns one result per file. Each file validates, scans, stores, persists, audits, and notifies independently; failure cleanup is local to that file.
- **Rationale**: This matches the accepted clarification, avoids retrying valid 25 MB files, and gives the user precise feedback. Database work for each file is isolated so one failure does not roll back successful siblings.
- **Alternatives considered**: Batch atomicity was rejected because it conflicts with the approved per-file behavior and would increase failure blast radius.

## Decision 7: Enforce authorization in the document service and content route

- **Decision**: Centralize visibility and management checks in a reusable authorization policy/service used by list, search, upload, share, edit, delete, download, and preview operations. Project access is based on manager or `ProjectMember`; team-lead management is limited to assigned project teams; administrators have global access.
- **Rationale**: Existing `ProjectService` and `TaskService` use service-level checks, but static-file middleware cannot authorize document files. An authorized route must resolve metadata first and stream only after the same check.
- **Alternatives considered**: UI-only checks were rejected because direct requests and guessed identifiers would bypass them.

## Decision 8: Reuse existing notifications and add document audit persistence

- **Decision**: Extend `NotificationType` with document-related types or use a documented existing project type where appropriate; create a dedicated `DocumentAuditEvent` table retaining events for 12 months.
- **Rationale**: `NotificationService` already persists user notifications and enforces recipient ownership for reads. A dedicated audit entity supports actor, document, action, timestamp, outcome, and retention without overloading user-facing notifications.
- **Alternatives considered**: Logging only to text files was rejected because administrators need queryable reporting and the specification requires auditable events.

## Decision 9: Validate through build plus focused manual/UI checks

- **Decision**: Use `dotnet build ContosoDashboard/ContosoDashboard.csproj` as the automated baseline, supplemented by a documented local smoke test for authentication, upload validation, authorization, search, download/preview, sharing, cleanup, and audit records.
- **Rationale**: The repository currently has no test project, test framework, or existing file-upload harness. The constitution requires executable verification without claiming unimplemented automated coverage.
- **Alternatives considered**: Adding a test framework in this planning slice was rejected as an unrelated infrastructure expansion; a future test project can target the service contracts once the feature exists.
