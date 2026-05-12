using System.ComponentModel.DataAnnotations;

namespace EmployeeTrainer.Models;

public class Exam
{
    public int Id { get; set; }
    
    [Required]
    public string Title { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public int TimeLimitMinutes { get; set; } = 0;
    
    public bool IsPublished { get; set; } = false;
    public bool ShuffleQuestions { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public List<ExamTask> Tasks { get; set; } = new();
    public List<ExamSubmission> Submissions { get; set; } = new();
}