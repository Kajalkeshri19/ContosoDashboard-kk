using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add authentication state provider for Blazor
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// Configure Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Mock Authentication (Cookie-based for training purposes)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Add authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Employee", policy => policy.RequireRole("Employee", "TeamLead", "ProjectManager", "Administrator"));
    options.AddPolicy("TeamLead", policy => policy.RequireRole("TeamLead", "ProjectManager", "Administrator"));
    options.AddPolicy("ProjectManager", policy => policy.RequireRole("ProjectManager", "Administrator"));
    options.AddPolicy("Administrator", policy => policy.RequireRole("Administrator"));
});

// Register application services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IMalwareScanner, ConfiguredMalwareScanner>();
builder.Services.AddScoped<DocumentAuthorizationService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();

// Add HttpContextAccessor for accessing user claims
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated(); // For development - use migrations in production
        EnsureDocumentSchema(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    // Use HSTS even in development for training purposes
    app.UseHsts();
}

// Add security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    
    // Content Security Policy for Blazor Server
    context.Response.Headers["Content-Security-Policy"] = 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "font-src 'self' https://cdn.jsdelivr.net; " +
        "img-src 'self' data: https:; " +
        "connect-src 'self' wss: ws:;";
    
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Enable authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/documents/{documentId:int}/content", async (
    int documentId,
    HttpContext httpContext,
    IDocumentService documentService,
    CancellationToken cancellationToken) =>
{
    if (!httpContext.User.Identity?.IsAuthenticated ?? true)
        return Results.Unauthorized();

    var userIdClaim = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!int.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();

    var content = await documentService.GetContentAsync(documentId, userId, cancellationToken);
    if (content == null)
        return Results.NotFound();

    var inline = string.Equals(httpContext.Request.Query["disposition"], "inline", StringComparison.OrdinalIgnoreCase);
    return Results.File(content.Content, content.ContentType, inline ? null : content.DownloadName, enableRangeProcessing: true);
});

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

static void EnsureDocumentSchema(ApplicationDbContext context)
{
    context.Database.ExecuteSqlRaw(@"
IF OBJECT_ID(N'[Documents]', N'U') IS NULL
BEGIN
    CREATE TABLE [Documents] (
        [DocumentId] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Documents] PRIMARY KEY,
        [Title] nvarchar(255) NOT NULL,
        [Description] nvarchar(2000) NULL,
        [Category] nvarchar(100) NOT NULL,
        [Tags] nvarchar(1000) NULL,
        [OriginalFileName] nvarchar(255) NOT NULL,
        [StorageKey] nvarchar(500) NOT NULL,
        [ContentType] nvarchar(255) NOT NULL,
        [FileExtension] nvarchar(20) NOT NULL,
        [FileSizeBytes] bigint NOT NULL,
        [UploadedDate] datetime2 NOT NULL,
        [UploadedByUserId] int NOT NULL,
        [ProjectId] int NULL,
        [TaskId] int NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedDate] datetime2 NULL,
        CONSTRAINT [FK_Documents_Users_UploadedByUserId] FOREIGN KEY ([UploadedByUserId]) REFERENCES [Users] ([UserId]),
        CONSTRAINT [FK_Documents_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([ProjectId]),
        CONSTRAINT [FK_Documents_Tasks_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [Tasks] ([TaskId])
    );
END;
IF OBJECT_ID(N'[DocumentShares]', N'U') IS NULL
BEGIN
    CREATE TABLE [DocumentShares] (
        [DocumentShareId] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_DocumentShares] PRIMARY KEY,
        [DocumentId] int NOT NULL,
        [SharedByUserId] int NOT NULL,
        [RecipientUserId] int NULL,
        [ProjectId] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [RevokedDate] datetime2 NULL,
        CONSTRAINT [FK_DocumentShares_Documents_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [Documents] ([DocumentId]) ON DELETE CASCADE,
        CONSTRAINT [FK_DocumentShares_Users_SharedByUserId] FOREIGN KEY ([SharedByUserId]) REFERENCES [Users] ([UserId]),
        CONSTRAINT [FK_DocumentShares_Users_RecipientUserId] FOREIGN KEY ([RecipientUserId]) REFERENCES [Users] ([UserId]),
        CONSTRAINT [FK_DocumentShares_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([ProjectId])
    );
END;
IF OBJECT_ID(N'[DocumentAuditEvents]', N'U') IS NULL
BEGIN
    CREATE TABLE [DocumentAuditEvents] (
        [DocumentAuditEventId] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_DocumentAuditEvents] PRIMARY KEY,
        [DocumentId] int NULL,
        [ActorUserId] int NOT NULL,
        [Action] nvarchar(50) NOT NULL,
        [Outcome] nvarchar(20) NOT NULL,
        [Details] nvarchar(2000) NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [FK_DocumentAuditEvents_Documents_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [Documents] ([DocumentId]) ON DELETE SET NULL,
        CONSTRAINT [FK_DocumentAuditEvents_Users_ActorUserId] FOREIGN KEY ([ActorUserId]) REFERENCES [Users] ([UserId])
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Documents_StorageKey' AND object_id = OBJECT_ID(N'[Documents]')) CREATE UNIQUE INDEX [IX_Documents_StorageKey] ON [Documents] ([StorageKey]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Documents_UploadedByUserId_UploadedDate' AND object_id = OBJECT_ID(N'[Documents]')) CREATE INDEX [IX_Documents_UploadedByUserId_UploadedDate] ON [Documents] ([UploadedByUserId], [UploadedDate]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Documents_ProjectId_IsDeleted' AND object_id = OBJECT_ID(N'[Documents]')) CREATE INDEX [IX_Documents_ProjectId_IsDeleted] ON [Documents] ([ProjectId], [IsDeleted]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Documents_Category_UploadedDate' AND object_id = OBJECT_ID(N'[Documents]')) CREATE INDEX [IX_Documents_Category_UploadedDate] ON [Documents] ([Category], [UploadedDate]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DocumentAuditEvents_CreatedDate' AND object_id = OBJECT_ID(N'[DocumentAuditEvents]')) CREATE INDEX [IX_DocumentAuditEvents_CreatedDate] ON [DocumentAuditEvents] ([CreatedDate]);");
}
