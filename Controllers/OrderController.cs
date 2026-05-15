using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace EmployeeTrainer.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
public class OrderController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}