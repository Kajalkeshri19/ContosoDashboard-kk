using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Services;

public sealed class DocumentAuthorizationService
{
    private readonly ApplicationDbContext _context;

    public DocumentAuthorizationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> CanViewAsync(Document document, int userId)
    {
        if (document.IsDeleted)
            return false;

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
            return false;
        if (user.Role == UserRole.Administrator || document.UploadedByUserId == userId)
            return true;

        if (document.ProjectId.HasValue)
        {
            var isProjectMember = await _context.ProjectMembers.AnyAsync(pm => pm.ProjectId == document.ProjectId && pm.UserId == userId);
            var isProjectManager = await _context.Projects.AnyAsync(p => p.ProjectId == document.ProjectId && p.ProjectManagerId == userId);
            if (isProjectMember || isProjectManager)
                return true;
        }

        return await _context.DocumentShares.AnyAsync(share =>
            share.DocumentId == document.DocumentId && share.RevokedDate == null &&
            (share.RecipientUserId == userId ||
             (share.ProjectId.HasValue && _context.ProjectMembers.Any(pm => pm.ProjectId == share.ProjectId && pm.UserId == userId))));
    }

    public async Task<bool> CanManageAsync(Document document, int userId)
    {
        if (document.IsDeleted)
            return false;

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
            return false;
        if (user.Role == UserRole.Administrator || document.UploadedByUserId == userId)
            return true;
        if (!document.ProjectId.HasValue)
            return false;

        if (await _context.Projects.AnyAsync(p => p.ProjectId == document.ProjectId && p.ProjectManagerId == userId))
            return true;

        return user.Role == UserRole.TeamLead && await _context.ProjectMembers.AnyAsync(pm =>
            pm.ProjectId == document.ProjectId && pm.UserId == userId && pm.Role == "TeamLead");
    }

    public async Task<bool> CanUploadToProjectAsync(int userId, int? projectId, int? taskId)
    {
        if (taskId.HasValue && (!projectId.HasValue || !await _context.Tasks.AnyAsync(t => t.TaskId == taskId && t.ProjectId == projectId)))
            return false;
        if (!projectId.HasValue)
            return true;

        return await _context.Projects.AnyAsync(p => p.ProjectId == projectId && p.ProjectManagerId == userId) ||
            await _context.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
    }
}