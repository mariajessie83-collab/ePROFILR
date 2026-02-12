using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Gsystem.Services;
using Blazored.LocalStorage;

namespace Gsystem
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            // builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(Gsystem.Components.Appsettings.BaseUrl) });
            
            // Register authentication service
            builder.Services.AddScoped<AuthService>();
            
            // Register offline storage service
            builder.Services.AddScoped<OfflineStorageService>();
            
            // Register Blazored.LocalStorage
            builder.Services.AddBlazoredLocalStorage();
            
            await builder.Build().RunAsync();
        }
    }
}
