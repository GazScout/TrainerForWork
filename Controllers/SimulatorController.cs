using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmployeeTrainer.Data;
using EmployeeTrainer.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace EmployeeTrainer.Controllers;

[Authorize]
public class SimulatorController : Controller
{
    private readonly ApplicationDbContext _context;

    public SimulatorController(ApplicationDbContext context)
    {
        _context = context;
    }

    #region Группы заданий

    public async Task<IActionResult> Index()
    {
        var groups = await _context.TaskGroups
            .Include(g => g.Tasks)
            .Where(g => g.IsPublished)
            .OrderBy(g => g.CreatedAt)
            .ToListAsync();

        ViewBag.UngroupedTasks = await _context.SimulatorTasks
            .Where(t => t.TaskGroupId == null)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();

        return View(groups);
    }

    public async Task<IActionResult> Group(int id)
    {
        var group = await _context.TaskGroups
            .Include(g => g.Tasks)
            .FirstOrDefaultAsync(g => g.Id == id && g.IsPublished);

        return group == null ? NotFound() : View(group);
    }

    #endregion

    #region Маппинг заданий

    private List<SimulatorTaskViewModel> MapTasks(List<SimulatorTask> tasks)
    {
        return tasks.Select(t =>
        {
            var vm = new SimulatorTaskViewModel
            {
                Id = t.Id, Type = t.Type, Title = t.Title, Content = t.Content,
                Explanation = t.Explanation, Hint = t.Hint, ImageUrl = t.ImageUrl
            };

            if (t.Type is TaskType.PhotoSingle or TaskType.PhotoMultiple)
            {
                var parts = t.AnswerJson.Split('|', StringSplitOptions.RemoveEmptyEntries);
                vm.Options = parts.Select(p => p.Trim()).OrderBy(_ => Guid.NewGuid()).ToList();
                var correctAnswers = t.Content.Split('|', StringSplitOptions.RemoveEmptyEntries);
                vm.CorrectIndexes = correctAnswers.Select(c => vm.Options.IndexOf(c.Trim())).Where(i => i >= 0).ToList();
            }
            else if (t.Type == TaskType.PhotoFree)
            {
                vm.FreeAnswerHint = t.AnswerJson;
            }
            else if (t.Type == TaskType.FormTask)
            {
                try
                {
                    vm.FormFields = JsonSerializer.Deserialize<Dictionary<string, string>>(t.Content) ?? new();
                    vm.FormCorrectAnswers = JsonSerializer.Deserialize<Dictionary<string, string>>(t.AnswerJson) ?? new();
                }
                catch { }
            }

            return vm;
        }).ToList();
    }

    #endregion
}