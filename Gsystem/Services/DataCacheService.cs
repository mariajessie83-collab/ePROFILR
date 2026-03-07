using System.Net.Http.Json;
using System.Text.Json;
using SharedProject;

namespace Gsystem.Services
{
    public class DataCacheService
    {
        private readonly HttpClient _http;
        private List<School> _allSchools = new();
        private List<string> _divisions = new();
        private bool _isInitialized = false;
        private Task? _initializationTask;

        public DataCacheService(HttpClient http)
        {
            _http = http;
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            if (_initializationTask != null)
            {
                await _initializationTask;
                return;
            }

            _initializationTask = LoadDataInternal();
            await _initializationTask;
        }

        private async Task LoadDataInternal()
        {
            try
            {
                var response = await _http.GetAsync("/api/Auth/schools");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<School>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        _allSchools = apiResponse.Data;
                        _divisions = _allSchools
                            .Where(s => !string.IsNullOrEmpty(s.Division))
                            .Select(s => s.Division!.Trim())
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .OrderBy(d => d)
                            .ToList();
                        
                        _isInitialized = true;
                        Console.WriteLine($"[DataCacheService] Pre-loaded {_allSchools.Count} schools and {_divisions.Count} divisions.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DataCacheService] Error pre-loading data: {ex.Message}");
            }
            finally
            {
                _initializationTask = null;
            }
        }

        public List<string> GetDivisions() => _divisions;

        public List<School> GetSchoolsByDivision(string division)
        {
            if (string.IsNullOrEmpty(division)) return new List<School>();
            
            return _allSchools
                .Where(s => string.Equals(s.Division?.Trim(), division.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s.SchoolName)
                .ToList();
        }

        public bool IsInitialized => _isInitialized;
    }
}
