using System.Text.Json;
using Microsoft.JSInterop;
using SharedProject;

namespace Gsystem.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;

        public AuthService(HttpClient httpClient, IJSRuntime jsRuntime)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            try
            {
                // Check if user is logged in by checking localStorage
                var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
                var userRole = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "userRole");
                
                return !string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(userRole);
            }
            catch
            {
                return false;
            }
        }

        public async Task<string?> GetUserRoleAsync()
        {
            try
            {
                return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "userRole");
            }
            catch
            {
                return null;
            }
        }

        public async Task<string?> GetUsernameAsync()
        {
            try
            {
                return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "username");
            }
            catch
            {
                return null;
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "userRole");
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "username");
            }
            catch
            {
                // Ignore errors during logout
            }
        }
    }
}
