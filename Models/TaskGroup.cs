using System.ComponentModel.DataAnnotations;

namespace EmployeeTrainer.Models;

public class TaskGroup
{
    public int Id { get; set; }
    
    [Required]
    public string Title { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public bool IsPublished { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public List<SimulatorTask> Tasks { get; set; } = new();
}