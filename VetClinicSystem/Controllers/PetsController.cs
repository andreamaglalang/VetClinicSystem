using Microsoft.AspNetCore.Mvc;
using VetClinicSystem.Models;
using VetClinicSystem.Services.Pets;

namespace VetClinicSystem.Controllers
{
    public class PetsController : Controller
    {
        private readonly IPetService _petService;

        public PetsController(IPetService petService)
        {
            _petService = petService;
        }

        public IActionResult Index()
        {
            return View(_petService.GetAll());
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Pet pet)
        {
            if (!ModelState.IsValid)
                return View(pet);

            _petService.Add(pet);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var pet = _petService.GetById(id);
            if (pet == null) return NotFound();
            return View(pet);
        }

        [HttpPost]
        public IActionResult Edit(Pet pet)
        {
            if (!ModelState.IsValid)
                return View(pet);

            _petService.Update(pet);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var pet = _petService.GetById(id);
            if (pet == null) return NotFound();
            return View(pet);
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var pet = _petService.GetById(id);
            if (pet == null) return NotFound();
            return View(pet);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            _petService.Delete(id);
            return RedirectToAction("Index");
        }
    }
}