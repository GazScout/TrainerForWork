using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmployeeTrainer.Data;
using EmployeeTrainer.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace EmployeeTrainer.Controllers;

[Authorize]
public class ReportController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReportController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Submit(string entityType, int entityId, string entityTitle, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return Json(new { success = false, error = "Опишите проблему" });

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        _context.Reports.Add(new Report
        {
            UserId = userId,
            EntityType = entityType,
            EntityId = entityId,
            EntityTitle = entityTitle,
            Message = message,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }
}