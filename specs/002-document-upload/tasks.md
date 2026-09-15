# Tasks: Document Upload and Management

**Input**: Design documents from `/specs/002-document-upload/`
**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [quickstart.md](quickstart.md), [contracts/document-management.yaml](contracts/document-management.yaml)

**Tests**: No automated test tasks are included because the feature specification does not request TDD and the repository has no test project. Each story includes executable build and UI/service validation tasks; a dedicated test project is listed as future hardening.

**Organization**: Tasks are grouped by user story so each priority slice can be implemented and validated independently after the shared foundation.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare configuration and the existing single-project Blazor Server application for document work.

- [X] T001 Review `ContosoDashboard/ContosoDashboard.csproj` and confirm the implementation targets `net9.0` with Entity Framework Core SQL Server 9.0.0.
- [X] T002 [P] Add `DocumentStorage:RootPath` and scanner configuration placeholders to `ContosoDashboard/appsettings.json`.
- [X] T003 [P] Add local training storage and scanner settings to `ContosoDashboard/appsettings.Development.json` without placing uploads under `ContosoDashboard/wwwroot`.
- [X] T004 [P] Document the configured storage root, fail-closed scanner prerequisite, and reset procedure in `specs/002-document-upload/quickstart.md`.
- [X] T005 Run `dotnet build .\\ContosoDashboard\\ContosoDashboard.csproj --no-restore` and record the baseline result before feature implementation in `specs/002-document-upload/quickstart.md`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish shared persistence, storage, scanning, authorization, and composition boundaries required by all user stories.

**Checkpoint**: No story work starts until the foundation builds and the document metadata, storage, scanner, and authorization contracts are registered.

- [X] T006 [P] Create `ContosoDashboard/Models/Document.cs` with integer `DocumentId`, required metadata, approved text category, generated `StorageKey`, optional `ProjectId` and `TaskId`, ownership, lifecycle fields, and navigation properties.
- [X] T007 [P] Create `ContosoDashboard/Models/DocumentShare.cs` with integer key, document/user/project-team targets, grantor, created/revoked timestamps, and mutually exclusive recipient invariants.
- [X] T008 [P] Create `ContosoDashboard/Models/DocumentAuditEvent.cs` with integer key, actor, document, action, outcome, sanitized details, and timestamp fields.
- [X] T009 Update `ContosoDashboard/Data/ApplicationDbContext.cs` with document DbSets, foreign-key relationships, delete behaviors, indexes for visibility/search/retention, and model constraints from `specs/002-document-upload/data-model.md`.
- [X] T010 [P] Create `ContosoDashboard/Services/IFileStorageService.cs` with generated-key write, read, replace, and delete operations that never accept an unsafe absolute path.
- [X] T011 [P] Create `ContosoDashboard/Services/LocalFileStorageService.cs` to resolve the configured root outside `wwwroot`, normalize relative keys, create directories, stream files, and prevent path traversal.
- [X] T012 [P] Create `ContosoDashboard/Services/IMalwareScanner.cs` with a safe/unsafe/error result contract that supports fail-closed upload behavior.
- [X] T013 [P] Create `ContosoDashboard/Services/ConfiguredMalwareScanner.cs` to require a configured scanner and return failure when unavailable, errored, or unsafe, without adding a cloud SDK dependency.
- [X] T014 Create `ContosoDashboard/Services/DocumentAuthorizationService.cs` with reusable visibility and management checks for owners, administrators, project managers, project members, active shares, and assigned team leads.
- [X] T015 Create `ContosoDashboard/Services/IDocumentService.cs` with list/search/filter, per-file upload, content authorization, metadata update, replacement, deletion, sharing, notification, and audit operations matching `specs/002-document-upload/contracts/document-management.yaml`.
- [X] T016 Register `IFileStorageService`, `IMalwareScanner`, `DocumentAuthorizationService`, and `IDocumentService` in `ContosoDashboard/Program.cs` and preserve the existing authentication and role policies.
- [X] T017 Add `Documents` and `DocumentShares` navigation collections to `ContosoDashboard/Models/User.cs` and `ContosoDashboard/Models/Project.cs`, and add document navigation to `ContosoDashboard/Models/TaskItem.cs` where required by the model.
- [X] T018 Extend `ContosoDashboard/Models/Notification.cs` with document-share and project-document notification types, preserving existing notification persistence and read authorization.
- [X] T019 Implement the document audit retention query/purge operation in `ContosoDashboard/Services/DocumentService.cs` so events older than 12 months are eligible for maintenance without affecting current audit reporting.
- [X] T020 Run `dotnet build .\ContosoDashboard\ContosoDashboard.csproj --no-restore` and verify the foundation compiles before beginning user-story implementation.

