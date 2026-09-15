# Data Model: Document Upload and Management

## Document

Represents one uploaded file and its searchable metadata.

| Field | Type | Required | Rules |
|---|---|---:|---|
| `DocumentId` | `int` | yes | Identity primary key; existing integer-key convention |
| `Title` | `string` | yes | 1-255 characters after trimming |
| `Description` | `string?` | no | Maximum 2000 characters |
| `Category` | `string` | yes | Maximum 100; one of Project Documents, Team Resources, Personal Files, Reports, Presentations, Other |
| `Tags` | `string?` | no | Maximum 1000; normalized for search |
| `OriginalFileName` | `string` | yes | Display-only original name; never used as a storage path |
| `StorageKey` | `string` | yes | Relative generated key; unique; outside `wwwroot` |
| `ContentType` | `string` | yes | Maximum 255; allow-listed MIME type |
| `FileExtension` | `string` | yes | Normalized approved extension |
| `FileSizeBytes` | `long` | yes | Greater than 0 and at most 26,214,400 bytes |
| `UploadedDate` | `DateTime` | yes | UTC timestamp |
| `UploadedByUserId` | `int` | yes | FK to `User`; owner and audit actor baseline |
| `ProjectId` | `int?` | no | FK to `Project`; required for project-scoped access when present |
| `TaskId` | `int?` | no | FK to `TaskItem`; task association must match its project |
| `IsDeleted` | `bool` | yes | Defaults false; deleted records are inaccessible and may be hard-removed per spec |
| `DeletedDate` | `DateTime?` | no | UTC deletion timestamp for audit/recovery diagnostics |

### Document invariants

- `StorageKey` must be generated from trusted identifiers and a GUID; no user-supplied path segment is accepted.
- `ProjectId` and `TaskId` are optional independently, but a task association requires the task's project to equal `ProjectId`.
- A document is visible to its owner, an administrator, an authorized project member, a project manager, or an active share recipient.
- Team-lead management requires a `ProjectMember` row for the document's project with role `TeamLead`.
- Deleted documents cannot be searched, downloaded, previewed, edited, shared, or returned by dashboard counts.

## DocumentShare

Represents an explicit share grant to one user or one project team.

| Field | Type | Required | Rules |
|---|---|---:|---|
| `DocumentShareId` | `int` | yes | Identity primary key |
| `DocumentId` | `int` | yes | FK to `Document`; restrict or cascade only after file cleanup policy is satisfied |
| `SharedByUserId` | `int` | yes | FK to `User`; must own or manage the document |
| `RecipientUserId` | `int?` | conditional | Specific recipient; mutually exclusive with `ProjectId` |
| `ProjectId` | `int?` | conditional | Existing project team recipient; mutually exclusive with `RecipientUserId` |
| `CreatedDate` | `DateTime` | yes | UTC timestamp |
| `RevokedDate` | `DateTime?` | no | UTC timestamp; revoked shares do not grant access |

### DocumentShare invariants

- Exactly one of `RecipientUserId` and `ProjectId` is set.
- A user share recipient must exist and cannot be the granting actor only as a duplicate active grant.
- A project share must target the document's project or be rejected; department-wide shares are out of scope.
- Unique active grants prevent duplicate notifications and duplicate access rows.
- Revocation is auditable and immediately removes share-derived access.

## DocumentAuditEvent

Represents an auditable document action retained for 12 months.

| Field | Type | Required | Rules |
|---|---|---:|---|
| `DocumentAuditEventId` | `int` | yes | Identity primary key |
| `DocumentId` | `int?` | no | Nullable for rejected uploads with no persisted document |
| `ActorUserId` | `int` | yes | FK to `User` |
| `Action` | `string` | yes | Upload, Download, Preview, Replace, Delete, Share, or UploadRejected |
| `Outcome` | `string` | yes | Succeeded or Failed |
| `Details` | `string?` | no | Sanitized diagnostic context; maximum 2000 |
| `CreatedDate` | `DateTime` | yes | UTC timestamp; indexed for retention purge |

## Existing relationships

- `User` owns many `Document` records and creates many audit events/shares.
- `Project` has many documents and shares; its manager and members determine access.
- `TaskItem` may reference many documents through `TaskId`; task access follows its project/assignment authorization.
- `Notification` remains the user-facing delivery mechanism for share and project-document events.

## State transitions

```text
Validated -> ScannedSafe -> Stored -> Persisted -> Available
Validated -> ScanFailed/Rejected
Stored -> PersistenceFailed -> FileCleaned
Available -> Replaced -> Available
Available -> Deleted -> Inaccessible
Available -> Shared -> Available
Shared -> Revoked -> Available (for other authorized paths)
```
