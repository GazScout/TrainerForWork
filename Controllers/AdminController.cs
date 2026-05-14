using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmployeeTrainer.Data;
using EmployeeTrainer.Models;
using EmployeeTrainer.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;
using ClosedXML.Excel;

namespace EmployeeTrainer.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AuditService _audit;

    public AdminController(ApplicationDbContext context, AuditService audit)
    {
        _context = context;
        _audit = audit;
    }

    #region Вспомогательные методы

    private async Task LogAudit(string action, string entityType, int? entityId = null, string? details = null)
    {
        var logEntry = _audit.CreateLogEntry(action, entityType, entityId, details);
        _context.AuditLogs.Add(logEntry);
        await _context.SaveChangesAsync();
    }

    #endregion

    #region Дашборд

    public async Task<IActionResult> Index()
    {
        ViewBag.TotalUsers = await _context.Users.CountAsync();
        ViewBag.ActiveUsers = await _context.Users.CountAsync(u => u.IsActive);
        ViewBag.TotalTests = await _context.Tests.CountAsync();
        ViewBag.TotalQuestions = await _context.TestQuestions.CountAsync();
        ViewBag.TotalExams = await _context.Exams.CountAsync();
        ViewBag.TotalTasks = await _context.SimulatorTasks.CountAsync();
        ViewBag.TotalArticles = await _context.HandbookArticles.CountAsync();
        ViewBag.PendingReports = await _context.Reports.CountAsync(r => r.Status == ReportStatus.New);
        ViewBag.PendingExams = await _context.ExamSubmissions.CountAsync(s => s.Score == null);
        ViewBag.PendingTasks = await _context.TaskSubmissions.CountAsync(s => s.Score == null);
        ViewBag.RecentResults = await _context.TestResults
            .Include(r => r.User).Include(r => r.Test)
            .OrderByDescending(r => r.CompletedAt).Take(5).ToListAsync();
        ViewBag.AvgScore = await _context.TestResults.AnyAsync()
            ? (int)await _context.TestResults.AverageAsync(r => r.Percentage) : 0;
        ViewBag.TopUsers = await _context.TestResults
            .Include(r => r.User).GroupBy(r => r.UserId)
            .Select(g => new { User = g.First().User, AvgScore = (int)g.Average(r => r.Percentage), TestsPassed = g.Count() })
            .OrderByDescending(x => x.AvgScore).Take(5).ToListAsync();
        return View();
    }

    #endregion

    #region Справочник

    public async Task<IActionResult> Articles(string? search)
    {
        var query = _context.HandbookArticles.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => a.Title.Contains(search) || (a.Tags != null && a.Tags.Contains(search)));
        return View(await query.OrderByDescending(a => a.CreatedAt).ToListAsync());
    }

    public IActionResult CreateArticle() => View();

    [HttpPost]
    public async Task<IActionResult> CreateArticle(HandbookArticle article)
    {
        if (ModelState.IsValid)
        {
            article.CreatedAt = DateTime.UtcNow;
            _context.HandbookArticles.Add(article);
            await _context.SaveChangesAsync();
            await LogAudit("Create", "Article", article.Id, $"Статья: {article.Title}");
            return RedirectToAction(nameof(Articles));
        }
        return View(article);
    }

    public async Task<IActionResult> EditArticle(int id)
    {
        var article = await _context.HandbookArticles.FindAsync(id);
        return article == null ? NotFound() : View(article);
    }

    [HttpPost]
    public async Task<IActionResult> EditArticle(int id, HandbookArticle article)
    {
        if (id != article.Id) return NotFound();
        if (ModelState.IsValid)
        {
            var existing = await _context.HandbookArticles.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Title = article.Title;
            existing.Content = article.Content;
            existing.Tags = article.Tags;
            existing.ImageUrl = article.ImageUrl;
            existing.UpdatedAt = DateTime.UtcNow;  // ← обязательно

            await _context.SaveChangesAsync();
            await LogAudit("Update", "Article", id, $"Статья: {article.Title}");
            return RedirectToAction(nameof(Articles));
        }
        return View(article);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteArticle(int id)
    {
        var article = await _context.HandbookArticles.FindAsync(id);
        if (article != null)
        {
            _context.HandbookArticles.Remove(article);
            await _context.SaveChangesAsync();
            await LogAudit("Delete", "Article", id, "Удалена статья");
        }
        return RedirectToAction(nameof(Articles));
    }

    #endregion

    #region Тесты

    public async Task<IActionResult> Tests(string? search)
    {
        var query = _context.Tests.Include(t => t.Questions).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(t => t.Title.Contains(search));
        return View(await query.OrderByDescending(t => t.CreatedAt).ToListAsync());
    }

    public IActionResult CreateTest() => View();

    [HttpPost]
    public async Task<IActionResult> CreateTest(string title, string? description, int timeLimitMinutes, bool shuffleQuestions)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            var test = new Test { Title = title, Description = description, TimeLimitMinutes = timeLimitMinutes, ShuffleQuestions = shuffleQuestions, CreatedAt = DateTime.UtcNow };
            _context.Tests.Add(test);
            await _context.SaveChangesAsync();
            await LogAudit("Create", "Test", test.Id, $"Тест: {title}");
            return RedirectToAction(nameof(Tests));
        }
        return View();
    }

    public async Task<IActionResult> ManageQuestions(int testId)
    {
        var test = await _context.Tests.Include(t => t.Questions).FirstOrDefaultAsync(t => t.Id == testId);
        return test == null ? NotFound() : View(test);
    }

    [HttpPost]
    public async Task<IActionResult> AddQuestionJson(int testId, string questionText, string OptionsJson, string CorrectAnswersJson, bool AllowMultipleCorrect)
    {
        if (!string.IsNullOrWhiteSpace(questionText) && !string.IsNullOrWhiteSpace(OptionsJson))
        {
            var question = new TestQuestion
            {
                TestId = testId, Question = questionText, OptionsJson = OptionsJson,
                CorrectAnswersJson = CorrectAnswersJson, AllowMultipleCorrect = AllowMultipleCorrect,
                OrderIndex = await _context.TestQuestions.CountAsync(q => q.TestId == testId)
            };
            _context.TestQuestions.Add(question);
            await _context.SaveChangesAsync();
            await LogAudit("Create", "Question", question.Id, $"Вопрос добавлен в тест {testId}");
        }
        return RedirectToAction(nameof(ManageQuestions), new { testId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteQuestionFromTest(int questionId, int testId)
    {
        var question = await _context.TestQuestions.FindAsync(questionId);
        if (question != null)
        {
            _context.TestQuestions.Remove(question);
            await _context.SaveChangesAsync();
            await LogAudit("Delete", "Question", questionId, $"Вопрос удалён из теста {testId}");
        }
        return RedirectToAction(nameof(ManageQuestions), new { testId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteTest(int id)
    {
        var test = await _context.Tests.FindAsync(id);
        if (test != null)
        {
            _context.Tests.Remove(test);
            await _context.SaveChangesAsync();
            await LogAudit("Delete", "Test", id, "Удалён тест");
        }
        return RedirectToAction(nameof(Tests));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateTestSettings(int testId, int timeLimitMinutes, bool shuffleQuestions)
    {
        var test = await _context.Tests.FindAsync(testId);
        if (test == null) return NotFound();
        test.TimeLimitMinutes = timeLimitMinutes;
        test.ShuffleQuestions = shuffleQuestions;
        await _context.SaveChangesAsync();
        await LogAudit("Update", "Test", testId, $"Настройки: время={timeLimitMinutes}мин, перемешивание={shuffleQuestions}");
        TempData["Success"] = $"Настройки сохранены. Перемешивание: {(shuffleQuestions ? "включено" : "выключено")}";
        return RedirectToAction(nameof(ManageQuestions), new { testId });
    }

    [HttpPost]
    public async Task<IActionResult> CopyTest(int id)
    {
        var original = await _context.Tests.Include(t => t.Questions).FirstOrDefaultAsync(t => t.Id == id);
        if (original == null) return NotFound();
        var copy = new Test { Title = $"{original.Title} (Копия)", Description = original.Description, TimeLimitMinutes = original.TimeLimitMinutes, ShuffleQuestions = original.ShuffleQuestions, CreatedAt = DateTime.UtcNow };
        _context.Tests.Add(copy);
        await _context.SaveChangesAsync();
        foreach (var q in original.Questions.OrderBy(q => q.OrderIndex))
            _context.TestQuestions.Add(new TestQuestion { TestId = copy.Id, Question = q.Question, OptionsJson = q.OptionsJson, CorrectAnswersJson = q.CorrectAnswersJson, AllowMultipleCorrect = q.AllowMultipleCorrect, Category = q.Category, OrderIndex = q.OrderIndex });
        await _context.SaveChangesAsync();
        await LogAudit("Create", "Test", copy.Id, $"Скопирован тест {id} → {copy.Id}");
        TempData["Success"] = $"Тест «{original.Title}» скопирован";
        return RedirectToAction(nameof(Tests));
    }

    [AllowAnonymous]
    public async Task<IActionResult> PreviewTest(int id)
    {
        var test = await _context.Tests.Include(t => t.Questions).FirstOrDefaultAsync(t => t.Id == id);
        if (test == null) return NotFound();
        var model = new TestTakeViewModel
        {
            TestId = test.Id, Title = $"[ПРЕДПРОСМОТР] {test.Title}", Description = test.Description, TimeLimitMinutes = 0,
            Questions = test.Questions.OrderBy(q => q.OrderIndex).Select(q => new TestQuestionViewModel
            {
                Id = q.Id, Question = q.Question,
                Options = string.IsNullOrEmpty(q.OptionsJson) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(q.OptionsJson) ?? new List<string>(),
                CorrectIndexes = string.IsNullOrEmpty(q.CorrectAnswersJson) ? new List<int>() : JsonSerializer.Deserialize<List<int>>(q.CorrectAnswersJson) ?? new List<int>(),
                AllowMultipleCorrect = q.AllowMultipleCorrect, CorrectAnswersJson = q.CorrectAnswersJson
            }).ToList()
        };
        ViewBag.IsPreview = true;
        return View("~/Views/Test/Take.cshtml", model);
    }

    [HttpPost]
    public async Task<IActionResult> ReorderQuestions(int testId, List<int> questionIds)
    {
        for (int i = 0; i < questionIds.Count; i++)
        {
            var q = await _context.TestQuestions.FindAsync(questionIds[i]);
            if (q != null && q.TestId == testId) q.OrderIndex = i;
        }
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> EditQuestion(int id)
    {
        var question = await _context.TestQuestions.FindAsync(id);
        if (question == null) return NotFound();
        ViewBag.QuestionId = question.Id;
        ViewBag.QuestionText = question.Question;
        ViewBag.Options = string.IsNullOrEmpty(question.OptionsJson) ? "" : string.Join("\n", JsonSerializer.Deserialize<List<string>>(question.OptionsJson) ?? new List<string>());
        ViewBag.CorrectAnswersJson = question.CorrectAnswersJson ?? "[0]";
        ViewBag.AllowMultipleCorrect = question.AllowMultipleCorrect;
        ViewBag.TestId = question.TestId;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> EditQuestion(int id, string questionText, string OptionsJson, string CorrectAnswersJson, bool AllowMultipleCorrect, int testId)
    {
        var question = await _context.TestQuestions.FindAsync(id);
        if (question == null) return NotFound();
        question.Question = questionText;
        question.OptionsJson = OptionsJson;
        question.CorrectAnswersJson = CorrectAnswersJson;
        question.AllowMultipleCorrect = AllowMultipleCorrect;
        await _context.SaveChangesAsync();
        await LogAudit("Update", "Question", id, "Вопрос изменён");
        return RedirectToAction(nameof(ManageQuestions), new { testId });
    }

    #endregion

    #region Пользователи

    [HttpGet]
    public async Task<IActionResult> Users(string? search)
    {
        var query = _context.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(u => u.Username.Contains(search) || (u.FullName != null && u.FullName.Contains(search)));
        ViewBag.CurrentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        return View(await query.OrderByDescending(u => u.CreatedAt).ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(string username, string password, string? fullName, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        { TempData["Error"] = "Логин и пароль обязательны"; return RedirectToAction(nameof(Users)); }
        if (await _context.Users.AnyAsync(u => u.Username == username))
        { TempData["Error"] = "Пользователь с таким логином уже существует"; return RedirectToAction(nameof(Users)); }
        var user = new User { Username = username, PasswordHash = BCrypt.Net.BCrypt.HashPassword(password), FullName = fullName, Role = role, IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        await LogAudit("Create", "User", user.Id, $"Создан пользователь: {username}, роль: {role}");
        TempData["Success"] = $"Пользователь {username} создан";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    public async Task<IActionResult> ChangeUserRole(int userId, UserRole newRole)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var currentUser = await _context.Users.FindAsync(currentUserId);
        var targetUser = await _context.Users.FindAsync(userId);
        if (targetUser == null) { TempData["Error"] = "Пользователь не найден"; return RedirectToAction(nameof(Users)); }
        if (targetUser.Role == UserRole.SuperAdmin) { TempData["Error"] = "Нельзя изменить роль Супер-админа"; return RedirectToAction(nameof(Users)); }
        if (newRole == UserRole.Admin && currentUser?.Role != UserRole.SuperAdmin) { TempData["Error"] = "Только Супер-админ может назначать администраторов"; return RedirectToAction(nameof(Users)); }
        if (newRole == UserRole.SuperAdmin) { TempData["Error"] = "Невозможно назначить роль Супер-админа"; return RedirectToAction(nameof(Users)); }
        if (userId == currentUserId) { TempData["Error"] = "Нельзя изменить свою собственную роль"; return RedirectToAction(nameof(Users)); }
        targetUser.Role = newRole;
        await _context.SaveChangesAsync();
        await LogAudit("Update", "User", userId, $"Роль изменена на {newRole}");
        TempData["Success"] = $"Роль пользователя {targetUser.Username} изменена";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleUserStatus(int userId)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);
        if (user == null) { TempData["Error"] = "Пользователь не найден"; return RedirectToAction(nameof(Users)); }
        if (user.Role == UserRole.SuperAdmin) { TempData["Error"] = "Нельзя заблокировать Супер-админа"; return RedirectToAction(nameof(Users)); }
        var currentUser = await _context.Users.FindAsync(currentUserId);
        if (user.Role == UserRole.Admin && currentUser?.Role != UserRole.SuperAdmin) { TempData["Error"] = "Только Супер-админ может блокировать администраторов"; return RedirectToAction(nameof(Users)); }
        if (userId == currentUserId) { TempData["Error"] = "Нельзя заблокировать самого себя"; return RedirectToAction(nameof(Users)); }
        user.IsActive = !user.IsActive;
        await _context.SaveChangesAsync();
        await LogAudit("Update", "User", userId, user.IsActive ? "Пользователь разблокирован" : "Пользователь заблокирован");
        TempData["Success"] = user.IsActive ? $"Пользователь {user.Username} разблокирован" : $"Пользователь {user.Username} заблокирован";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);
        if (user == null) { TempData["Error"] = "Пользователь не найден"; return RedirectToAction(nameof(Users)); }
        if (user.Role == UserRole.SuperAdmin) { TempData["Error"] = "Нельзя удалить Супер-админа"; return RedirectToAction(nameof(Users)); }
        var currentUser = await _context.Users.FindAsync(currentUserId);
        if (user.Role == UserRole.Admin && currentUser?.Role != UserRole.SuperAdmin) { TempData["Error"] = "Только Супер-админ может удалять администраторов"; return RedirectToAction(nameof(Users)); }
        if (userId == currentUserId) { TempData["Error"] = "Нельзя удалить самого себя"; return RedirectToAction(nameof(Users)); }
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        await LogAudit("Delete", "User", userId, "Удалён пользователь");
        TempData["Success"] = $"Пользователь {user.Username} удалён";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(int userId, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 3) { TempData["Error"] = "Пароль должен быть не менее 3 символов"; return RedirectToAction(nameof(Users)); }
        var user = await _context.Users.FindAsync(userId);
        if (user == null) { TempData["Error"] = "Пользователь не найден"; return RedirectToAction(nameof(Users)); }
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (user.Role == UserRole.SuperAdmin && currentUserId != userId) { TempData["Error"] = "Нельзя сбросить пароль Супер-админа"; return RedirectToAction(nameof(Users)); }
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _context.SaveChangesAsync();
        await LogAudit("Update", "User", userId, $"Сброшен пароль пользователя {user.Username}");
        TempData["Success"] = $"Пароль пользователя {user.Username} сброшен";
        return RedirectToAction(nameof(Users));
    }

    #endregion

    #region Статистика и журнал

    public async Task<IActionResult> TestStats()
    {
        var results = await _context.TestResults.Include(r => r.Test).Include(r => r.User)
            .OrderByDescending(r => r.CompletedAt).Take(100).ToListAsync();
        return View(results);
    }

    public async Task<IActionResult> AuditLog(string? logAction, string? entityType, string? search, int page = 1)
    {
        var query = _context.AuditLogs.Include(l => l.User).AsQueryable();
        if (!string.IsNullOrWhiteSpace(logAction)) query = query.Where(l => l.Action == logAction);
        if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(l => l.EntityType == entityType);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(l => l.Details != null && l.Details.Contains(search));
        var totalItems = await query.CountAsync();
        var pageSize = 20;
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        if (page < 1) page = 1;
        if (page > totalPages && totalPages > 0) page = totalPages;
        var logs = await query.OrderByDescending(l => l.Timestamp).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        ViewBag.LogAction = logAction; ViewBag.EntityType = entityType; ViewBag.Search = search;
        ViewBag.CurrentPage = page; ViewBag.TotalPages = totalPages; ViewBag.TotalItems = totalItems;
        return View(logs);
    }

    #endregion

    #region Тренажёр

    public async Task<IActionResult> Tasks(int? groupId, string? search)
    {
        var query = _context.SimulatorTasks.Include(t => t.TaskGroup).AsQueryable();
        if (groupId.HasValue) query = query.Where(t => t.TaskGroupId == groupId.Value);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(t => t.Title.Contains(search) || t.Content.Contains(search));
        ViewBag.Groups = await _context.TaskGroups.OrderBy(g => g.Title).ToListAsync();
        ViewBag.SelectedGroupId = groupId;
        return View(await query.OrderByDescending(t => t.CreatedAt).ToListAsync());
    }

    public async Task<IActionResult> CreateTask() { ViewBag.TaskGroups = await _context.TaskGroups.OrderBy(g => g.Title).ToListAsync(); return View(); }

    [HttpPost]
    public async Task<IActionResult> CreateTask(SimulatorTask task)
    {
        task.CreatedAt = DateTime.UtcNow;
        if (task.TaskGroupId == 0) task.TaskGroupId = null;
        _context.SimulatorTasks.Add(task);
        await _context.SaveChangesAsync();
        await LogAudit("Create", "Task", task.Id, $"Задание: {task.Title}");
        return RedirectToAction(nameof(Tasks));
    }

    public async Task<IActionResult> EditTask(int id)
    {
        var task = await _context.SimulatorTasks.FindAsync(id);
        if (task == null) return NotFound();
        ViewBag.TaskGroups = await _context.TaskGroups.OrderBy(g => g.Title).ToListAsync();
        return View(task);
    }

    [HttpPost]
    public async Task<IActionResult> EditTask(int id, SimulatorTask task)
    {
        if (id != task.Id) return NotFound();
        var existing = await _context.SimulatorTasks.FindAsync(id);
        if (existing == null) return NotFound();
        existing.Title = task.Title; existing.Type = task.Type; existing.Content = task.Content;
        existing.AnswerJson = task.AnswerJson; existing.Explanation = task.Explanation;
        existing.Hint = task.Hint; existing.ImageUrl = task.ImageUrl;
        existing.TaskGroupId = task.TaskGroupId == 0 ? null : task.TaskGroupId;
        await _context.SaveChangesAsync();
        await LogAudit("Update", "Task", id, $"Задание: {task.Title}");
        return RedirectToAction(nameof(Tasks));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var task = await _context.SimulatorTasks.FindAsync(id);
        if (task != null) { _context.SimulatorTasks.Remove(task); await _context.SaveChangesAsync(); await LogAudit("Delete", "Task", id, "Удалено задание"); }
        return RedirectToAction(nameof(Tasks));
    }

    [HttpPost]
    public async Task<IActionResult> UploadImage(IFormFile image)
    {
        if (image == null || image.Length == 0) return Json(new { success = false, error = "Файл не выбран" });
        var allowed = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowed.Contains(image.ContentType)) return Json(new { success = false, error = "Только JPEG, PNG, GIF, WebP" });
        if (image.Length > 5 * 1024 * 1024) return Json(new { success = false, error = "Максимум 5 МБ" });
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create)) await image.CopyToAsync(stream);
        return Json(new { success = true, url = $"/uploads/{fileName}" });
    }

    public async Task<IActionResult> TaskGroups()
    {
        return View(await _context.TaskGroups.Include(g => g.Tasks).OrderByDescending(g => g.CreatedAt).ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> CreateTaskGroup(string title, string? description)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            var group = new TaskGroup { Title = title, Description = description, CreatedAt = DateTime.UtcNow };
            _context.TaskGroups.Add(group);
            await _context.SaveChangesAsync();
            await LogAudit("Create", "TaskGroup", group.Id, $"Группа: {title}");
        }
        return RedirectToAction(nameof(TaskGroups));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteTaskGroup(int id)
    {
        var group = await _context.TaskGroups.FindAsync(id);
        if (group != null) { _context.TaskGroups.Remove(group); await _context.SaveChangesAsync(); await LogAudit("Delete", "TaskGroup", id, "Удалена группа"); }
        return RedirectToAction(nameof(TaskGroups));
    }

    [HttpPost]
    public async Task<IActionResult> TogglePublishTaskGroup(int id)
    {
        var group = await _context.TaskGroups.Include(g => g.Tasks).FirstOrDefaultAsync(g => g.Id == id);
        if (group == null) return NotFound();
        if (!group.IsPublished && !group.Tasks.Any())
        { TempData["Error"] = $"Нельзя опубликовать группу «{group.Title}» — в ней нет заданий"; return RedirectToAction(nameof(TaskGroups)); }
        group.IsPublished = !group.IsPublished;
        await _context.SaveChangesAsync();
        await LogAudit("Update", "TaskGroup", id, group.IsPublished ? "Опубликована" : "Скрыта");
        TempData["Success"] = group.IsPublished ? $"Группа «{group.Title}» опубликована" : $"Группа «{group.Title}» скрыта";
        return RedirectToAction(nameof(TaskGroups));
    }

    public async Task<IActionResult> ReviewSubmissions()
    {
        var submissions = await _context.TaskSubmissions.Include(s => s.Task).Include(s => s.User)
            .OrderByDescending(s => s.SubmittedAt).ToListAsync();
        return View(submissions);
    }

    #endregion

    #region Репорты

    public async Task<IActionResult> Reports()
    {
        var reports = await _context.Reports.Include(r => r.User).Include(r => r.ResolvedBy)
            .OrderByDescending(r => r.CreatedAt).ToListAsync();
        return View(reports);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateReportStatus(int reportId, ReportStatus status)
    {
        var report = await _context.Reports.FindAsync(reportId);
        if (report == null) return NotFound();
        var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        report.Status = status;
        if (status == ReportStatus.Resolved) { report.ResolvedAt = DateTime.UtcNow; report.ResolvedByUserId = adminId; }
        await _context.SaveChangesAsync();
        await LogAudit("Update", "Report", reportId, $"Статус: {status}");
        return RedirectToAction(nameof(Reports));
    }

    #endregion

    #region Экзамены

    public async Task<IActionResult> Exams()
    {
        return View(await _context.Exams.Include(e => e.Tasks).OrderByDescending(e => e.CreatedAt).ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> CreateExam(string title, string? description, int timeLimitMinutes)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            var exam = new Exam { Title = title, Description = description, TimeLimitMinutes = timeLimitMinutes, CreatedAt = DateTime.UtcNow };
            _context.Exams.Add(exam);
            await _context.SaveChangesAsync();
            await LogAudit("Create", "Exam", exam.Id, $"Экзамен: {title}");
        }
        return RedirectToAction(nameof(Exams));
    }

    [HttpPost]
    public async Task<IActionResult> TogglePublishExam(int id)
    {
        var exam = await _context.Exams.Include(e => e.Tasks).FirstOrDefaultAsync(e => e.Id == id);
        if (exam == null) return NotFound();
        if (!exam.IsPublished && !exam.Tasks.Any())
        { TempData["Error"] = "Добавьте хотя бы одно задание"; return RedirectToAction(nameof(Exams)); }
        exam.IsPublished = !exam.IsPublished;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Exams));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteExam(int id)
    {
        var exam = await _context.Exams.FindAsync(id);
        if (exam != null) { _context.Exams.Remove(exam); await _context.SaveChangesAsync(); }
        return RedirectToAction(nameof(Exams));
    }

    public async Task<IActionResult> ManageExamTasks(int examId)
    {
        var exam = await _context.Exams.Include(e => e.Tasks).FirstOrDefaultAsync(e => e.Id == examId);
        return exam == null ? NotFound() : View(exam);
    }

    [HttpPost]
    public async Task<IActionResult> AddExamTask(int examId, string title, string question, int type, string optionsJson, string correctJson, string? imageUrl)
    {
        var task = new ExamTask { ExamId = examId, Title = title, Question = question, Type = (ExamTaskType)type, OptionsJson = optionsJson, CorrectAnswersJson = correctJson, ImageUrl = imageUrl, OrderIndex = await _context.ExamTasks.CountAsync(t => t.ExamId == examId) };
        _context.ExamTasks.Add(task);
        await _context.SaveChangesAsync();
        await LogAudit("Create", "ExamTask", task.Id, $"Задание добавлено в экзамен {examId}");
        return RedirectToAction(nameof(ManageExamTasks), new { examId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteExamTask(int id, int examId)
    {
        var task = await _context.ExamTasks.FindAsync(id);
        if (task != null) { _context.ExamTasks.Remove(task); await _context.SaveChangesAsync(); }
        return RedirectToAction(nameof(ManageExamTasks), new { examId });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateExamSettings(int examId, string title, string description, int timeLimitMinutes, bool shuffleQuestions)
    {
        var exam = await _context.Exams.FindAsync(examId);
        if (exam == null) return NotFound();
        exam.Title = title; exam.Description = description;
        exam.TimeLimitMinutes = Math.Min(timeLimitMinutes, 180);
        exam.ShuffleQuestions = shuffleQuestions;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Настройки сохранены";
        return RedirectToAction(nameof(ManageExamTasks), new { examId });
    }

    public async Task<IActionResult> ReviewExams(bool showAll = false)
    {
        var query = _context.ExamSubmissions
            .Include(s => s.User)
            .Include(s => s.Exam!)
                .ThenInclude(e => e.Tasks)
            .AsQueryable();

        if (!showAll)
            query = query.Where(s => s.Score == null);

        ViewBag.ShowAll = showAll;
        return View(await query.OrderByDescending(s => s.SubmittedAt).ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> ReviewExamSubmission(int submissionId, int score, string? comment)
    {
        var submission = await _context.ExamSubmissions.FindAsync(submissionId);
        if (submission == null) return NotFound();
        var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        submission.Score = Math.Clamp(score, 0, 100);
        submission.AdminComment = comment;
        submission.ReviewedAt = DateTime.UtcNow;
        submission.ReviewedByUserId = adminId;
        await _context.SaveChangesAsync();
        await LogAudit("Update", "ExamSubmission", submissionId, $"Оценка: {submission.Score}");
        TempData["Success"] = "Оценка выставлена";
        return RedirectToAction(nameof(ReviewExams));
    }

    #endregion

    #region Экспорт в Excel

    public async Task<IActionResult> ExportExamResults(int examId)
    {
        var exam = await _context.Exams.Include(e => e.Tasks).FirstOrDefaultAsync(e => e.Id == examId);
        if (exam == null) return NotFound();
        var submissions = await _context.ExamSubmissions.Include(s => s.User).Where(s => s.ExamId == examId).OrderByDescending(s => s.SubmittedAt).ToListAsync();
        using var workbook = new XLWorkbook();
        var sheetName = $"Экзамен {exam.Title}".Replace(":", "").Replace("\\", "").Replace("/", "").Replace("?", "").Replace("*", "").Replace("[", "").Replace("]", "");
        if (sheetName.Length > 31) sheetName = sheetName.Substring(0, 31);
        var ws = workbook.Worksheets.Add(sheetName);
        ws.Cell(1, 1).Value = $"Результаты экзамена: {exam.Title}"; ws.Cell(1, 1).Style.Font.Bold = true; ws.Cell(1, 1).Style.Font.FontSize = 14; ws.Range(1, 1, 1, 7).Merge();
        ws.Cell(2, 1).Value = $"Дата выгрузки: {DateTime.Now:dd.MM.yyyy HH:mm}"; ws.Range(2, 1, 2, 7).Merge();
        var headers = new[] { "№", "Сотрудник", "Логин", "Дата сдачи", "Оценка", "Статус", "Комментарий" };
        for (int i = 0; i < headers.Length; i++) { ws.Cell(4, i + 1).Value = headers[i]; ws.Cell(4, i + 1).Style.Font.Bold = true; ws.Cell(4, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray; ws.Cell(4, i + 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin; }
        int row = 5;
        foreach (var sub in submissions)
        {
            ws.Cell(row, 1).Value = row - 4; ws.Cell(row, 2).Value = sub.User?.FullName ?? "—"; ws.Cell(row, 3).Value = sub.User?.Username ?? "—";
            ws.Cell(row, 4).Value = sub.SubmittedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm"); ws.Cell(row, 5).Value = sub.Score?.ToString() ?? "—";
            ws.Cell(row, 6).Value = sub.Score == null ? "На проверке" : "Проверено"; ws.Cell(row, 7).Value = sub.AdminComment ?? "";
            if (sub.Score == null) ws.Range(row, 1, row, 7).Style.Fill.BackgroundColor = XLColor.LightYellow;
            else if (sub.Score >= 80) ws.Range(row, 1, row, 7).Style.Fill.BackgroundColor = XLColor.LightGreen;
            else ws.Range(row, 1, row, 7).Style.Fill.BackgroundColor = XLColor.LightSalmon;
            row++;
        }
        row += 2; ws.Cell(row, 1).Value = "Статистика:"; ws.Cell(row, 1).Style.Font.Bold = true; row++;
        ws.Cell(row, 1).Value = "Всего сдач:"; ws.Cell(row, 2).Value = submissions.Count; row++;
        ws.Cell(row, 1).Value = "Проверено:"; ws.Cell(row, 2).Value = submissions.Count(s => s.Score != null); row++;
        ws.Cell(row, 1).Value = "Средний балл:"; ws.Cell(row, 2).Value = submissions.Any(s => s.Score != null) ? (int)submissions.Where(s => s.Score != null).Average(s => s.Score!.Value) : "—"; row++;
        ws.Cell(row, 1).Value = "Сдано успешно (≥80):"; ws.Cell(row, 2).Value = submissions.Count(s => s.Score >= 80);
        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream(); workbook.SaveAs(stream); stream.Position = 0;
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Экзамен_{exam.Title}_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    public async Task<IActionResult> ExportAllExamResults()
    {
        var submissions = await _context.ExamSubmissions.Include(s => s.Exam).Include(s => s.User).OrderBy(s => s.Exam!.Title).ThenByDescending(s => s.SubmittedAt).ToListAsync();
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Все экзамены");
        ws.Cell(1, 1).Value = "Результаты всех экзаменов"; ws.Cell(1, 1).Style.Font.Bold = true; ws.Cell(1, 1).Style.Font.FontSize = 14; ws.Range(1, 1, 1, 8).Merge();
        ws.Cell(2, 1).Value = $"Дата выгрузки: {DateTime.Now:dd.MM.yyyy HH:mm}"; ws.Range(2, 1, 2, 8).Merge();
        var headers = new[] { "№", "Экзамен", "Сотрудник", "Логин", "Дата сдачи", "Оценка", "Статус", "Комментарий" };
        for (int i = 0; i < headers.Length; i++) { ws.Cell(4, i + 1).Value = headers[i]; ws.Cell(4, i + 1).Style.Font.Bold = true; ws.Cell(4, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray; }
        int row = 5;
        foreach (var sub in submissions)
        {
            ws.Cell(row, 1).Value = row - 4; ws.Cell(row, 2).Value = sub.Exam?.Title ?? "—"; ws.Cell(row, 3).Value = sub.User?.FullName ?? "—"; ws.Cell(row, 4).Value = sub.User?.Username ?? "—";
            ws.Cell(row, 5).Value = sub.SubmittedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm"); ws.Cell(row, 6).Value = sub.Score?.ToString() ?? "—";
            ws.Cell(row, 7).Value = sub.Score == null ? "На проверке" : "Проверено"; ws.Cell(row, 8).Value = sub.AdminComment ?? "";
            if (sub.Score == null) ws.Range(row, 1, row, 8).Style.Fill.BackgroundColor = XLColor.LightYellow;
            else if (sub.Score >= 80) ws.Range(row, 1, row, 8).Style.Fill.BackgroundColor = XLColor.LightGreen;
            else ws.Range(row, 1, row, 8).Style.Fill.BackgroundColor = XLColor.LightSalmon;
            row++;
        }
        row += 2; ws.Cell(row, 1).Value = "Итого сдач:"; ws.Cell(row, 2).Value = submissions.Count; row++;
        ws.Cell(row, 1).Value = "Проверено:"; ws.Cell(row, 2).Value = submissions.Count(s => s.Score != null); row++;
        ws.Cell(row, 1).Value = "Средний балл:"; ws.Cell(row, 2).Value = submissions.Any(s => s.Score != null) ? (int)submissions.Where(s => s.Score != null).Average(s => s.Score!.Value) : 0; row++;
        ws.Cell(row, 1).Value = "Успешно (≥80):"; ws.Cell(row, 2).Value = submissions.Count(s => s.Score >= 80);
        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream(); workbook.SaveAs(stream); stream.Position = 0;
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Все_экзамены_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    #endregion
}