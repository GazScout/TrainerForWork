using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeTrainer.Models;

public class ExamSubmission
{
    public int Id { get; set; }
    
    public int ExamId { get; set; }
    [ForeignKey("ExamId")]
    public Exam? Exam { get; set; }
    
    public int UserId { get; set; }
    [ForeignKey("UserId")]
    public User? User { get; set; }
    
    public string? AnswersJson { get; set; } // {"taskId": "ответ", ...}
    
    public int? Score { get; set; }
    
    public string? AdminComment { get; set; }
    
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ReviewedAt { get; set; }
    
    public int? ReviewedByUserId { get; set; }
}