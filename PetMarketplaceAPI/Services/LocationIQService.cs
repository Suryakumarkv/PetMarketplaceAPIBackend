using System.Text.Json;

namespace PetMarketplaceAPI.Services
{
    public class LocationIQService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public LocationIQService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["LocationIQ:ApiKey"] ?? "";
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        // Search nearby places using tag
        public async Task<List<LocationIQPlace>> SearchNearbyAsync(
            double latitude, double longitude, string tag, int radiusMeters = 5000)
        {
            var url = $"https://us1.locationiq.com/v1/nearby" +
                     $"?key={_apiKey}" +
                     $"&lat={latitude.ToString("F6")}" +
                     $"&lon={longitude.ToString("F6")}" +
                     $"&tag={tag}" +
                     $"&radius={radiusMeters}" +
                     $"&format=json";

            Console.WriteLine($"LocationIQ Search: tag={tag}, radius={radiusMeters}m");

            try
            {
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"LocationIQ Error {(int)response.StatusCode}: {errorContent}");
                    return new List<LocationIQPlace>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var results = JsonSerializer.Deserialize<List<LocationIQPlace>>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                Console.WriteLine($"Found {results?.Count ?? 0} results for {tag}");
                return results ?? new List<LocationIQPlace>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LocationIQ Exception: {ex.Message}");
                return new List<LocationIQPlace>();
            }
        }

        // Search using free text query
        public async Task<List<LocationIQPlace>> SearchTextAsync(
            string query, double? latitude = null, double? longitude = null, int limit = 20)
        {
            var url = $"https://us1.locationiq.com/v1/search" +
                     $"?key={_apiKey}" +
                     $"&q={Uri.EscapeDataString(query)}" +
                     $"&format=json" +
                     $"&limit={limit}" +
                     $"&countrycodes=in";

            if (latitude.HasValue && longitude.HasValue)
            {
                url += $"&viewbox={longitude - 0.5},{latitude + 0.5},{longitude + 0.5},{latitude - 0.5}";
                url += "&bounded=1";
            }

            Console.WriteLine($"LocationIQ Text Search: {query}");

            try
            {
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"LocationIQ Text Error {(int)response.StatusCode}: {errorContent}");
                    return new List<LocationIQPlace>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var results = JsonSerializer.Deserialize<List<LocationIQPlace>>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                Console.WriteLine($"Found {results?.Count ?? 0} results for '{query}'");
                return results ?? new List<LocationIQPlace>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LocationIQ text search error: {ex.Message}");
                return new List<LocationIQPlace>();
            }
        }
    }

    public class LocationIQPlace
    {
        [System.Text.Json.Serialization.JsonPropertyName("place_id")]
        public string PlaceId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("osm_id")]
        public string OsmId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("osm_type")]
        public string OsmType { get; set; }

        public string Name { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("display_name")]
        public string DisplayName { get; set; }

        public string Lat { get; set; }
        public string Lon { get; set; }
        public string Type { get; set; }
        public string Class { get; set; }

        public LocationIQAddress Address { get; set; }
    }

    public class LocationIQAddress
    {
        public string Road { get; set; }
        public string Suburb { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Postcode { get; set; }
        public string Country { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("country_code")]
        public string CountryCode { get; set; }
    }
}