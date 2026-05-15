using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeTrainer.Models;

public class OrderField
{
    public int Id { get; set; }

    public int TaskId { get; set; }
    [ForeignKey("TaskId")] public SimulatorTask? Task { get; set; }

    public string FieldKey { get; set; } = string.Empty;
    public string Mode { get; set; } = "show"; // show, input, fix
    public string? Value { get; set; }
    public string? CorrectValue { get; set; }
}