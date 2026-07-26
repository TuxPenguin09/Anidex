using Microsoft.JSInterop;

namespace Anidex.Services;

public class ThemeService
{
    private readonly IJSRuntime _jsRuntime;
    public bool IsDarkMode { get; private set; } = true;

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task ToggleThemeAsync()
    {
        IsDarkMode = !IsDarkMode;
        await ApplyThemeAsync();
    }

    public async Task ApplyThemeAsync()
    {
        await _jsRuntime.InvokeVoidAsync("document.documentElement.classList.toggle", "dark", IsDarkMode);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "theme", IsDarkMode ? "dark" : "light");
    }

    public async Task InitializeAsync()
    {
        var savedTheme = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "theme");
        if (!string.IsNullOrEmpty(savedTheme))
        {
            IsDarkMode = savedTheme == "dark";
        }
        await ApplyThemeAsync();
    }
}
