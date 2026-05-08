using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
            LoadSpeciesAndBreedOptions();
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

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please complete all required pet fields.";
                LoadSexOptions();
                LoadSpeciesAndBreedOptions(pet.Species, pet.Breed);
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
                LoadSpeciesAndBreedOptions(pet.Species, pet.Breed);
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
            LoadSpeciesAndBreedOptions(pet.Species, pet.Breed);
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
                LoadSpeciesAndBreedOptions(pet.Species, pet.Breed);
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
                LoadSpeciesAndBreedOptions(pet.Species, pet.Breed);
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

        private void LoadSpeciesAndBreedOptions(string? selectedSpecies = null, string? selectedBreed = null)
        {
            var speciesOptions = PetValidationHelper.GetAllowedSpecies();
            ViewBag.SpeciesOptions = new SelectList(speciesOptions, selectedSpecies);
            ViewBag.BreedOptions = new SelectList(PetValidationHelper.GetAllowedBreeds(selectedSpecies), selectedBreed);
            ViewBag.BreedMap = speciesOptions.ToDictionary(
                species => species,
                species => PetValidationHelper.GetAllowedBreeds(species).ToArray(),
                StringComparer.OrdinalIgnoreCase);
        }

        private static void NormalizePetFields(Pet pet)
        {
            pet.PetName = pet.PetName?.Trim() ?? string.Empty;
            pet.Species = pet.Species?.Trim() ?? string.Empty;
            pet.Breed = string.IsNullOrWhiteSpace(pet.Breed) ? null : pet.Breed.Trim();
            pet.Color = string.IsNullOrWhiteSpace(pet.Color) ? null : pet.Color.Trim();
            pet.Notes = string.IsNullOrWhiteSpace(pet.Notes) ? null : pet.Notes.Trim();
            pet.Sex = string.IsNullOrWhiteSpace(pet.Sex) ? null : pet.Sex.Trim();
        }
    }
}
