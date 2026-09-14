# Tasks: Document Upload and Management

**Input**: Design documents from `/specs/001-document-upload/`  
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and integration of document management support

- [ ] T001 Add document-related folders and supporting configuration under ContosoDashboard/
- [ ] T002 Review the current auth and role policies and map document permissions to existing roles
- [ ] T003 [P] Confirm shared UI patterns for pages, navigation, and dashboard widgets in the current app

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core document infrastructure that must be complete before story implementation

- [ ] T004 Create `ContosoDashboard/Models/Document.cs` with metadata fields, category text, integer DocumentId, and relationships to User and Project
- [ ] T005 Create `ContosoDashboard/Models/DocumentShare.cs` to track share assignments and recipient access
- [ ] T006 Update `ContosoDashboard/Data/ApplicationDbContext.cs` to include DbSet entries, indexes, and seed support for documents
- [ ] T007 Create `ContosoDashboard/Services/IFileStorageService.cs` with upload, delete, download, and URL-generation contracts
- [ ] T008 Create `ContosoDashboard/Services/LocalFileStorageService.cs` using local filesystem storage outside `wwwroot`
- [ ] T009 Add secure upload path generation based on userId/projectId and GUID-based file names
- [ ] T010 Configure dependency injection for storage and document-related services in `ContosoDashboard/Program.cs`

**Checkpoint**: Foundation ready - document uploads and metadata persistence can now be implemented in parallel with story work.

---

## Phase 3: User Story 1 - Upload and secure document storage (Priority: P1) 🎯 MVP

**Goal**: Let employees upload and store validated documents in a secure, auditable way.

**Independent Test**: A logged-in user can upload a valid file and later see it listed in their document workspace.

### Implementation for User Story 1

- [ ] T011 [P] [US1] Implement `DocumentService` validation for file type, size, category, and project access rules in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T012 [P] [US1] Add document upload logic to persist file metadata after the file is safely stored on disk
- [ ] T013 [US1] Add secure upload endpoint or component flow for one-or-more file selection and progress feedback
- [ ] T014 [US1] Add success and error messaging for upload results in the document UI
- [ ] T015 [US1] Add audit logging for upload and failure actions in the service or notification workflow
- [ ] T016 [US1] Add a document list view for the current user in `ContosoDashboard/Pages/Documents.razor`

**Checkpoint**: User Story 1 should be fully functional and testable independently.

---

## Phase 4: User Story 2 - Browse, search, and organize documents (Priority: P2)

**Goal**: Make documents discoverable and visible by category, project, and search terms.

**Independent Test**: A user can filter and search the document list and view project documents with the right authorization.

### Implementation for User Story 2

- [ ] T017 [P] [US2] Add sorting and filtering logic for document title, upload date, category, file size, and project association
- [ ] T018 [US2] Implement search by title, description, tags, uploader, and project in the document service
- [ ] T019 [US2] Add project document visibility logic to `ContosoDashboard/Pages/ProjectDetails.razor`
- [ ] T020 [US2] Add document summary counts and recent-document widgets to `ContosoDashboard/Pages/Index.razor`
- [ ] T021 [US2] Update `ContosoDashboard/Shared/NavMenu.razor` to include a document management navigation entry
- [ ] T022 [US2] Enforce permission filtering so only authorized documents appear in list and search views

**Checkpoint**: User Stories 1 and 2 should work independently and support collaborative project browsing.

---

## Phase 5: User Story 3 - Share and manage document access (Priority: P3)

**Goal**: Enable secure sharing, notifications, and replacement or deletion of documents.

**Independent Test**: A user can share a document with another user and the recipient sees it in shared access and notification flows.

### Implementation for User Story 3

- [ ] T023 [P] [US3] Implement share logic in `DocumentService` and `DocumentShare` persistence
- [ ] T024 [US3] Create in-app notification actions when shared documents are assigned or project documents are added
- [ ] T025 [US3] Add access-control checks for delete, replace, and download actions based on ownership or project manager rules
- [ ] T026 [US3] Implement metadata edit and replace-file support for document owners in the UI
- [ ] T027 [US3] Add delete confirmation flow and permanent file cleanup for authorized users
- [ ] T028 [US3] Add audit logging for share, delete, replace, and download events

**Checkpoint**: All user stories should now be independently functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final hardening, performance review, and project readiness

- [ ] T029 [P] Security review for path traversal, file validation, role enforcement, and unauthorized download bypass
- [ ] T030 [P] Validate upload and list performance with realistic document counts and search queries
- [ ] T031 [P] Clean up UI copy, validation messaging, and navigation consistency across document pages
- [ ] T032 [P] Review error handling and recovery for partial upload failures and database insert exceptions
- [ ] T033 [P] Update supporting documentation for document categories, local storage path rules, and future Azure migration notes

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup**: No dependencies
- **Foundational**: Depends on Setup completion and blocks all user stories
- **User Story 1**: Depends on foundational work
- **User Story 2**: Depends on User Story 1 baseline but should remain independently testable
- **User Story 3**: Depends on foundational and user story access patterns
- **Polish**: Depends on all desired stories being complete

### Parallel Opportunities

- The foundational service and storage tasks can proceed in parallel when the service contracts and path strategy are agreed.
- The document list/search tasks can proceed in parallel after the core document model is available.
- Notification and share tasks can proceed with the document UI work once the share model and permission logic are in place.
