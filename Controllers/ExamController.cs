using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmployeeTrainer.Data;
using EmployeeTrainer.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace EmployeeTrainer.Controllers;

[Authorize]
public class ExamController : Controller
{
    private readonly ApplicationDbContext _context;

    public ExamController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Список доступных экзаменов
    public async Task<IActionResult> Index()
    {
        var exams = await _context.Exams
            .Where(e => e.IsPublished)
            .Include(e => e.Tasks)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
        return View(exams);
    }

    // Прохождение экзамена
    public async Task<IActionResult> Take(int id)
    {
        var exam = await _context.Exams
            .Include(e => e.Tasks)
            .FirstOrDefaultAsync(e => e.Id == id && e.IsPublished);

        if (exam == null) return NotFound();

        var tasks = exam.Tasks.AsEnumerable();
        if (exam.ShuffleQuestions)
        {
            tasks = tasks.OrderBy(t => Guid.NewGuid());
        }
        ViewBag.Exam = exam;
        return View(tasks);
    }

    // Отправка экзамена
    [HttpPost]
    public async Task<IActionResult> Submit(int examId, IFormCollection form)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var answers = new Dictionary<string, string>();
        foreach (var key in form.Keys)
        {
            if (key.StartsWith("answer_"))
            {
                var taskId = key.Replace("answer_", "");
                answers[taskId] = form[key].ToString();
            }
        }

        var submission = new ExamSubmission
        {
            ExamId = examId,
            UserId = userId,
            AnswersJson = JsonSerializer.Serialize(answers),
            SubmittedAt = DateTime.UtcNow
        };

        _context.ExamSubmissions.Add(submission);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Экзамен отправлен на проверку!";
        return RedirectToAction(nameof(Index));
    }
}