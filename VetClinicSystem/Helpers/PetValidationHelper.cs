using System.Collections.ObjectModel;

namespace VetClinicSystem.Helpers
{
    public static class PetValidationHelper
    {
        private static readonly ReadOnlyDictionary<string, string[]> AllowedSpeciesBreeds =
            new(new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["Dog"] =
                [
                    "Aspin",
                    "Beagle",
                    "Chihuahua",
                    "Dachshund",
                    "French Bulldog",
                    "German Shepherd",
                    "Golden Retriever",
                    "Labrador Retriever",
                    "Pomeranian",
                    "Poodle",
                    "Shih Tzu",
                    "Siberian Husky",
                    "Yorkshire Terrier",
                    "Mixed Breed"
                ],
                ["Cat"] =
                [
                    "Persian",
                    "Siamese",
                    "Maine Coon",
                    "British Shorthair",
                    "Ragdoll",
                    "Bengal",
                    "Scottish Fold",
                    "Sphynx",
                    "Puspin",
                    "Mixed Breed"
                ]
            });

        public static IReadOnlyCollection<string> GetAllowedSpecies()
        {
            return AllowedSpeciesBreeds.Keys.ToList().AsReadOnly();
        }

        public static IReadOnlyCollection<string> GetAllowedBreeds(string? species)
        {
            if (string.IsNullOrWhiteSpace(species))
                return Array.Empty<string>();

            return AllowedSpeciesBreeds.TryGetValue(species.Trim(), out var breeds)
                ? breeds
                : Array.Empty<string>();
        }

        public static bool IsValidSpecies(string? species)
        {
            return !string.IsNullOrWhiteSpace(species) && AllowedSpeciesBreeds.ContainsKey(species.Trim());
        }

        public static bool IsValidBreed(string? species, string? breed)
        {
            if (string.IsNullOrWhiteSpace(breed))
                return false;

            return GetAllowedBreeds(species)
                .Any(x => string.Equals(x, breed.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }
}
