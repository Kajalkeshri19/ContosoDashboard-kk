using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public sealed record DocumentQuery(string? Search = null, string? Category = null, int? ProjectId = null, DateTime? FromDate = null, DateTime? ToDate = null, string Sort = "uploadedDate");

public sealed record DocumentSummary(int DocumentId, string Title, string Category, long FileSizeBytes, string ContentType, DateTime UploadedDate, int? ProjectId, string? ProjectName, string OriginalFileName);

public sealed class DocumentUploadRequest
{
    public required Stream Content { get; init; }
    public required string OriginalFileName { get; init; }
    public required string ContentType { get; init; }
    public required long FileSizeBytes { get; init; }
    public required string Title { get; init; }
    public required string Category { get; init; }
    public string? Description { get; init; }
    public string? Tags { get; init; }
    public int? ProjectId { get; init; }
    public int? TaskId { get; init; }
}

public sealed record DocumentUploadResult(string OriginalFileName, bool Succeeded, string? Error, DocumentSummary? Document);

public sealed class DocumentContent : IDisposable
{
    public required Stream Content { get; init; }
    public required string ContentType { get; init; }
    public required string DownloadName { get; init; }
    public void Dispose() => Content.Dispose();
}

public interface IDocumentService
{
    Task<List<DocumentSummary>> GetDocumentsAsync(int userId, DocumentQuery query, CancellationToken cancellationToken = default);
    Task<List<DocumentUploadResult>> UploadFilesAsync(int userId, IEnumerable<DocumentUploadRequest> requests, CancellationToken cancellationToken = default);
    Task<DocumentContent?> GetContentAsync(int documentId, int userId, CancellationToken cancellationToken = default);
    Task<bool> PurgeExpiredAuditEventsAsync(CancellationToken cancellationToken = default);
    Task<bool> UpdateMetadataAsync(int documentId, int userId, string title, string? description, string category, string? tags, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int documentId, int userId, CancellationToken cancellationToken = default);
}