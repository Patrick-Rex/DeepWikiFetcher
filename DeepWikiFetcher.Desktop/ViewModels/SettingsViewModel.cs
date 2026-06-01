using System.Collections.ObjectModel;
using DeepWikiFetcher.Shared.Enums;

namespace DeepWikiFetcher.Desktop.ViewModels;

/// <summary>
/// 设置页 ViewModel。
/// </summary>
public sealed class SettingsViewModel : ViewModelBase
{
    private const string GitHubUrlKey = "Settings.GitHubUrl";
    private const string OutputRootKey = "Settings.OutputRoot";
    private const string OutputFormatKey = "Settings.OutputFormat";
    private const string TranslationEnabledKey = "Settings.TranslationEnabled";
    private const string TranslationBaseUrlKey = "Settings.TranslationBaseUrl";
    private const string TranslationApiKeyKey = "Settings.TranslationApiKey";
    private const string TranslationModelKey = "Settings.TranslationModel";
    private const string MaxConcurrencyKey = "Settings.MaxConcurrency";
    private const string BatchSizeKey = "Settings.BatchSize";
    private const string PlaywrightEnabledKey = "Settings.PlaywrightEnabled";
    private const string DefaultOutputRoot = "Output";
    private const string DefaultTranslationModel = "gpt-4o";

    private string _gitHubUrl = string.Empty;
    private string _outputRoot = DefaultOutputRoot;
    private OutputFormat _selectedOutputFormat = OutputFormat.Markdown;
    private bool _translationEnabled;
    private string _translationBaseUrl = string.Empty;
    private string _translationApiKey = string.Empty;
    private string _translationModel = DefaultTranslationModel;
    private int _maxConcurrency = 3;
    private int _batchSize = 10;
    private bool _playwrightEnabled;
    private string _statusMessage = "Ready";

    public SettingsViewModel()
    {
        OutputFormats = new ObservableCollection<OutputFormat>(Enum.GetValues<OutputFormat>());
        SaveCommand = new Command(async () => await SaveAsync());
        Load();
        _ = LoadSensitiveAsync();
    }

    /// <summary>输出格式选项。</summary>
    public ObservableCollection<OutputFormat> OutputFormats { get; }

    /// <summary>保存命令。</summary>
    public Command SaveCommand { get; }

    /// <summary>GitHub 仓库 URL。</summary>
    public string GitHubUrl
    {
        get => _gitHubUrl;
        set => SetProperty(ref _gitHubUrl, value);
    }

    /// <summary>输出根目录。</summary>
    public string OutputRoot
    {
        get => _outputRoot;
        set => SetProperty(ref _outputRoot, value);
    }

    /// <summary>选中的输出格式。</summary>
    public OutputFormat SelectedOutputFormat
    {
        get => _selectedOutputFormat;
        set => SetProperty(ref _selectedOutputFormat, value);
    }

    /// <summary>是否启用翻译。</summary>
    public bool TranslationEnabled
    {
        get => _translationEnabled;
        set => SetProperty(ref _translationEnabled, value);
    }

    /// <summary>翻译 API Base URL。</summary>
    public string TranslationBaseUrl
    {
        get => _translationBaseUrl;
        set => SetProperty(ref _translationBaseUrl, value);
    }

    /// <summary>翻译 API Key。</summary>
    public string TranslationApiKey
    {
        get => _translationApiKey;
        set => SetProperty(ref _translationApiKey, value);
    }

    /// <summary>翻译模型。</summary>
    public string TranslationModel
    {
        get => _translationModel;
        set => SetProperty(ref _translationModel, value);
    }

    /// <summary>最大并发数。</summary>
    public int MaxConcurrency
    {
        get => _maxConcurrency;
        set => SetProperty(ref _maxConcurrency, value);
    }

    /// <summary>翻译批大小。</summary>
    public int BatchSize
    {
        get => _batchSize;
        set => SetProperty(ref _batchSize, value);
    }

    /// <summary>是否启用 Playwright。</summary>
    public bool PlaywrightEnabled
    {
        get => _playwrightEnabled;
        set => SetProperty(ref _playwrightEnabled, value);
    }

    /// <summary>状态消息。</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    /// 保存设置。
    /// </summary>
    public async Task SaveAsync()
    {
        Preferences.Set(GitHubUrlKey, GitHubUrl);
        Preferences.Set(OutputRootKey, OutputRoot);
        Preferences.Set(OutputFormatKey, SelectedOutputFormat.ToString());
        Preferences.Set(TranslationEnabledKey, TranslationEnabled);
        Preferences.Set(TranslationBaseUrlKey, TranslationBaseUrl);
        Preferences.Set(TranslationModelKey, TranslationModel);
        Preferences.Set(MaxConcurrencyKey, MaxConcurrency);
        Preferences.Set(BatchSizeKey, BatchSize);
        Preferences.Set(PlaywrightEnabledKey, PlaywrightEnabled);

        if (string.IsNullOrWhiteSpace(TranslationApiKey))
        {
            SecureStorage.Remove(TranslationApiKeyKey);
        }
        else
        {
            await SecureStorage.SetAsync(TranslationApiKeyKey, TranslationApiKey);
        }

        StatusMessage = "Saved";
    }

    private void Load()
    {
        GitHubUrl = Preferences.Get(GitHubUrlKey, string.Empty);
        OutputRoot = Preferences.Get(OutputRootKey, DefaultOutputRoot);
        string format = Preferences.Get(OutputFormatKey, OutputFormat.Markdown.ToString());
        SelectedOutputFormat = Enum.TryParse(format, out OutputFormat outputFormat) ? outputFormat : OutputFormat.Markdown;
        TranslationEnabled = Preferences.Get(TranslationEnabledKey, false);
        TranslationBaseUrl = Preferences.Get(TranslationBaseUrlKey, string.Empty);
        TranslationModel = Preferences.Get(TranslationModelKey, DefaultTranslationModel);
        MaxConcurrency = Preferences.Get(MaxConcurrencyKey, 3);
        BatchSize = Preferences.Get(BatchSizeKey, 10);
        PlaywrightEnabled = Preferences.Get(PlaywrightEnabledKey, false);
    }

    private async Task LoadSensitiveAsync()
    {
        TranslationApiKey = await SecureStorage.GetAsync(TranslationApiKeyKey) ?? string.Empty;
    }
}