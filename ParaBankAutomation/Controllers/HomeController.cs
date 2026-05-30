using Microsoft.AspNetCore.Mvc;
using ParaBankAutomation.Models;

namespace ParaBankAutomation.Controllers;

public class HomeController : Controller
{
    internal static AutomationSession? CurrentSession;
    internal static string? UploadedFilePath;

    [HttpGet("/")]
    public IActionResult Index()
    {
        ViewBag.Session = CurrentSession;
        return View();
    }
}
