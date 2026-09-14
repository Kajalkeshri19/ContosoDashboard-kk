# Implementation Plan: Document Upload and Management

**Branch**: `001-document-upload` | **Date**: 2026-09-14 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-document-upload/spec.md`

## Summary

Add a secure document management capability to the Blazor Server dashboard using local filesystem storage, metadata persistence, role-aware access rules, and cloud-ready abstraction. The implementation will extend the existing EF Core model and service layer while keeping the current architecture intact and offline-friendly.

## Technical Context

**Language/Version**: C# on .NET 8.0  
**Primary Dependencies**: ASP.NET Core, Blazor Server, Entity Framework Core SQL Server, ASP.NET Core Authentication, custom service layer  
**Storage**: SQL Server for metadata, local file system under AppData/uploads for offline file storage  
**Testing**: No dedicated test project yet; validation should be implemented via build checks and targeted UI/service verification once the project adds test infrastructure  
**Target Platform**: Windows development environment with a web application deployed to local/intranet hosting  
**Project Type**: Web application  
**Performance Goals**: Uploads up to 25 MB complete within 30 seconds on a typical network; document search and list views return within 2 seconds for up to 500 rows  
**Constraints**: Must remain offline-capable, must not require Azure services, must use local filesystem storage for the training build, and must preserve current mock auth model  
**Scale/Scope**: Employee dashboard app with project/task data, notifications, multiple document categories, and role-based access for team leads, managers, and administrators

## Constitution Check

The design passes the constitution gate:

- **Training-first scope**: The feature remains local and offline-capable and does not add a cloud
	dependency or a second application architecture.
- **Layered architecture**: Metadata remains in EF Core, file operations are isolated behind
	`IFileStorageService`, and the local implementation is registered through dependency injection.
- **Secure by default**: Files stay outside `wwwroot`, names are generated, authorization is enforced
	in services as well as UI, and upload failure cleanup is part of the implementation plan.
- **Incremental delivery**: The three prioritized user stories have independent checks, with the P1
	upload workflow delivering the first usable slice.
- **Validation and traceability**: No dedicated test project exists yet, so build checks and targeted
	service/UI verification are explicitly required; security and performance checks are listed in the
	task plan.

## Project Structure

### Documentation (this feature)

```text
specs/001-document-upload/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
└── tasks.md
```

### Source Code (repository root)

```text
ContosoDashboard/
├── Data/
│   └── ApplicationDbContext.cs
├── Models/
│   ├── Document.cs
│   ├── DocumentShare.cs
│   ├── Notification.cs
│   ├── Project.cs
│   ├── User.cs
│   └── TaskItem.cs
├── Pages/
│   ├── Documents.razor
│   ├── ProjectDetails.razor
│   ├── Index.razor
│   └── Tasks.razor
├── Services/
│   ├── IDocumentService.cs
│   ├── DocumentService.cs
│   ├── IFileStorageService.cs
│   ├── LocalFileStorageService.cs
│   ├── NotificationService.cs
│   └── ProjectService.cs
├── Shared/
│   ├── NavMenu.razor
│   └── MainLayout.razor
├── Program.cs
├── appsettings.json
└── appsettings.Development.json
```

**Structure Decision**: Extend the existing single-project Blazor application with a focused document domain and service layer, keeping business logic and storage logic separate while integrating with current pages and authorization patterns.

## Complexity Tracking

No constitutional violations require special justification for this feature. The design remains within the current architecture and avoids cross-application complexity or unnecessary infrastructure.

---

## Phase-by-Phase Implementation Notes

### Phase 0: Research and design

- Confirm the file storage path pattern and access rules.
- Validate a local storage layout that keeps uploaded documents outside the web root.
- Define the Document and DocumentShare metadata model fields consistent with the existing integer key strategy.

### Phase 1: Data model and storage

- Add Document and DocumentShare entities in the EF Core context.
- Add a storage abstraction and local implementation for upload, delete, and download operations.
- Ensure unique file names and authorization checks are enforced before database data is committed.

### Phase 2: Service and permission layer

- Implement business logic for upload validation, category handling, project authorization, and replacement workflows.
- Add project and user permission checks to ensure only authorized users can access or share documents.

### Phase 3: UI and integration

- Add a document management page, upload form, and browsing controls.
- Integrate document counts and recent-document widgets into the dashboard and project detail pages.
- Add in-app notifications for document share actions and project document additions.

### Phase 4: Hardening and polish

- Verify role protections, file validation, and secure local file paths.
- Check performance for project and search views.
- Confirm audit logging behavior and coverage for document events.
