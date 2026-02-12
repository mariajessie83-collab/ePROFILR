using Microsoft.JSInterop;
using System.Text.Json;

namespace Gsystem.Services
{
    public class OfflineStorageService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

        public OfflineStorageService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
            _moduleTask = new Lazy<Task<IJSObjectReference>>(() => 
                jsRuntime.InvokeAsync<IJSObjectReference>(
                    "import", "/js/offline-storage.js?v=" + DateTime.Now.Ticks).AsTask());
        }

        public async Task InitializeAsync()
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("initDB");
        }

        public async Task<int> AddIncidentReportAsync(object reportData)
        {
            var module = await _moduleTask.Value;
            return await module.InvokeAsync<int>("addIncidentReport", reportData);
        }

        public async Task<List<T>> GetUnsyncedReportsAsync<T>()
        {
            var module = await _moduleTask.Value;
            var result = await module.InvokeAsync<JsonElement>("getUnsyncedReports");
            
            var reports = new List<T>();
            if (result.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in result.EnumerateArray())
                {
                    var report = JsonSerializer.Deserialize<T>(item.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (report != null)
                    {
                        reports.Add(report);
                    }
                }
            }
            return reports;
        }

        public async Task<List<T>> GetAllReportsAsync<T>()
        {
            var module = await _moduleTask.Value;
            var result = await module.InvokeAsync<JsonElement>("getAllReports");
            
            var reports = new List<T>();
            if (result.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in result.EnumerateArray())
                {
                    var report = JsonSerializer.Deserialize<T>(item.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (report != null)
                    {
                        reports.Add(report);
                    }
                }
            }
            return reports;
        }

        public async Task MarkAsSyncedAsync(int id)
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("markAsSynced", id);
        }

        public async Task DeleteReportAsync(int id)
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("deleteReport", id);
        }

        public async Task ClearSyncedReportsAsync()
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("clearSyncedReports");
        }

        public async Task<bool> IsOnlineAsync()
        {
            var module = await _moduleTask.Value;
            return await module.InvokeAsync<bool>("isOnline");
        }

        public async Task<ConnectionStatus> GetConnectionStatusAsync()
        {
            var module = await _moduleTask.Value;
            return await module.InvokeAsync<ConnectionStatus>("getConnectionStatus");
        }

        public async Task DeleteDatabaseAsync()
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("deleteDatabase");
        }

        public async Task SaveStudentsAsync<T>(List<T> students)
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("saveStudents", students);
        }

        public async Task<List<T>> GetCachedStudentsAsync<T>()
        {
            var module = await _moduleTask.Value;
            var result = await module.InvokeAsync<JsonElement>("getCachedStudents");
            
            var students = new List<T>();
            if (result.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in result.EnumerateArray())
                {
                    var student = JsonSerializer.Deserialize<T>(item.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (student != null)
                    {
                        students.Add(student);
                    }
                }
            }
            return students;
        }

        public async Task SaveTeachersAsync<T>(List<T> teachers)
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("saveTeachers", teachers);
        }

        public async Task<List<T>> GetCachedTeachersAsync<T>()
        {
            var module = await _moduleTask.Value;
            var result = await module.InvokeAsync<JsonElement>("getCachedTeachers");
            
            var teachers = new List<T>();
            if (result.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in result.EnumerateArray())
                {
                    var teacher = JsonSerializer.Deserialize<T>(item.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (teacher != null)
                    {
                        teachers.Add(teacher);
                    }
                }
            }
            return teachers;
        }

        public async ValueTask DisposeAsync()
        {
            if (_moduleTask.IsValueCreated)
            {
                var module = await _moduleTask.Value;
                await module.DisposeAsync();
            }
        }
    }

    public class ConnectionStatus
    {
        public bool Online { get; set; }
        public string Timestamp { get; set; } = string.Empty;
    }

    public class OfflineIncidentReport
    {
        public int? Id { get; set; }
        public string ComplainantName { get; set; } = string.Empty;
        public string ComplainantGrade { get; set; } = string.Empty;
        public string? ComplainantStrand { get; set; }
        public string ComplainantSection { get; set; } = string.Empty;
        public string VictimName { get; set; } = string.Empty;
        public string? RoomNumber { get; set; }
        public string VictimContact { get; set; } = string.Empty;
        public string IncidentType { get; set; } = string.Empty;
        public string? OtherIncidentType { get; set; }
        public string IncidentDescription { get; set; } = string.Empty;
        public string RespondentName { get; set; } = string.Empty;
        public string AdviserName { get; set; } = string.Empty;
        public string? PODIncharge { get; set; }
        public string? EvidencePhotoPath { get; set; }
        public string? EvidencePhotoBase64 { get; set; }
        public string DateReported { get; set; } = DateTime.Now.ToString("o");
        public string Status { get; set; } = "Pending";
        public string SchoolName { get; set; } = string.Empty;
        public string Division { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string? Timestamp { get; set; }
        public bool Synced { get; set; }
        public string? SyncedAt { get; set; }
    }
}
