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

    #region Список с поиском и фильтрацией

    public async Task<IActionResult> Index(string? search, string? tags)
    {
        var articles = _context.HandbookArticles.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            articles = articles.Where(a => a.Title.Contains(term) || (a.Tags != null && a.Tags.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(tags))
        {
            var tagList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();
            foreach (var tag in tagList)
                articles = articles.Where(a => a.Tags != null && a.Tags.Contains(tag));
        }

        var allTags = await _context.HandbookArticles.Where(a => a.Tags != null).Select(a => a.Tags).ToListAsync();
        var uniqueTags = allTags
            .Where(t => !string.IsNullOrEmpty(t))
            .SelectMany(t => t!.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Select(t => t.Trim()).Distinct().OrderBy(t => t).ToList();

        ViewBag.Search = search;
        ViewBag.Tags = uniqueTags;
        return View(await articles.OrderByDescending(a => a.CreatedAt).ToListAsync());
    }

    #endregion

    #region Детали статьи

    public async Task<IActionResult> Details(int id)
    {
        var article = await _context.HandbookArticles.FindAsync(id);
        return article == null ? NotFound() : View(article);
    }

    #endregion
}