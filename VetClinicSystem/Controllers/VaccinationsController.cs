using Microsoft.AspNetCore.Mvc;

namespace VetClinicSystem.Controllers
{
    public class VaccinationsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
