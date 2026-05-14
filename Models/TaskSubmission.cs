using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeTrainer.Models;

public class TaskSubmission
{
    public int Id { get; set; }

    public int TaskId { get; set; }
    [ForeignKey("TaskId")] public SimulatorTask? Task { get; set; }

    public int UserId { get; set; }
    [ForeignKey("UserId")] public User? User { get; set; }

    public string? UserAnswers { get; set; }

    public int? Score { get; set; }

    public string? AdminComment { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }

    public int? ReviewedByUserId { get; set; }
}