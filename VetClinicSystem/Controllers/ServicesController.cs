using Microsoft.AspNetCore.Mvc;

namespace VetClinicSystem.Controllers
{
    public class ServicesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
