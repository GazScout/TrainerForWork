using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeTrainer.Models;

public class TestQuestion
{
    public int Id { get; set; }

    [Required] public string Question { get; set; } = string.Empty;

    public string? OptionsJson { get; set; }

    public string? CorrectAnswersJson { get; set; }

    public int OrderIndex { get; set; }

    public bool AllowMultipleCorrect { get; set; }

    public int TestId { get; set; }

    [ForeignKey("TestId")] public Test? Test { get; set; }

    public string? Category { get; set; }

    public string? Explanation { get; set; }
}