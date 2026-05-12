using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmployeeTrainer.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace EmployeeTrainer.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly ApplicationDbContext _context;

    public ProfileController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        
        var user = await _context.Users.FindAsync(userId);
        var results = await _context.TestResults
            .Include(r => r.Test)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CompletedAt)
            .ToListAsync();

        var totalTests = await _context.Tests.CountAsync();
        var completedTestIds = results.Select(r => r.TestId).Distinct().Count();

        ViewBag.User = user;
        ViewBag.TotalTests = totalTests;
        ViewBag.CompletedTests = completedTestIds;
        ViewBag.AverageScore = results.Any() ? (int)results.Average(r => r.Percentage) : 0;
        ViewBag.BestScore = results.Any() ? results.Max(r => r.Percentage) : 0;
        ViewBag.TotalAttempts = results.Count;
        ViewBag.ExamResults = await _context.ExamSubmissions
    .Include(s => s.Exam)
    .Where(s => s.UserId == userId)
    .OrderByDescending(s => s.SubmittedAt)
    .ToListAsync();

        return View(results);
    }
}