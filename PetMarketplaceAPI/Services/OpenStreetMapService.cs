using System.Text.Json;
using System.Net.Http.Headers;

namespace PetMarketplaceAPI.Services
{
    public class OpenStreetMapService
    {
        private readonly HttpClient _httpClient;
        private readonly List<string> _overpassServers = new()
        {
            "https://overpass-api.de/api/interpreter",
            "https://overpass.kumi.systems/api/interpreter",
            "https://overpass.nchc.org.tw/api/interpreter"
        };

        public OpenStreetMapService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("PetMarketplace/1.0 (contact@petmarketplace.com)");
        }

        // Main search method - tries multiple servers
        public async Task<List<NearbyPlace>> SearchNearbyPlacesAsync(
            double latitude,
            double longitude,
            string serviceType,
            int radiusMeters = 10000)
        {
            var places = new List<NearbyPlace>();

            // Get search queries for this service type
            var searchQueries = GetSearchQueries(serviceType);

            foreach (var query in searchQueries)
            {
                var results = await SearchWithMultipleServers(latitude, longitude, query.Key, query.Value, radiusMeters);
                places.AddRange(results);
                await Task.Delay(1000); // Delay between requests
            }

            // Remove duplicates and sort by distance
            var uniquePlaces = places
                .GroupBy(p => p.PlaceId)
                .Select(g => g.First())
                .OrderBy(p => p.DistanceKm)
                .ToList();

            Console.WriteLine($"Total unique places found: {uniquePlaces.Count}");
            return uniquePlaces;
        }

        // Try multiple Overpass servers
        private async Task<List<NearbyPlace>> SearchWithMultipleServers(
            double latitude, double longitude, string key, string value, int radiusMeters)
        {
            foreach (var server in _overpassServers)
            {
                try
                {
                    var results = await SearchOnServer(server, latitude, longitude, key, value, radiusMeters);
                    if (results.Count > 0)
                    {
                        Console.WriteLine($"Found {results.Count} results from {server}");
                        return results;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Server {server} failed: {ex.Message}");
                }
            }

            return new List<NearbyPlace>();
        }

        // Search on specific server with India-optimized tags
        private async Task<List<NearbyPlace>> SearchOnServer(
            string server, double latitude, double longitude, string key, string value, int radiusMeters)
        {
            // Build comprehensive query for Indian data
            var overpassQuery = BuildOverpassQuery(latitude, longitude, key, value, radiusMeters);

            var url = $"{server}?data={Uri.EscapeDataString(overpassQuery)}";

            Console.WriteLine($"Querying: {server}");
            Console.WriteLine($"Query: {overpassQuery.Substring(0, Math.Min(100, overpassQuery.Length))}...");

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"HTTP {(int)response.StatusCode} from {server}");
                return new List<NearbyPlace>();
            }

