using Microsoft.AspNetCore.Mvc;

namespace VetClinicSystem.Controllers
{
    public class PetsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
