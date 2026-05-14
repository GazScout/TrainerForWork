using System.ComponentModel.DataAnnotations;

namespace EmployeeTrainer.Models;

public class HandbookArticle
{
    public int Id { get; set; }

    [Required] public string Title { get; set; } = string.Empty;

    [Required] public string Content { get; set; } = string.Empty;

    public string? Tags { get; set; }

    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsNew => (DateTime.UtcNow - CreatedAt).TotalDays < 3;

    public bool IsUpdated => (UpdatedAt - CreatedAt).TotalMinutes > 1;
}