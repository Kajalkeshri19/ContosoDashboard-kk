using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class DocumentShare
{
    [Key]
    public int DocumentShareId { get; set; }

    public int DocumentId { get; set; }
    public int SharedByUserId { get; set; }
    public int? RecipientUserId { get; set; }
    public int? ProjectId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedDate { get; set; }

    [ForeignKey(nameof(DocumentId))]
    public virtual Document Document { get; set; } = null!;

    [ForeignKey(nameof(SharedByUserId))]
    public virtual User SharedByUser { get; set; } = null!;

    [ForeignKey(nameof(RecipientUserId))]
    public virtual User? RecipientUser { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public virtual Project? Project { get; set; }
}