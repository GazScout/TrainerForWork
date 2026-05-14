using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmployeeTrainer.Data;
using EmployeeTrainer.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

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

        var viewedArticles = await _context.ArticleViews
            .Where(v => v.UserId == userId)
            .ToListAsync();

        var viewedDict = viewedArticles
            .GroupBy(v => v.ArticleId)
            .ToDictionary(g => g.Key, g => g.Max(v => v.ArticleUpdatedAt));

        var model = await articles.OrderByDescending(a => a.CreatedAt).ToListAsync();

        var allTags = await _context.HandbookArticles.Where(a => a.Tags != null).Select(a => a.Tags).ToListAsync();
        ViewBag.Tags = allTags.Where(t => !string.IsNullOrEmpty(t)).SelectMany(t => t!.Split(',')).Select(t => t.Trim()).Distinct().OrderBy(t => t).ToList();
        ViewBag.Search = search;
        ViewBag.ViewedDict = viewedDict;

        return View(model);
    }

    #endregion

    #region Детали статьи

    public async Task<IActionResult> Details(int id)
    {
        var article = await _context.HandbookArticles.FindAsync(id);
        if (article == null) return NotFound();

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var oldViews = await _context.ArticleViews
            .Where(v => v.ArticleId == id && v.UserId == userId)
            .ToListAsync();
        _context.ArticleViews.RemoveRange(oldViews);

        _context.ArticleViews.Add(new ArticleView
        {
            ArticleId = id,
            UserId = userId,
            ViewedAt = DateTime.UtcNow,
            ArticleUpdatedAt = article.UpdatedAt
        });

        await _context.SaveChangesAsync();
        return View(article);
    }

    #endregion
}