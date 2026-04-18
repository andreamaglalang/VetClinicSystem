using Microsoft.AspNetCore.Mvc;

namespace VetClinicSystem.Controllers
{
    public class ClinicController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
