using Microsoft.AspNetCore.Mvc;

namespace VetClinicSystem.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
