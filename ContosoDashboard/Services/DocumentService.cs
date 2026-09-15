using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Services;

public sealed class DocumentService : IDocumentService
{
    private const long MaxFileSizeBytes = 25 * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, string> AllowedContentTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xls"] = "application/vnd.ms-excel",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".ppt"] = "application/vnd.ms-powerpoint",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        [".txt"] = "text/plain",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png"
    };

    private static readonly string[] Categories =
    ["Project Documents", "Team Resources", "Personal Files", "Reports", "Presentations", "Other"];

    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _storage;
    private readonly IMalwareScanner _scanner;
    private readonly DocumentAuthorizationService _authorization;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        ApplicationDbContext context,
        IFileStorageService storage,
        IMalwareScanner scanner,
        DocumentAuthorizationService authorization,
        ILogger<DocumentService> logger)
    {
        _context = context;
        _storage = storage;
        _scanner = scanner;
        _authorization = authorization;
        _logger = logger;
    }

    public async Task<List<DocumentSummary>> GetDocumentsAsync(int userId, DocumentQuery query, CancellationToken cancellationToken = default)
    {
        var documents = await _context.Documents
            .AsNoTracking()
            .Include(d => d.Project)
            .Include(d => d.Shares)
            .Include(d => d.UploadedByUser)
            .Where(d => !d.IsDeleted)
            .ToListAsync(cancellationToken);

        var visible = new List<Document>();
        foreach (var document in documents)
        {
            if (!await _authorization.CanViewAsync(document, userId))
                continue;

            var search = query.Search?.Trim();
            if (!string.IsNullOrWhiteSpace(search) &&
                !string.Join(' ', document.Title, document.Description, document.Tags, document.UploadedByUser.DisplayName, document.Project?.Name)
                    .Contains(search, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!string.IsNullOrWhiteSpace(query.Category) && !string.Equals(document.Category, query.Category, StringComparison.OrdinalIgnoreCase))
                continue;
            if (query.ProjectId.HasValue && document.ProjectId != query.ProjectId)
                continue;
            if (query.FromDate.HasValue && document.UploadedDate < query.FromDate.Value)
                continue;
            if (query.ToDate.HasValue && document.UploadedDate > query.ToDate.Value)
                continue;
            visible.Add(document);
        }

        var ordered = query.Sort.ToLowerInvariant() switch
        {
            "title" => visible.OrderBy(d => d.Title),
            "category" => visible.OrderBy(d => d.Category).ThenByDescending(d => d.UploadedDate),
            "filesize" => visible.OrderBy(d => d.FileSizeBytes),
            _ => visible.OrderByDescending(d => d.UploadedDate)
        };

        return ordered.Select(ToSummary).ToList();
    }

    public async Task<List<DocumentUploadResult>> UploadFilesAsync(int userId, IEnumerable<DocumentUploadRequest> requests, CancellationToken cancellationToken = default)
    {
        var results = new List<DocumentUploadResult>();
        foreach (var request in requests)
        {
            results.Add(await UploadOneAsync(userId, request, cancellationToken));
        }
        return results;
    }

    public async Task<DocumentContent?> GetContentAsync(int documentId, int userId, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted, cancellationToken);
        if (document == null || !await _authorization.CanViewAsync(document, userId))
            return null;

        var stream = await _storage.OpenReadAsync(document.StorageKey, cancellationToken);
        if (stream == null)
        {
            await RecordAuditAsync(documentId, userId, "Download", "Failed", "Stored file was not found.", cancellationToken);
            return null;
        }

        await RecordAuditAsync(documentId, userId, "Download", "Succeeded", null, cancellationToken);
        return new DocumentContent
        {
            Content = stream,
            ContentType = document.ContentType,
            DownloadName = document.OriginalFileName
        };
    }

    public async Task<bool> PurgeExpiredAuditEventsAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddMonths(-12);
        var expired = await _context.DocumentAuditEvents.Where(e => e.CreatedDate < cutoff).ToListAsync(cancellationToken);
        if (expired.Count == 0)
            return false;

        _context.DocumentAuditEvents.RemoveRange(expired);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UpdateMetadataAsync(int documentId, int userId, string title, string? description, string category, string? tags, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted, cancellationToken);
        if (document == null || !await _authorization.CanManageAsync(document, userId) || !ValidateMetadata(title, description, category, tags, out _))
            return false;

        document.Title = title.Trim();
        document.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        document.Category = category.Trim();
        document.Tags = string.IsNullOrWhiteSpace(tags) ? null : tags.Trim();
        await _context.SaveChangesAsync(cancellationToken);
        await RecordAuditAsync(documentId, userId, "MetadataUpdate", "Succeeded", null, cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int documentId, int userId, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted, cancellationToken);
        if (document == null || !await _authorization.CanManageAsync(document, userId))
            return false;

        document.IsDeleted = true;
        document.DeletedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        try
        {
            await _storage.DeleteAsync(document.StorageKey, cancellationToken);
            await RecordAuditAsync(documentId, userId, "Delete", "Succeeded", null, cancellationToken);
            return true;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to delete stored document {DocumentId}.", documentId);
            await RecordAuditAsync(documentId, userId, "Delete", "Failed", "The metadata was removed but file cleanup failed.", cancellationToken);
            return false;
        }
    }

    private async Task<DocumentUploadResult> UploadOneAsync(int userId, DocumentUploadRequest request, CancellationToken cancellationToken)
    {
        string? storageKey = null;
        try
        {
            if (!ValidateRequest(request, out var validationError))
                return await FailedUploadAsync(userId, request, validationError!, cancellationToken);
            if (!await _authorization.CanUploadToProjectAsync(userId, request.ProjectId, request.TaskId))
                return await FailedUploadAsync(userId, request, "You are not authorized to upload to the selected project or task.", cancellationToken);

            if (request.Content.CanSeek)
                request.Content.Position = 0;
            var scan = await _scanner.ScanAsync(request.Content, request.ContentType, cancellationToken);
            if (!scan.IsSafe)
                return await FailedUploadAsync(userId, request, scan.Reason ?? "The file did not pass malware scanning.", cancellationToken);
            if (request.Content.CanSeek)
                request.Content.Position = 0;

            var extension = Path.GetExtension(request.OriginalFileName).ToLowerInvariant();
            var scope = request.ProjectId?.ToString() ?? "personal";
            storageKey = $"{userId}/{scope}/{Guid.NewGuid():N}{extension}";
            await _storage.SaveAsync(request.Content, storageKey, cancellationToken);

            var document = new Document
            {
                Title = request.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                Category = request.Category.Trim(),
                Tags = string.IsNullOrWhiteSpace(request.Tags) ? null : request.Tags.Trim(),
                OriginalFileName = Path.GetFileName(request.OriginalFileName),
                StorageKey = storageKey,
                ContentType = request.ContentType.Trim(),
                FileExtension = extension,
                FileSizeBytes = request.FileSizeBytes,
                UploadedByUserId = userId,
                ProjectId = request.ProjectId,
                TaskId = request.TaskId,
                UploadedDate = DateTime.UtcNow
            };
            _context.Documents.Add(document);
            _context.DocumentAuditEvents.Add(new DocumentAuditEvent
            {
                Document = document,
                ActorUserId = userId,
                Action = "Upload",
                Outcome = "Succeeded",
                CreatedDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);
            return new DocumentUploadResult(request.OriginalFileName, true, null, ToSummary(document));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Document upload failed for {FileName}.", request.OriginalFileName);
            if (storageKey != null)
            {
                try { await _storage.DeleteAsync(storageKey, cancellationToken); }
                catch (Exception cleanupException) { _logger.LogError(cleanupException, "Document upload cleanup failed for {StorageKey}.", storageKey); }
            }
            return await FailedUploadAsync(userId, request, "The document could not be stored. No document was created.", cancellationToken);
        }
    }

    private async Task<DocumentUploadResult> FailedUploadAsync(int userId, DocumentUploadRequest request, string error, CancellationToken cancellationToken)
    {
        await RecordAuditAsync(null, userId, "UploadRejected", "Failed", error, cancellationToken);
        return new DocumentUploadResult(request.OriginalFileName, false, error, null);
    }

    private async Task RecordAuditAsync(int? documentId, int actorUserId, string action, string outcome, string? details, CancellationToken cancellationToken)
    {
        try
        {
            _context.DocumentAuditEvents.Add(new DocumentAuditEvent
            {
                DocumentId = documentId,
                ActorUserId = actorUserId,
                Action = action,
                Outcome = outcome,
                Details = details?.Length > 2000 ? details[..2000] : details,
                CreatedDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not record document audit event {Action}.", action);
        }
    }

    private static bool ValidateRequest(DocumentUploadRequest request, out string? error)
    {
        error = null;
        if (request.FileSizeBytes <= 0 || request.FileSizeBytes > MaxFileSizeBytes)
            error = "Files must be greater than zero and no larger than 25 MB.";
        else if (!ValidateMetadata(request.Title, request.Description, request.Category, request.Tags, out error))
            return false;
        else
        {
            var extension = Path.GetExtension(request.OriginalFileName);
            if (!AllowedContentTypes.TryGetValue(extension, out var expectedType) || !string.Equals(expectedType, request.ContentType, StringComparison.OrdinalIgnoreCase))
                error = "This file type is not supported.";
        }
        return error == null;
    }

    private static bool ValidateMetadata(string title, string? description, string category, string? tags, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 255)
            error = "A title between 1 and 255 characters is required.";
        else if (description?.Length > 2000)
            error = "Description cannot exceed 2000 characters.";
        else if (!Categories.Contains(category.Trim(), StringComparer.OrdinalIgnoreCase))
            error = "Select an approved document category.";
        else if (tags?.Length > 1000)
            error = "Tags cannot exceed 1000 characters.";
        return error == null;
    }

    private static DocumentSummary ToSummary(Document document)
        => new(document.DocumentId, document.Title, document.Category, document.FileSizeBytes, document.ContentType,
            document.UploadedDate, document.ProjectId, document.Project?.Name, document.OriginalFileName);
}