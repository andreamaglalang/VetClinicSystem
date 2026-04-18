using Microsoft.AspNetCore.Mvc;

namespace VetClinicSystem.Controllers
{
    public class MedicalRecordsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
