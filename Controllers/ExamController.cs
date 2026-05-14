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

    #region Список экзаменов

    public async Task<IActionResult> Index()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var exams = await _context.Exams
            .Where(e => e.IsPublished)
            .Include(e => e.Tasks)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        var completedExams = await _context.ExamSubmissions
            .Where(s => s.UserId == userId)
            .Select(s => s.ExamId)
            .Distinct()
            .ToListAsync();

        ViewBag.CompletedExams = completedExams;
        return View(exams);
    }

    #endregion

    #region Прохождение экзамена

    public async Task<IActionResult> Take(int id)
    {
        var exam = await _context.Exams
            .Include(e => e.Tasks)
            .FirstOrDefaultAsync(e => e.Id == id && e.IsPublished);

        if (exam == null) return NotFound();

        var tasks = exam.Tasks.AsEnumerable();
        if (exam.ShuffleQuestions)
            tasks = tasks.OrderBy(t => Guid.NewGuid());

        ViewBag.Exam = exam;
        return View(tasks.ToList());
    }

    #endregion

    #region Отправка на проверку

    [HttpPost]
    public async Task<IActionResult> Submit(int examId, IFormCollection form)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var answers = new Dictionary<string, string>();
        foreach (var key in form.Keys)
        {
            if (key.StartsWith("answer_"))
                answers[key.Replace("answer_", "")] = form[key].ToString();
        }

        _context.ExamSubmissions.Add(new ExamSubmission
        {
            ExamId = examId,
            UserId = userId,
            AnswersJson = JsonSerializer.Serialize(answers),
            SubmittedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        TempData["Success"] = "Экзамен отправлен на проверку!";
        return RedirectToAction(nameof(Index));
    }

    #endregion
}