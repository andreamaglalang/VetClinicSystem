using Microsoft.AspNetCore.Mvc;

namespace VetClinicSystem.Controllers
{
    public class AppointmentsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
