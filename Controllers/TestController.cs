using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmployeeTrainer.Data;
using EmployeeTrainer.Models;
using System.Text.Json;
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

    public async Task<IActionResult> Index()
    {
        var tests = await _context.Tests
            .Include(t => t.Questions)
            .ToListAsync();

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var completedTests = await _context.TestResults
            .Where(r => r.UserId == userId)
            .Select(r => r.TestId)
            .ToListAsync();

        ViewBag.CompletedTests = completedTests;
        return View(tests);
    }

    public async Task<IActionResult> Take(int id)
    {
        var test = await _context.Tests
            .Include(t => t.Questions)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (test == null) return NotFound();

        var questions = test.Questions.AsEnumerable();
        if (test.ShuffleQuestions)
        {
            var rng = new Random();
            questions = questions.OrderBy(q => rng.Next());
        }

        var model = new TestTakeViewModel
        {
            TestId = test.Id,
            Title = test.Title,
            Description = test.Description,
            TimeLimitMinutes = test.TimeLimitMinutes,
            Questions = questions.Select(q => new TestQuestionViewModel
            {
                Id = q.Id,
                Question = q.Question,
                Options = string.IsNullOrEmpty(q.OptionsJson)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(q.OptionsJson) ?? new List<string>(),
                CorrectIndexes = string.IsNullOrEmpty(q.CorrectAnswersJson)
                    ? new List<int>()
                    : JsonSerializer.Deserialize<List<int>>(q.CorrectAnswersJson) ?? new List<int>(),
                AllowMultipleCorrect = q.AllowMultipleCorrect,
                CorrectAnswersJson = q.CorrectAnswersJson
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Check(int testId, IFormCollection form, int timeSpentSeconds)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var questions = await _context.TestQuestions
            .Where(q => q.TestId == testId)
            .ToListAsync();

        var answers = new Dictionary<int, List<int>>();
        foreach (var key in form.Keys)
        {
            if (key.StartsWith("answers["))
            {
                var match = System.Text.RegularExpressions.Regex.Match(key, @"answers\[(\d+)\]");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int qId))
                {
                    if (!answers.ContainsKey(qId))
                        answers[qId] = new List<int>();

                    foreach (var val in form[key])
                    {
                        if (int.TryParse(val, out int idx))
                            answers[qId].Add(idx);
                    }
                }
            }
        }

        int totalQuestions = questions.Count;
        int correctAnswers = 0;

        foreach (var question in questions)
        {
            var correctIndexes = string.IsNullOrEmpty(question.CorrectAnswersJson)
                ? new List<int>()
                : JsonSerializer.Deserialize<List<int>>(question.CorrectAnswersJson) ?? new List<int>();
            if (correctIndexes.Count == 0)
            {
                if (!answers.ContainsKey(question.Id) || answers[question.Id].Count == 0)
                    correctAnswers++;
            }
            else if (answers.TryGetValue(question.Id, out var userAnswers))
            {
                if (userAnswers.OrderBy(x => x).SequenceEqual(correctIndexes.OrderBy(x => x)))
                    correctAnswers++;
            }
        }

        int percentage = totalQuestions > 0
            ? (int)Math.Round((double)correctAnswers / totalQuestions * 100)
            : 0;

        var result = new TestResult
        {
            TestId = testId,
            UserId = userId,
            Score = correctAnswers,
            TotalQuestions = totalQuestions,
            Percentage = percentage,
            TimeSpentSeconds = timeSpentSeconds,
            CompletedAt = DateTime.UtcNow
        };

        _context.TestResults.Add(result);
        await _context.SaveChangesAsync();

        ViewBag.TestId = testId;
        ViewBag.CorrectAnswers = correctAnswers;
        ViewBag.TotalQuestions = totalQuestions;
        ViewBag.Percentage = percentage;
        ViewBag.TimeSpent = FormatTime(timeSpentSeconds);

        return View();
    }

    public async Task<IActionResult> MyStats()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var results = await _context.TestResults
            .Include(r => r.Test)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CompletedAt)
            .ToListAsync();

        return View(results);
    }

    private string FormatTime(int seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.TotalMinutes >= 1
            ? $"{(int)ts.TotalMinutes} мин {ts.Seconds} сек"
            : $"{ts.Seconds} сек";
    }
}