using Microsoft.EntityFrameworkCore;
using EmployeeTrainer.Models;

namespace EmployeeTrainer.Data;

public class ApplicationDbContext : DbContext
{
    public DbSet<HandbookArticle> HandbookArticles => Set<HandbookArticle>();
    public DbSet<Test> Tests => Set<Test>();
    public DbSet<TestQuestion> TestQuestions => Set<TestQuestion>();
    public DbSet<SimulatorTask> SimulatorTasks => Set<SimulatorTask>();
    public DbSet<User> Users => Set<User>();
    public DbSet<TestResult> TestResults => Set<TestResult>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<TaskGroup> TaskGroups => Set<TaskGroup>();
    public DbSet<TaskSubmission> TaskSubmissions => Set<TaskSubmission>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<ExamTask> ExamTasks => Set<ExamTask>();
    public DbSet<ExamSubmission> ExamSubmissions => Set<ExamSubmission>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Связи
        modelBuilder.Entity<TaskGroup>()
            .HasMany(g => g.Tasks).WithOne(t => t.TaskGroup)
            .HasForeignKey(t => t.TaskGroupId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Test>()
            .HasMany(t => t.Questions).WithOne(q => q.Test)
            .HasForeignKey(q => q.TestId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Exam>()
            .HasMany(e => e.Tasks).WithOne(t => t.Exam)
            .HasForeignKey(t => t.ExamId).OnDelete(DeleteBehavior.Cascade);

        // Начальные данные
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 1, Username = "Gaz",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("GoodLuck"),
            FullName = "Супер-администратор", Role = UserRole.SuperAdmin,
            IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        modelBuilder.Entity<HandbookArticle>().HasData(
            new HandbookArticle { Id = 1, Title = "Как приветствовать клиента", Content = "Установите зрительный контакт, улыбнитесь и скажите: «Добрый день! Меня зовут [Имя]. Чем я могу вам помочь?»", Tags = "клиент,приветствие,общение" },
            new HandbookArticle { Id = 2, Title = "Правила возврата товара", Content = "Возврат возможен в течение 14 дней при наличии чека и сохранении товарного вида. Технически сложные товары — только при обнаружении дефекта.", Tags = "возврат,правила,чек" }
        );

        modelBuilder.Entity<Test>().HasData(new Test
        {
            Id = 1, Title = "Основы работы с клиентами",
            Description = "Базовые правила общения и возврата товаров",
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        modelBuilder.Entity<TestQuestion>().HasData(
            new TestQuestion { Id = 1, TestId = 1, Question = "В течение какого срока возможен возврат товара?", OptionsJson = """["7 дней","14 дней","30 дней","60 дней"]""", CorrectAnswersJson = "[1]", AllowMultipleCorrect = false, Category = "правила" },
            new TestQuestion { Id = 2, TestId = 1, Question = "Какие документы нужны для возврата? (выберите все подходящие)", OptionsJson = """["Чек","Паспорт","Заявление","Упаковка"]""", CorrectAnswersJson = "[0,2]", AllowMultipleCorrect = true, Category = "правила" }
        );
    }
}