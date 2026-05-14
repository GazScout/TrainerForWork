using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeTrainer.Models;

public enum ExamTaskType
{
    Single,
    Multiple,
    Free
}

public class ExamTask
{
    public int Id { get; set; }

    public int ExamId { get; set; }
    [ForeignKey("ExamId")] public Exam? Exam { get; set; }

    public ExamTaskType Type { get; set; }

    [Required] public string Title { get; set; } = string.Empty;

    [Required] public string Question { get; set; } = string.Empty;

    public string? OptionsJson { get; set; }

    public string? CorrectAnswersJson { get; set; }

    public string? ImageUrl { get; set; }

    public int OrderIndex { get; set; }
}