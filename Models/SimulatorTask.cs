using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeTrainer.Models;

public enum TaskType
{
    PhotoSingle,
    PhotoMultiple,
    PhotoFree,
    FormTask
}

public class SimulatorTask
{
    public int Id { get; set; }

    public TaskType Type { get; set; }

    [Required] public string Title { get; set; } = string.Empty;

    [Required] public string Content { get; set; } = string.Empty;

    [Required] public string AnswerJson { get; set; } = string.Empty;

    public string? Explanation { get; set; }

    public string? Hint { get; set; }

    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? TaskGroupId { get; set; }

    [ForeignKey("TaskGroupId")] public TaskGroup? TaskGroup { get; set; }
}