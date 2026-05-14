using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeTrainer.Models;

public class TestResult
{
    public int Id { get; set; }

    public int TestId { get; set; }
    [ForeignKey("TestId")] public Test? Test { get; set; }

    public int UserId { get; set; }
    [ForeignKey("UserId")] public User? User { get; set; }

    public int Score { get; set; }

    public int TotalQuestions { get; set; }

    public int Percentage { get; set; }

    public int TimeSpentSeconds { get; set; }

    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}