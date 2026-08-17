using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetMarketplaceAPI.Data;
using PetMarketplaceAPI.Models;
using PetMarketplaceAPI.Services;

namespace PetMarketplaceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServiceProvidersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly LocationIQService _locationIQService;

        public ServiceProvidersController(
            ApplicationDbContext context,
            LocationIQService locationIQService)
        {
            _context = context;
            _locationIQService = locationIQService;
        }

        // GET: api/ServiceProviders/veterinary
        [HttpGet("veterinary")]
        public async Task<ActionResult> GetVeterinaryServices(
            [FromQuery] double latitude,
            [FromQuery] double longitude,
            [FromQuery] double radiusKm = 10)
        {
            Console.WriteLine($"\n=== Searching Veterinary Services near {latitude}, {longitude} ===");

            var allProviders = new List<object>();

            // Use text search for better results
            var searchQueries = new[]
            {
                "veterinary clinic",
                "veterinary hospital",
                "animal hospital",
                "pet clinic",
                "animal clinic"
            };

            foreach (var query in searchQueries)
            {
                var places = await _locationIQService.SearchTextAsync(
                    query, latitude, longitude, 20);

                foreach (var place in places)
                {
                    double placeLat = 0, placeLng = 0;
                    double.TryParse(place.Lat, out placeLat);
                    double.TryParse(place.Lon, out placeLng);

                    if (placeLat == 0 && placeLng == 0) continue;

                    var distanceKm = CalculateDistance(latitude, longitude, placeLat, placeLng);

                    if (distanceKm <= radiusKm)
                    {
                        allProviders.Add(new
                        {
                            Id = place.PlaceId,
                            Name = place.Name ?? query,
                            ServiceType = "Veterinary",
                            Category = DetermineCategory(place, "vet"),
                            Address = BuildAddress(place),
                            Latitude = placeLat,
                            Longitude = placeLng,
                            DistanceKm = Math.Round(distanceKm, 2),
                            Source = "LocationIQ"
                        });
                    }
                }

                await Task.Delay(500);
            }

            // Get local vets from database
            var localVets = await _context.PetServiceProviders
                .Where(sp => sp.ServiceType == "Vet" || sp.ServiceType == "Veterinary")
                .ToListAsync();

            foreach (var provider in localVets)
            {
                var distanceKm = CalculateDistance(latitude, longitude, provider.Latitude, provider.Longitude);

                if (distanceKm <= radiusKm)
                {
                    allProviders.Add(new
                    {
                        provider.Id,
                        provider.Name,
                        ServiceType = "Veterinary",
                        Category = "Vet Clinic",
                        provider.Description,
                        provider.PhoneNumber,
                        provider.Address,
                        provider.Latitude,
                        provider.Longitude,
                        provider.Rating,
                        DistanceKm = Math.Round(distanceKm, 2),
                        Source = "Local Database"
                    });
                }
            }

            var uniqueProviders = allProviders
                .GroupBy(p => $"{((dynamic)p).Name}_{((dynamic)p).Latitude}")
                .Select(g => g.First())
                .OrderBy(p => ((dynamic)p).DistanceKm)
                .ToList();

            Console.WriteLine($"Total veterinary services: {uniqueProviders.Count}");
            return Ok(uniqueProviders);
        }

        // GET: api/ServiceProviders/pet-care
        [HttpGet("pet-care")]
        public async Task<ActionResult> GetPetCareServices(
            [FromQuery] double latitude,
            [FromQuery] double longitude,
            [FromQuery] double radiusKm = 10)
        {
            Console.WriteLine($"\n=== Searching Pet Care Services near {latitude}, {longitude} ===");

            var allProviders = new List<object>();

            var searchQueries = new[]
            {
                "pet grooming",
                "pet store",
                "pet shop",
                "pet supplies",
                "pet food store",
                "pet boarding"
            };

            foreach (var query in searchQueries)
            {
                var places = await _locationIQService.SearchTextAsync(
                    query, latitude, longitude, 20);

                foreach (var place in places)
                {
                    double placeLat = 0, placeLng = 0;
                    double.TryParse(place.Lat, out placeLat);
                    double.TryParse(place.Lon, out placeLng);

                    if (placeLat == 0 && placeLng == 0) continue;

                    var distanceKm = CalculateDistance(latitude, longitude, placeLat, placeLng);

                    if (distanceKm <= radiusKm)
                    {
                        allProviders.Add(new
                        {
                            Id = place.PlaceId,
                            Name = place.Name ?? query,
                            ServiceType = "PetCare",
                            Category = DetermineCategory(place, "petcare"),
                            Address = BuildAddress(place),
                            Latitude = placeLat,
                            Longitude = placeLng,
                            DistanceKm = Math.Round(distanceKm, 2),
                            Source = "LocationIQ"
                        });
                    }
                }

                await Task.Delay(500);
            }

            // Get local pet care from database
            var localPetCare = await _context.PetServiceProviders
                .Where(sp => sp.ServiceType == "PetShop" || sp.ServiceType == "Grooming")
                .ToListAsync();

            foreach (var provider in localPetCare)
            {
                var distanceKm = CalculateDistance(latitude, longitude, provider.Latitude, provider.Longitude);

                if (distanceKm <= radiusKm)
                {
                    allProviders.Add(new
                    {
                        provider.Id,
                        provider.Name,
                        ServiceType = "PetCare",
                        Category = provider.ServiceType == "Grooming" ? "Grooming" : "Pet Store",
                        provider.Description,
                        provider.PhoneNumber,
                        provider.Address,
                        provider.Latitude,
                        provider.Longitude,
                        provider.Rating,
                        DistanceKm = Math.Round(distanceKm, 2),
                        Source = "Local Database"
                    });
                }
            }

            var uniqueProviders = allProviders
                .GroupBy(p => $"{((dynamic)p).Name}_{((dynamic)p).Latitude}")
                .Select(g => g.First())
                .OrderBy(p => ((dynamic)p).DistanceKm)
                .ToList();

            Console.WriteLine($"Total pet care services: {uniqueProviders.Count}");
            return Ok(uniqueProviders);
        }

        // GET: api/ServiceProviders/pharmacy
        [HttpGet("pharmacy")]
        public async Task<ActionResult> GetPharmacyServices(
            [FromQuery] double latitude,
            [FromQuery] double longitude,
            [FromQuery] double radiusKm = 10)
        {
            Console.WriteLine($"\n=== Searching Pharmacies near {latitude}, {longitude} ===");

            var allProviders = new List<object>();

            var searchQueries = new[]
            {
                "pharmacy",
                "chemist",
                "medical store"
            };

            foreach (var query in searchQueries)
            {
                var places = await _locationIQService.SearchTextAsync(
                    query, latitude, longitude, 20);

                foreach (var place in places)
                {
                    double placeLat = 0, placeLng = 0;
                    double.TryParse(place.Lat, out placeLat);
                    double.TryParse(place.Lon, out placeLng);

                    if (placeLat == 0 && placeLng == 0) continue;

                    var distanceKm = CalculateDistance(latitude, longitude, placeLat, placeLng);

                    if (distanceKm <= radiusKm)
                    {
                        allProviders.Add(new
                        {
                            Id = place.PlaceId,
                            Name = place.Name ?? query,
                            ServiceType = "Pharmacy",
                            Category = "Pharmacy",
                            Address = BuildAddress(place),
                            Latitude = placeLat,
                            Longitude = placeLng,
                            DistanceKm = Math.Round(distanceKm, 2),
                            Source = "LocationIQ"
                        });
                    }
                }

                await Task.Delay(500);
            }

            var uniqueProviders = allProviders
                .GroupBy(p => $"{((dynamic)p).Name}_{((dynamic)p).Latitude}")
                .Select(g => g.First())
                .OrderBy(p => ((dynamic)p).DistanceKm)
                .ToList();

            Console.WriteLine($"Total pharmacies: {uniqueProviders.Count}");
            return Ok(uniqueProviders);
        }

        // GET: api/ServiceProviders/all
        [HttpGet("all")]
        public async Task<ActionResult> GetAllServices(
            [FromQuery] double latitude,
            [FromQuery] double longitude,
            [FromQuery] double radiusKm = 10)
        {
            var allProviders = new List<object>();
            var radiusMeters = (int)(radiusKm * 1000);

            var allTags = new List<string>
            {
                "amenity:veterinary",
                "amenity:pharmacy",
                "shop:pet",
                "shop:pet_grooming",
                "amenity:animal_boarding"
            };

            foreach (var tag in allTags)
            {
                var places = await _locationIQService.SearchNearbyAsync(
                    latitude, longitude, tag, radiusMeters);

                foreach (var place in places)
                {
                    double placeLat = 0, placeLng = 0;
                    double.TryParse(place.Lat, out placeLat);
                    double.TryParse(place.Lon, out placeLng);

                    if (placeLat == 0 && placeLng == 0) continue;

                    var distanceKm = CalculateDistance(latitude, longitude, placeLat, placeLng);

                    if (distanceKm <= radiusKm)
                    {
                        var serviceType = tag.Contains("veterinary") ? "Veterinary" :
                                         tag.Contains("pharmacy") ? "Pharmacy" : "PetCare";

                        allProviders.Add(new
                        {
                            Id = place.PlaceId,
                            Name = place.Name ?? serviceType,
                            ServiceType = serviceType,
                            Category = serviceType,
                            Address = BuildAddress(place),
                            Latitude = placeLat,
                            Longitude = placeLng,
                            DistanceKm = Math.Round(distanceKm, 2),
                            Source = "LocationIQ"
                        });
                    }
                }

                await Task.Delay(500);
            }

            var uniqueProviders = allProviders
                .GroupBy(p => $"{((dynamic)p).Name}_{((dynamic)p).Latitude}")
                .Select(g => g.First())
                .OrderBy(p => ((dynamic)p).DistanceKm)
                .ToList();

            return Ok(uniqueProviders);
        }

        // Helper: Determine category
        private string DetermineCategory(LocationIQPlace place, string type)
        {
            var name = (place.Name ?? "").ToLower();
            var displayName = (place.DisplayName ?? "").ToLower();

            if (type == "vet")
            {
                if (name.Contains("hospital") || displayName.Contains("hospital"))
                    return "Vet Hospital";
                else if (name.Contains("dispensary") || displayName.Contains("dispensary"))
                    return "Vet Dispensary";
                else
                    return "Vet Clinic";
            }
            else if (type == "petcare")
            {
                if (name.Contains("groom") || displayName.Contains("groom"))
                    return "Grooming";
                else if (name.Contains("board") || displayName.Contains("board"))
                    return "Pet Boarding";
                else
                    return "Pet Store";
            }

            return "Other";
        }

        // Helper: Build address
        private string BuildAddress(LocationIQPlace place)
        {
            if (!string.IsNullOrEmpty(place.DisplayName))
            {
                var parts = place.DisplayName.Split(',');
                if (parts.Length >= 3)
                {
                    return string.Join(",", parts.Take(3));
                }
                return place.DisplayName;
            }

            if (place.Address != null)
            {
                var parts = new List<string>();
                if (!string.IsNullOrEmpty(place.Address.Road)) parts.Add(place.Address.Road);
                if (!string.IsNullOrEmpty(place.Address.Suburb)) parts.Add(place.Address.Suburb);
                if (!string.IsNullOrEmpty(place.Address.City)) parts.Add(place.Address.City);
                if (!string.IsNullOrEmpty(place.Address.State)) parts.Add(place.Address.State);
                return string.Join(", ", parts);
            }

            return "Address not available";
        }

        // Helper: Calculate distance
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371;
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLon = (lon2 - lon1) * Math.PI / 180;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }
    }
}