---

## Phase 3: User Story 1 - Upload and secure documents (Priority: P1) 🎯 MVP

**Goal**: Let authenticated employees upload supported documents to personal or permitted project areas with fail-closed scanning, per-file results, secure storage, metadata, audit records, and cleanup on failure.

**Independent Test**: Sign in as an employee, upload a valid file and a mixed-validity batch, verify successful metadata and storage outside `wwwroot`, verify rejected files leave no record/file, and verify a disallowed user cannot retrieve content.

### Implementation for User Story 1

- [X] T021 [US1] Implement supported extension, content-type, 25 MB size, title, category, tag, project, and task validation in `ContosoDashboard/Services/DocumentService.cs`.
- [X] T022 [US1] Implement fail-closed scan orchestration in `ContosoDashboard/Services/DocumentService.cs` using `IMalwareScanner` before any file is persisted.
- [X] T023 [US1] Implement generated storage-key creation and per-file upload ordering in `ContosoDashboard/Services/DocumentService.cs`, including cleanup when metadata persistence fails.
- [X] T024 [US1] Implement per-file upload result reporting in `ContosoDashboard/Services/DocumentService.cs` so valid files remain successful when sibling files fail.
- [X] T025 [US1] Add successful and failed upload audit events in `ContosoDashboard/Services/DocumentService.cs`, including scanner, validation, storage, and persistence outcomes.
- [X] T026 [US1] Add the authorized document content route in `ContosoDashboard/Program.cs` that resolves access through `DocumentAuthorizationService` before streaming download or inline preview content.
- [X] T027 [US1] Create `ContosoDashboard/Pages/Documents.razor` with authenticated upload controls, required metadata, multi-file selection, progress/results, validation messages, and the current user's document list.
- [X] T028 [US1] Add `@key` input reset, copied `IBrowserFile` metadata, bounded stream reads, and cleared browser-file references in `ContosoDashboard/Pages/Documents.razor`.
- [X] T029 [US1] Add document navigation to `ContosoDashboard/Shared/NavMenu.razor` and protect the route with the existing `[Authorize]` pattern.
- [X] T030 [US1] Run the P1 smoke test from `specs/002-document-upload/quickstart.md`, including valid upload, oversize/unsupported/scanner rejection, mixed batch independence, storage-root inspection, and unauthorized content access.
- [X] T031 [US1] Run `dotnet build .\ContosoDashboard\ContosoDashboard.csproj --no-restore` and resolve any P1 implementation errors before the MVP checkpoint.

**Checkpoint**: User Story 1 is independently usable when a permitted user can upload and list valid files, rejected files produce no accessible artifacts, and direct unauthorized content access is denied.

---

## Phase 4: User Story 2 - Find and use permitted documents (Priority: P2)

**Goal**: Make permitted documents discoverable through list, search, sort, filter, project/task views, downloads, and safe PDF/image previews.

**Independent Test**: Seed or upload documents across categories and projects, verify search/filter results and timing for up to 500 records, verify project/task visibility, and verify permitted download/preview without exposing `StorageKey`.

### Implementation for User Story 2

- [ ] T032 [P] [US2] Implement permission-filtered document projections, search, sorting, category/project/date/size filters, and uploader/project joins in `ContosoDashboard/Services/DocumentService.cs`.
- [ ] T033 [P] [US2] Add query indexes and projection support for title/category/date/project/size/search fields in `ContosoDashboard/Data/ApplicationDbContext.cs`.
- [ ] T034 [US2] Implement download and inline-preview content-type/disposition handling in `ContosoDashboard/Program.cs` and `ContosoDashboard/Services/DocumentService.cs`, ensuring the storage key never reaches the response.
- [ ] T035 [US2] Extend `ContosoDashboard/Pages/Documents.razor` with search, sort, category/project/date/size filters, permitted download links, and PDF/image preview actions.
- [ ] T036 [US2] Add permitted project-document loading and display to `ContosoDashboard/Pages/ProjectDetails.razor` using `IDocumentService` rather than direct EF queries.
- [ ] T037 [US2] Add task-document loading/upload entry points to `ContosoDashboard/Pages/Tasks.razor`, ensuring task associations match the task's project.
- [ ] T038 [US2] Add recent-five document data and document count summary to `ContosoDashboard/Pages/Index.razor` using a permission-filtered service projection.
- [ ] T039 [US2] Run the P2 smoke and performance checks from `specs/002-document-upload/quickstart.md` for search/filter authorization, project/task visibility, download, preview, 500-row list/search timing, and 3-second preview timing.
- [ ] T040 [US2] Run `dotnet build .\ContosoDashboard\ContosoDashboard.csproj --no-restore` and resolve any P2 implementation errors before the discovery checkpoint.