            var content = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(content) || content.Length < 10)
            {
                Console.WriteLine("Empty response");
                return new List<NearbyPlace>();
            }

            var result = JsonSerializer.Deserialize<OsmResponse>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result?.Elements == null || result.Elements.Count == 0)
            {
                Console.WriteLine("No elements in response");
                return new List<NearbyPlace>();
            }

            var places = new List<NearbyPlace>();

            foreach (var element in result.Elements)
            {
                var name = GetPlaceName(element.Tags, value);
                var address = BuildAddress(element.Tags);
                var phone = GetPhone(element.Tags);
                var website = GetWebsite(element.Tags);

                // Skip if no name and no address
                if (name == "Unknown" && address == "Address not available")
                    continue;

                var distanceKm = CalculateDistance(latitude, longitude, element.Lat, element.Lon);

                places.Add(new NearbyPlace
                {
                    PlaceId = $"{element.Type}_{element.Id}",
                    Name = name,
                    Vicinity = address,
                    Phone = phone,
                    Website = website,
                    DistanceKm = Math.Round(distanceKm, 2),
                    Geometry = new NearbyPlaceGeometry
                    {
                        Location = new NearbyPlaceLocation
                        {
                            Lat = element.Lat,
                            Lng = element.Lon
                        }
                    }
                });
            }

            return places;
        }

        // Build Overpass query with India-specific tags
        private string BuildOverpassQuery(double lat, double lon, string key, string value, int radius)
        {
            // Include multiple tag variations for better coverage
            var queries = new List<string>();

            // Standard amenity tag
            queries.Add($"node[\"{key}\"=\"{value}\"](around:{radius},{lat.ToString("F6")},{lon.ToString("F6")});");
            queries.Add($"way[\"{key}\"=\"{value}\"](around:{radius},{lat.ToString("F6")},{lon.ToString("F6")});");

            // For veterinary, also search for related tags
            if (value == "veterinary")
            {
                queries.Add($"node[\"amenity\"=\"animal_boarding\"](around:{radius},{lat.ToString("F6")},{lon.ToString("F6")});");
                queries.Add($"node[\"amenity\"=\"animal_shelter\"](around:{radius},{lat.ToString("F6")},{lon.ToString("F6")});");
                queries.Add($"node[\"shop\"=\"pet\"](around:{radius},{lat.ToString("F6")},{lon.ToString("F6")});");
            }

            // For pet store, also search for pet grooming
            if (value == "pet")
            {
                queries.Add($"node[\"shop\"=\"pet_grooming\"](around:{radius},{lat.ToString("F6")},{lon.ToString("F6")});");
                queries.Add($"node[\"amenity\"=\"veterinary\"](around:{radius},{lat.ToString("F6")},{lon.ToString("F6")});");
            }

            // For pharmacy
            if (value == "pharmacy")
            {
                queries.Add($"node[\"shop\"=\"chemist\"](around:{radius},{lat.ToString("F6")},{lon.ToString("F6")});");
            }

            var queryString = string.Join("", queries);

            return $"[out:json][timeout:50];({queryString});out center tags;";
        }

        // Get search queries for service type
        private List<KeyValuePair<string, string>> GetSearchQueries(string serviceType)
        {
            return serviceType switch
            {
                "veterinary_care" => new List<KeyValuePair<string, string>>
                {
                    new("amenity", "veterinary")
                },
                "pharmacy" => new List<KeyValuePair<string, string>>
                {
                    new("amenity", "pharmacy")
                },
                "pet_store" => new List<KeyValuePair<string, string>>
                {
                    new("shop", "pet")
                },
                _ => new List<KeyValuePair<string, string>>
                {
                    new("amenity", "veterinary")
                }
            };
        }

        // Fallback: Search by name keywords
        public async Task<List<NearbyPlace>> SearchByNameAsync(
            double latitude, double longitude, string keyword, int radiusMeters = 5000)
        {
            var overpassQuery = $"[out:json][timeout:30];" +
                               $"(node[\"name\"~\"{keyword}\",i](around:{radiusMeters},{latitude.ToString("F6")},{longitude.ToString("F6")});" +
                               $"way[\"name\"~\"{keyword}\",i](around:{radiusMeters},{latitude.ToString("F6")},{longitude.ToString("F6")}););" +
                               $"out center tags;";

            var url = $"{_overpassServers[0]}?data={Uri.EscapeDataString(overpassQuery)}";

            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OsmResponse>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var places = new List<NearbyPlace>();

                foreach (var element in result?.Elements ?? new List<OsmElement>())
                {
                    var name = element.Tags?.Name ?? "Unknown";
                    var distanceKm = CalculateDistance(latitude, longitude, element.Lat, element.Lon);

                    places.Add(new NearbyPlace
                    {
                        PlaceId = $"{element.Type}_{element.Id}",
                        Name = name,
                        Vicinity = BuildAddress(element.Tags),
                        Phone = GetPhone(element.Tags),
                        Website = GetWebsite(element.Tags),
                        DistanceKm = Math.Round(distanceKm, 2),
                        Geometry = new NearbyPlaceGeometry
                        {
                            Location = new NearbyPlaceLocation
                            {
                                Lat = element.Lat,
                                Lng = element.Lon
                            }
                        }
                    });
                }

                return places;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Name search error: {ex.Message}");
                return new List<NearbyPlace>();
            }
        }

        // Helper methods
        private string GetPlaceName(OsmTags tags, string serviceType)
        {
            if (tags == null) return "Unknown";

            if (!string.IsNullOrEmpty(tags.Name))
                return tags.Name;

            // Try other name fields
            if (!string.IsNullOrEmpty(tags.Brand))
                return tags.Brand;

            return serviceType switch
            {
                "veterinary" => "Veterinary Clinic",
                "animal_boarding" => "Animal Boarding",
                "animal_shelter" => "Animal Shelter",
                "pharmacy" => "Pharmacy",
                "chemist" => "Chemist",
                "pet" => "Pet Store",
                "pet_grooming" => "Pet Grooming",
                _ => "Pet Service"
            };
        }

        private string BuildAddress(OsmTags tags)
        {
            if (tags == null) return "Address not available";

            var parts = new List<string>();

            if (!string.IsNullOrEmpty(tags.Housenumber))
                parts.Add(tags.Housenumber);

            if (!string.IsNullOrEmpty(tags.Street))
                parts.Add(tags.Street);

            if (!string.IsNullOrEmpty(tags.City))
                parts.Add(tags.City);

            if (!string.IsNullOrEmpty(tags.State))
                parts.Add(tags.State);

            if (!string.IsNullOrEmpty(tags.Postcode))
                parts.Add(tags.Postcode);

            if (!string.IsNullOrEmpty(tags.AddrFull))
                return tags.AddrFull;

            return parts.Count > 0 ? string.Join(", ", parts) : "Address not available";
        }

        private string GetPhone(OsmTags tags)
        {
            if (tags == null) return "";
            return tags.Phone ?? tags.ContactPhone ?? tags.ContactMobile ?? "";
        }

        private string GetWebsite(OsmTags tags)
        {
            if (tags == null) return "";
            return tags.Website ?? tags.ContactWebsite ?? "";
        }

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

    // Update OsmTags to include more fields
    public class OsmTags
    {
        public string Name { get; set; }
        public string Brand { get; set; }
        public string Phone { get; set; }
        public string Website { get; set; }
        public string Amenity { get; set; }
        public string Healthcare { get; set; }
        public string Shop { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Postcode { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("addr:street")]
        public string Street { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("addr:housenumber")]
        public string Housenumber { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("addr:city")]
        public string AddrCity { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("addr:postcode")]
        public string AddrPostcode { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("addr:full")]
        public string AddrFull { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("contact:phone")]
        public string ContactPhone { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("contact:mobile")]
        public string ContactMobile { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("contact:website")]
        public string ContactWebsite { get; set; }
    }

    // Keep other model classes same as before
    public class NearbyPlace
    {
        public string PlaceId { get; set; }
        public string Name { get; set; }
        public string Vicinity { get; set; }
        public string Phone { get; set; }
        public string Website { get; set; }
        public double DistanceKm { get; set; }
        public NearbyPlaceGeometry Geometry { get; set; }
    }

    public class NearbyPlaceGeometry
    {
        public NearbyPlaceLocation Location { get; set; }
    }

    public class NearbyPlaceLocation
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
    }

    public class OsmResponse
    {
        public List<OsmElement> Elements { get; set; }
    }

    public class OsmElement
    {
        public string Type { get; set; }
        public long Id { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public OsmTags Tags { get; set; }
    }
}