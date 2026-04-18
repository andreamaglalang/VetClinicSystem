using Microsoft.AspNetCore.Mvc;

namespace VetClinicSystem.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
