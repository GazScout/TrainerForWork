using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeTrainer.Models;

public class ArticleView
{
    public int Id { get; set; }

    public int ArticleId { get; set; }
    [ForeignKey("ArticleId")] public HandbookArticle? Article { get; set; }

    public int UserId { get; set; }
    [ForeignKey("UserId")] public User? User { get; set; }

    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;

    // Дата последнего обновления статьи на момент просмотра
    public DateTime ArticleUpdatedAt { get; set; }
}