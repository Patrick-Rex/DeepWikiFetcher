using DeepWikiFetcher.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DeepWikiFetcher.Desktop.Pages;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        BindingContext = App.Services?.GetRequiredService<SettingsViewModel>()
            ?? throw new InvalidOperationException("Application services are not initialized.");
    }
}
