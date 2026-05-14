namespace EmployeeTrainer.Models;

public class TestTakeViewModel
{
    public int TestId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int TimeLimitMinutes { get; set; }
    public List<TestQuestionViewModel> Questions { get; set; } = new();
}