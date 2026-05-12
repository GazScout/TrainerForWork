using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmployeeTrainer.Data;
using Microsoft.AspNetCore.Authorization;

namespace EmployeeTrainer.Controllers;
[Authorize]
public class HandbookController : Controller
{
    private readonly ApplicationDbContext _context;

    public HandbookController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var articles = string.IsNullOrWhiteSpace(search)
            ? await _context.HandbookArticles.ToListAsync()
            : await _context.HandbookArticles
                .Where(a => a.Title.Contains(search) || 
                           (a.Tags != null && a.Tags.Contains(search)))
                .ToListAsync();

        ViewBag.Search = search;
        return View(articles);
    }

    public async Task<IActionResult> Details(int id)
    {
        var article = await _context.HandbookArticles.FindAsync(id);
        if (article == null)
            return NotFound();
        
        return View(article);
    }
}