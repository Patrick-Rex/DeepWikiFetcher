using DeepWikiFetcher.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DeepWikiFetcher.Desktop.Pages;

public partial class CrawlPage : ContentPage
{
    public CrawlPage()
    {
        InitializeComponent();
        BindingContext = App.Services?.GetRequiredService<CrawlViewModel>()
            ?? throw new InvalidOperationException("Application services are not initialized.");
    }
}
