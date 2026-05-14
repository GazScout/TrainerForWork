using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeTrainer.Models;

public enum ReportStatus
{
    New,
    InProgress,
    Resolved
}

public class Report
{
    public int Id { get; set; }

    public int UserId { get; set; }
    [ForeignKey("UserId")] public User? User { get; set; }

    [Required] public string EntityType { get; set; } = string.Empty;

    public int EntityId { get; set; }

    public string? EntityTitle { get; set; }

    [Required] public string Message { get; set; } = string.Empty;

    public ReportStatus Status { get; set; } = ReportStatus.New;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ResolvedAt { get; set; }

    public int? ResolvedByUserId { get; set; }
    [ForeignKey("ResolvedByUserId")] public User? ResolvedBy { get; set; }
}