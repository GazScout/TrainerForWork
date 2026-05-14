using System.ComponentModel.DataAnnotations;

namespace EmployeeTrainer.Models;

public class Test
{
    public int Id { get; set; }

    [Required] public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int TimeLimitMinutes { get; set; }

    public bool ShuffleQuestions { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<TestQuestion> Questions { get; set; } = new();
}