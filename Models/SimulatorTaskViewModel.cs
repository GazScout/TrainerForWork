namespace EmployeeTrainer.Models;

public class SimulatorTaskViewModel
{
    public int Id { get; set; }
    public TaskType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public List<int> CorrectIndexes { get; set; } = new();
    public string? FreeAnswerHint { get; set; }
    public Dictionary<string, string> FormFields { get; set; } = new();
    public Dictionary<string, string> FormCorrectAnswers { get; set; } = new();
    public string? Explanation { get; set; }
    public string? Hint { get; set; }
    public string? ImageUrl { get; set; }
}