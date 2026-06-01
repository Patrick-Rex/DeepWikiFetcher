using System.Collections.ObjectModel;

namespace DeepWikiFetcher.Desktop.ViewModels;

/// <summary>
/// 历史页 ViewModel。
/// </summary>
public sealed class HistoryViewModel : ViewModelBase
{
    private readonly SettingsViewModel _settingsViewModel;
    private string _statusMessage = "Ready";

    public HistoryViewModel(SettingsViewModel settingsViewModel)
    {
        _settingsViewModel = settingsViewModel;
        Items = [];
        RefreshCommand = new Command(Refresh);
        OpenDirectoryCommand = new Command<HistoryItem>(async item => await OpenDirectoryAsync(item));
        Refresh();
    }

    /// <summary>历史条目。</summary>
    public ObservableCollection<HistoryItem> Items { get; }

    /// <summary>刷新命令。</summary>
    public Command RefreshCommand { get; }

    /// <summary>打开目录命令。</summary>
    public Command<HistoryItem> OpenDirectoryCommand { get; }

    /// <summary>状态消息。</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    private void Refresh()
    {
        Items.Clear();
        string outputRoot = string.IsNullOrWhiteSpace(_settingsViewModel.OutputRoot)
            ? "Output"
            : _settingsViewModel.OutputRoot;

        if (!Directory.Exists(outputRoot))
        {
            StatusMessage = "No history yet";
            return;
        }

        foreach (string metadataPath in Directory.EnumerateFiles(outputRoot, "_metadata.json", SearchOption.AllDirectories))
        {
            string directory = Path.GetDirectoryName(metadataPath) ?? outputRoot;
            Items.Add(new HistoryItem
            {
                Repository = Path.GetRelativePath(outputRoot, directory),
                OutputPath = directory,
                UpdatedAt = new DateTimeOffset(File.GetLastWriteTimeUtc(metadataPath))
            });
        }

        StatusMessage = Items.Count == 0 ? "No history yet" : $"{Items.Count} records";
    }

    private async Task OpenDirectoryAsync(HistoryItem? item)
    {
        if (item is null || !Directory.Exists(item.OutputPath))
        {
            return;
        }

        await Launcher.OpenAsync(new OpenFileRequest
        {
            File = new ReadOnlyFile(item.OutputPath)
        });
    }
}

/// <summary>
/// 历史记录条目。
/// </summary>
public sealed class HistoryItem
{
    /// <summary>仓库标识。</summary>
    public string Repository { get; set; } = string.Empty;

    /// <summary>输出路径。</summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>更新时间。</summary>
    public DateTimeOffset UpdatedAt { get; set; }
}