using Microsoft.AspNetCore.Mvc;
using VetClinicSystem.Models;
using VetClinicSystem.Services.Pets;

namespace VetClinicSystem.Controllers
{
    public class PetsController : Controller
    {
        private readonly IPetService _petService;
        private static readonly string[] SexOptions = ["Male", "Female"];

        public PetsController(IPetService petService)
        {
            _petService = petService;
        }

        public IActionResult Index(string? search)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            ViewBag.Search = search;

            if (roleId == 3)
                return View(_petService.SearchByUser(userId.Value, search));

            return View(_petService.Search(search));
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            LoadSexOptions();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Pet pet)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            ModelState.Remove("Owner");
            ModelState.Remove("OwnerId");

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please complete all required pet fields.";
                LoadSexOptions();
                return View(pet);
            }

            try
            {
                _petService.Add(pet, userId.Value);
                TempData["Success"] = "Pet added successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                LoadSexOptions();
                return View(pet);
            }
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var pet = _petService.GetById(id);
            if (pet == null) return NotFound();

            if (roleId == 3)
            {
                var myPets = _petService.GetByUser(userId.Value);
                if (!myPets.Any(p => p.Id == id))
                    return Unauthorized();
            }

            return View(pet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Pet pet)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            ModelState.Remove("Owner");
            ModelState.Remove("OwnerId");

            if (roleId == 3)
            {
                var myPets = _petService.GetByUser(userId.Value);
                if (!myPets.Any(p => p.Id == pet.Id))
                    return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please complete all required pet fields.";
                return View(pet);
            }

            try
            {
                _petService.Update(pet);
                TempData["Success"] = "Pet updated successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return View(pet);
            }
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var pet = _petService.GetById(id);
            if (pet == null) return NotFound();

            if (roleId == 3)
            {
                var myPets = _petService.GetByUser(userId.Value);
                if (!myPets.Any(p => p.Id == id))
                    return Unauthorized();
            }

            return View(pet);
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var pet = _petService.GetById(id);
            if (pet == null) return NotFound();

            if (roleId == 3)
            {
                var myPets = _petService.GetByUser(userId.Value);
                if (!myPets.Any(p => p.Id == id))
                    return Unauthorized();
            }

            return View(pet);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (roleId == 3)
            {
                var myPets = _petService.GetByUser(userId.Value);
                if (!myPets.Any(p => p.Id == id))
                    return Unauthorized();
            }

            try
            {
                _petService.Delete(id);
                TempData["Success"] = "Pet deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        private void LoadSexOptions()
        {
            ViewBag.SexOptions = SexOptions;
        }
    }
}
