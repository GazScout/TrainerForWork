namespace EmployeeTrainer.Models;

public class TestQuestionViewModel
{
    public int Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public List<int> CorrectIndexes { get; set; } = new();
    public bool AllowMultipleCorrect { get; set; } = false;
    public string? CorrectAnswersJson { get; set; }
    public string? Explanation { get; set; }
}