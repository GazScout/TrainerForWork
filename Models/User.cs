using System.ComponentModel.DataAnnotations;

namespace EmployeeTrainer.Models;

public enum UserRole
{
    Employee = 0,   // Сотрудник
    Admin = 1,      // Админ
    SuperAdmin = 2  // Супер-админ
}

public class User
{
    public int Id { get; set; }
    
    [Required]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    public string? FullName { get; set; }
    
    public UserRole Role { get; set; } = UserRole.Employee;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;
}