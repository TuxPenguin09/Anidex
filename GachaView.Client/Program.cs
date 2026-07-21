using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace GachaView.Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            builder.Services.AddAuthorizationCore();
            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddAuthenticationStateDeserialization();

            await builder.Build().RunAsync();
        }
    }
}
