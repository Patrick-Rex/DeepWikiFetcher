namespace DeepWikiFetcher.Desktop;

public partial class App : Application
{
    /// <summary>应用级服务提供器。</summary>
    public static IServiceProvider? Services { get; internal set; }

    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}