**Checkpoint**: User Stories 1 and 2 are independently functional when users can find only permitted documents, project/task pages respect the same access rules, and authorized content delivery meets the stated timing targets.

---

## Phase 5: User Story 3 - Share and manage document access (Priority: P3)

**Goal**: Enable authorized metadata edits, file replacement, deletion, project-team or specific-user sharing, notifications, shared-with-me visibility, and auditable lifecycle actions.

**Independent Test**: Share a document with a project team and a specific user, verify notifications and recipient visibility, then exercise owner/team-lead/project-manager/admin edit, replace, delete, revoke, and unauthorized actions.

### Implementation for User Story 3

- [ ] T041 [P] [US3] Implement metadata update authorization and validation in `ContosoDashboard/Services/DocumentService.cs` for owners, assigned team leads, project managers, and administrators.
- [ ] T042 [P] [US3] Implement scanned replacement-file staging, document-identity preservation, old-file cleanup, and failure recovery in `ContosoDashboard/Services/DocumentService.cs`.
- [ ] T043 [P] [US3] Implement delete authorization, confirmation-ready result handling, metadata inaccessibility, backing-file deletion, and audit outcomes in `ContosoDashboard/Services/DocumentService.cs`.
- [ ] T044 [US3] Implement user/project-team share validation, duplicate active-share prevention, revocation, and access re-evaluation in `ContosoDashboard/Services/DocumentService.cs`.
- [ ] T045 [US3] Create document sharing and lifecycle UI in `ContosoDashboard/Pages/Documents.razor`, including recipient selection, edit metadata, replace, delete confirmation, revoke, and shared-with-me views.
- [ ] T046 [US3] Extend `ContosoDashboard/Services/NotificationService.cs` or the document service integration to notify specific users and current project members for shares and new project documents.
- [ ] T047 [US3] Add lifecycle and sharing audit records with actor, document, action, outcome, timestamp, and sanitized details in `ContosoDashboard/Services/DocumentService.cs`.
- [ ] T048 [US3] Add authorized shared-document summaries to `ContosoDashboard/Pages/Index.razor` or `ContosoDashboard/Pages/Documents.razor` without exposing records after revocation or permission loss.
- [ ] T049 [US3] Run the P3 smoke test from `specs/002-document-upload/quickstart.md` across owner, assigned team lead, project manager, administrator, unauthorized user, project-team share, specific-user share, notification, replacement, deletion, revocation, and audit-retention cases.
- [ ] T050 [US3] Run `dotnet build .\ContosoDashboard\ContosoDashboard.csproj --no-restore` and resolve any P3 implementation errors before release hardening.

**Checkpoint**: All three user stories are functional when sharing, lifecycle management, notifications, access revocation, file cleanup, and 12-month audit behavior pass the role matrix.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Harden security, performance, documentation, and release validation across all stories.

- [ ] T051 [P] Review `ContosoDashboard/Services/LocalFileStorageService.cs` and `ContosoDashboard/Program.cs` for path traversal, content-disposition injection, response-header safety, and accidental static exposure.
- [ ] T052 [P] Review `ContosoDashboard/Services/DocumentAuthorizationService.cs` and `ContosoDashboard/Services/DocumentService.cs` against the full owner/team-lead/project-manager/administrator authorization matrix.
- [ ] T053 [P] Validate storage/database recovery and orphan cleanup scenarios in `ContosoDashboard/Services/DocumentService.cs` using the failure cases documented in `specs/002-document-upload/data-model.md`.
- [ ] T054 [P] Validate list/search/upload/preview timing and the five-item dashboard limit through `specs/002-document-upload/quickstart.md`.
- [ ] T055 [P] Update repository-root `README.md` with document storage location, scanner fail-closed behavior, training-only limitations, and local reset guidance.
- [ ] T056 Add any required document category, scanner, storage, or retention notes to `specs/002-document-upload/quickstart.md` after implementation differs from the planned defaults.
- [ ] T057 Run `dotnet build .\ContosoDashboard\ContosoDashboard.csproj --no-restore` and perform the complete `specs/002-document-upload/quickstart.md` smoke-test sequence.
- [ ] T058 Record the known absence of an automated test project and create the future test-project follow-up in `specs/002-document-upload/plan.md` if automated coverage is still unavailable.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; confirms the current net9.0 project and configuration surfaces.
- **Foundational (Phase 2)**: Depends on Setup; blocks all user stories because every story uses the entities, storage, scanner, authorization, and service contracts.
- **User Story 1 (Phase 3)**: Depends on Foundational; delivers the MVP upload and secure-storage slice.
- **User Story 2 (Phase 4)**: Depends on Foundational and the US1 document record/content route baseline; it adds discovery and project/task integration without changing US1 security rules.
- **User Story 3 (Phase 5)**: Depends on Foundational and the US1 document identity/storage baseline; it adds sharing and lifecycle management and reuses US2 visibility projections.
- **Polish (Phase 6)**: Depends on all desired story checkpoints.

