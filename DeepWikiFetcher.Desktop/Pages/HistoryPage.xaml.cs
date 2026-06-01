using DeepWikiFetcher.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DeepWikiFetcher.Desktop.Pages;

public partial class HistoryPage : ContentPage
{
    public HistoryPage()
    {
        InitializeComponent();
        BindingContext = App.Services?.GetRequiredService<HistoryViewModel>()
            ?? throw new InvalidOperationException("Application services are not initialized.");
    }
}
