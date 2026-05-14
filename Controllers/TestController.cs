using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmployeeTrainer.Data;
using EmployeeTrainer.Models;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace EmployeeTrainer.Controllers;

[Authorize]
public class TestController : Controller
{
    private readonly ApplicationDbContext _context;

    public TestController(ApplicationDbContext context)
    {
        _context = context;
    }

    #region Список тестов

    public async Task<IActionResult> Index()
    {
        var tests = await _context.Tests.Include(t => t.Questions).ToListAsync();
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        ViewBag.CompletedTests = await _context.TestResults
            .Where(r => r.UserId == userId).Select(r => r.TestId).ToListAsync();
        return View(tests);
    }

    #endregion

    #region Прохождение теста

    public async Task<IActionResult> Take(int id)
    {
        var test = await _context.Tests.Include(t => t.Questions).FirstOrDefaultAsync(t => t.Id == id);
        if (test == null) return NotFound();

        var questions = test.Questions.AsEnumerable();
        if (test.ShuffleQuestions) questions = questions.OrderBy(q => new Random().Next());

        return View(new TestTakeViewModel
        {
            TestId = test.Id, Title = test.Title, Description = test.Description,
            TimeLimitMinutes = test.TimeLimitMinutes,
            Questions = questions.Select(q => new TestQuestionViewModel
            {
                Id = q.Id, Question = q.Question,
                Options = string.IsNullOrEmpty(q.OptionsJson) ? new() : JsonSerializer.Deserialize<List<string>>(q.OptionsJson) ?? new(),
                CorrectIndexes = string.IsNullOrEmpty(q.CorrectAnswersJson) ? new() : JsonSerializer.Deserialize<List<int>>(q.CorrectAnswersJson) ?? new(),
                AllowMultipleCorrect = q.AllowMultipleCorrect, CorrectAnswersJson = q.CorrectAnswersJson
            }).ToList()
        });
    }

    #endregion

    #region Проверка ответов

    [HttpPost]
    public async Task<IActionResult> Check(int testId, IFormCollection form, int timeSpentSeconds)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var questions = await _context.TestQuestions.Where(q => q.TestId == testId).ToListAsync();

        var answers = new Dictionary<int, List<int>>();
        foreach (var key in form.Keys)
        {
            var match = Regex.Match(key, @"answers\[(\d+)\]");
            if (!match.Success || !int.TryParse(match.Groups[1].Value, out int qId)) continue;
            if (!answers.ContainsKey(qId)) answers[qId] = new();
            foreach (var val in form[key])
                if (int.TryParse(val, out int idx)) answers[qId].Add(idx);
        }

        int correct = 0;
        foreach (var q in questions)
        {
            var correctIdx = string.IsNullOrEmpty(q.CorrectAnswersJson) ? new()
                : JsonSerializer.Deserialize<List<int>>(q.CorrectAnswersJson) ?? new();

            if (correctIdx.Count == 0)
            {
                if (!answers.ContainsKey(q.Id) || answers[q.Id].Count == 0) correct++;
            }
            else if (answers.TryGetValue(q.Id, out var userAns))
            {
                if (userAns.OrderBy(x => x).SequenceEqual(correctIdx.OrderBy(x => x))) correct++;
            }
        }

        int pct = questions.Count > 0 ? (int)Math.Round((double)correct / questions.Count * 100) : 0;

        _context.TestResults.Add(new TestResult
        {
            TestId = testId, UserId = userId, Score = correct,
            TotalQuestions = questions.Count, Percentage = pct,
            TimeSpentSeconds = timeSpentSeconds, CompletedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        ViewBag.TestId = testId; ViewBag.CorrectAnswers = correct;
        ViewBag.TotalQuestions = questions.Count; ViewBag.Percentage = pct;
        ViewBag.TimeSpent = FormatTime(timeSpentSeconds);
        return View();
    }

    #endregion

    #region Статистика

    public async Task<IActionResult> MyStats()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return View(await _context.TestResults.Include(r => r.Test)
            .Where(r => r.UserId == userId).OrderByDescending(r => r.CompletedAt).ToListAsync());
    }

    #endregion

    #region Вспомогательные

    private string FormatTime(int seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.TotalMinutes >= 1 ? $"{(int)ts.TotalMinutes} мин {ts.Seconds} сек" : $"{ts.Seconds} сек";
    }

    #endregion
}