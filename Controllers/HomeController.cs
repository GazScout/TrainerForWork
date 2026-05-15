using Microsoft.AspNetCore.Mvc;
using EmployeeTrainer.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EmployeeTrainer.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var publishedExams = await _context.Exams.Where(e => e.IsPublished).CountAsync();
            var passedExams = await _context.ExamSubmissions
                .Where(s => s.UserId == userId)
                .Select(s => s.ExamId)
                .Distinct()
                .CountAsync();
            ViewBag.HasNewExams = publishedExams > passedExams;
        }
        return View();
    }

    public IActionResult Privacy() => View();
}