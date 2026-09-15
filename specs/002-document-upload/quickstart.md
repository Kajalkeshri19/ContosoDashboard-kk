# Quickstart: Document Upload and Management

## Prerequisites

- Windows development environment
- .NET 9 SDK compatible with `ContosoDashboard/ContosoDashboard.csproj`
- SQL Server LocalDB configured by the existing connection string
- A configured malware scanner implementation; uploads fail closed when none is registered

## Build

From the repository root:

```powershell
dotnet build .\ContosoDashboard\ContosoDashboard.csproj
```

Baseline recorded on 2026-09-15: build succeeded before MVP implementation with three existing nullable warnings in `ContosoDashboard/Services/TaskService.cs` and no errors.

## Configure local storage

Set a storage root outside `ContosoDashboard/wwwroot`, for example:

```json
{
  "DocumentStorage": {
    "RootPath": "AppData/uploads"
  }
}
```

The implementation must resolve relative roots beneath the application content root, create the directory on startup or first use, and persist only relative generated storage keys.

The development configuration enables the local training scanner adapter. The default configuration disables scanning, so non-development environments reject uploads until a scanner is explicitly configured.

## Run

```powershell
dotnet run --project .\ContosoDashboard\ContosoDashboard.csproj
```

Open the HTTPS URL printed by ASP.NET Core and sign in with one of the existing training users.

## Smoke-test sequence

1. Verify an authenticated employee can open the document page.
2. Upload a supported file under 25 MB with a title and approved category; verify it appears in the user's list and in the storage root outside `wwwroot`.
3. Upload a mixed batch; verify valid files succeed independently and invalid or unscanned files fail without an accessible file or metadata record.
4. Attempt a disallowed project upload, direct content request, download, and preview; verify each is denied and creates the required audit outcome.
5. Search and filter by title, category, project, date, size, tags, and uploader; verify unauthorized documents never appear.
6. Share with a project team and a specific user; verify notifications and shared-with-me visibility.
7. Replace and delete as the owner, assigned team lead, project manager, and administrator; verify role boundaries, file cleanup, document identity, and audit records.
8. Confirm dashboard recent documents shows five items and project/task views show only permitted documents.
9. Verify audit events remain queryable for 12 months and that expired events are eligible for the documented purge process.

## Reset local training state

Because the application currently uses `EnsureCreated`, reset the LocalDB database using the repository's established training procedure before retesting schema changes. Also remove the configured local upload root after confirming no unrelated files are stored there.
