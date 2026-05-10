using Microsoft.AspNetCore.Mvc;
using VetClinicSystem.Helpers;
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
            NormalizePetFields(pet);
            ValidatePetInput(pet);

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

            LoadSexOptions();
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
            NormalizePetFields(pet);
            ValidatePetInput(pet);

            if (roleId == 3)
            {
                var myPets = _petService.GetByUser(userId.Value);
                if (!myPets.Any(p => p.Id == pet.Id))
                    return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please complete all required pet fields.";
                LoadSexOptions();
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
                LoadSexOptions();
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

        private static void NormalizePetFields(Pet pet)
        {
            pet.PetName = InputValidationHelper.NormalizeTrimmed(pet.PetName);
            pet.Species = InputValidationHelper.NormalizeTrimmed(pet.Species);
            pet.Breed = string.IsNullOrWhiteSpace(pet.Breed) ? null : InputValidationHelper.NormalizeTrimmed(pet.Breed);
            pet.Color = string.IsNullOrWhiteSpace(pet.Color) ? null : InputValidationHelper.NormalizeColorValue(pet.Color);
            pet.Notes = string.IsNullOrWhiteSpace(pet.Notes) ? null : InputValidationHelper.NormalizeTrimmed(pet.Notes);
            pet.Sex = string.IsNullOrWhiteSpace(pet.Sex) ? null : InputValidationHelper.NormalizeTrimmed(pet.Sex);
        }

        private void ValidatePetInput(Pet pet)
        {
            AddModelErrorIfMissing(nameof(Pet.PetName), InputValidationHelper.IsValidPetName(pet.PetName), InputValidationHelper.PetNameMessage);
            AddModelErrorIfMissing(nameof(Pet.Species), InputValidationHelper.IsValidSpeciesName(pet.Species), InputValidationHelper.SpeciesMessage);
            AddModelErrorIfMissing(nameof(Pet.Breed), InputValidationHelper.IsValidBreedName(pet.Breed), InputValidationHelper.BreedMessage);
            AddModelErrorIfMissing(nameof(Pet.Color), InputValidationHelper.IsValidColorName(pet.Color), InputValidationHelper.ColorMessage);

            if (!string.IsNullOrWhiteSpace(pet.Species) && !PetValidationHelper.IsValidSpecies(pet.Species))
            {
                AddModelErrorIfMissing(nameof(Pet.Species), false, "Please enter a recognized species.");
            }

            if (!string.IsNullOrWhiteSpace(pet.Breed) && !PetValidationHelper.IsValidBreed(pet.Species, pet.Breed))
            {
                AddModelErrorIfMissing(nameof(Pet.Breed), false, "Please enter a recognized breed for the selected species.");
            }
        }

        private void AddModelErrorIfMissing(string key, bool isValid, string message)
        {
            if (isValid)
                return;

            if (!ModelState.TryGetValue(key, out var entry) || entry.Errors.Count == 0)
            {
                ModelState.AddModelError(key, message);
            }
        }
    }
}