### User Story Dependencies

- **US1 (P1)**: Starts after Phase 2; no dependency on another user story.
- **US2 (P2)**: Starts after Phase 2, with the persisted document and protected content baseline from US1 required for meaningful discovery validation.
- **US3 (P3)**: Starts after Phase 2, with document identity/storage from US1 and visibility projections from US2 reused by sharing and lifecycle UI.

### Within Each User Story

- Models and contracts precede services.
- Storage/scanner/authorization services precede upload orchestration.
- Services precede Razor components and content routes.
- Each story's smoke/build check must pass before its checkpoint is considered complete.

### Parallel Opportunities

- T002-T004 can proceed in parallel because they touch separate configuration/documentation files.
- T006-T008 and T010-T013 can proceed in parallel because they create separate model and infrastructure files; T009, T014, and T015 consume those contracts afterward.
- T032 and T033 can proceed in parallel after the foundation because query implementation and database indexing are separate regions/files.
- T041-T043 can proceed in parallel after the document service baseline because metadata, replacement, and deletion are separate use cases within the same service contract; integrate carefully before T045.
- T051-T056 can proceed in parallel after the story checkpoints because they review separate security, performance, documentation, and recovery surfaces.
- Different user stories can be assigned to different developers only after the foundational checkpoint, with shared edits to `DocumentService.cs` coordinated.

---

## Parallel Example: User Story 1

```text
Task: "T021 [US1] Implement validation in ContosoDashboard/Services/DocumentService.cs"
Task: "T026 [US1] Add authorized content route in ContosoDashboard/Program.cs"
Task: "T027 [US1] Create upload/list UI in ContosoDashboard/Pages/Documents.razor"
Task: "T029 [US1] Add navigation in ContosoDashboard/Shared/NavMenu.razor"
```

These tasks can be prepared in parallel after T015/T016, but the service implementation must be available before the UI smoke test T030.

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 setup.
2. Complete Phase 2 foundation and run T020.
3. Complete Phase 3 upload/security implementation.
4. Run T030 and T031.
5. Stop at the US1 checkpoint and demonstrate secure upload before adding discovery or sharing.

### Incremental Delivery

1. Foundation ready: models, storage, scanner, authorization, service contract, and DI.
2. Add US1: upload, secure content, per-file results, and audit baseline; validate independently.
3. Add US2: search, filters, project/task views, dashboard summaries, download, and preview; validate independently.
4. Add US3: sharing, notifications, replacement, deletion, revocation, and full audit; validate independently.
5. Complete polish and run the full quickstart sequence.

### Parallel Team Strategy

1. One developer completes Phase 1 and coordinates the Phase 2 contracts.
2. After the foundational checkpoint:
   - Developer A: US1 upload/storage/UI.
   - Developer B: US2 query/projection and project/task/dashboard integration.
   - Developer C: US3 sharing/lifecycle/notification integration.
3. Coordinate changes to `DocumentService.cs`, `ApplicationDbContext.cs`, `Documents.razor`, and `Program.cs` before each story checkpoint.

---

## Notes

- Every task is executable and includes at least one concrete repository path.
- `[P]` marks tasks that can proceed in parallel without depending on incomplete work in another task.
- No automated test tasks are claimed because the repository has no test project; smoke/build validation remains mandatory.
- The implementation must preserve the approved fail-closed scanner behavior, independent multi-file processing, project-team/specific-user sharing, 12-month audit retention, and team-lead project boundary.
