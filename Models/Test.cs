using System.ComponentModel.DataAnnotations;

namespace EmployeeTrainer.Models;

public class Test
{
    public int Id { get; set; }
    
    [Required]
    public string Title { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public int TimeLimitMinutes { get; set; } = 0;
    
    public bool ShuffleQuestions { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public List<TestQuestion> Questions { get; set; } = new();
}