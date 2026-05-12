using System.ComponentModel.DataAnnotations;

namespace EmployeeTrainer.Models;

public class HandbookArticle
{
    public int Id { get; set; }
    
    [Required]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public string? Tags { get; set; } // через запятую: "клиент,возврат,правила"
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}