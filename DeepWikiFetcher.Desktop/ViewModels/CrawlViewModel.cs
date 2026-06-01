using System.Collections.ObjectModel;
using DeepWikiFetcher.Services.Interfaces;
using DeepWikiFetcher.Shared.Models;

namespace DeepWikiFetcher.Desktop.ViewModels;

/// <summary>
/// 抓取页 ViewModel。
/// </summary>
public sealed class CrawlViewModel : ViewModelBase
{
    private readonly ICrawlOrchestrator _crawlOrchestrator;
    private readonly SettingsViewModel _settingsViewModel;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isRunning;
    private double _progressValue;
    private string _currentPageTitle = string.Empty;
    private string _statusMessage = "Ready";

    public CrawlViewModel(ICrawlOrchestrator crawlOrchestrator, SettingsViewModel settingsViewModel)
    {
        _crawlOrchestrator = crawlOrchestrator;
        _settingsViewModel = settingsViewModel;
        Logs = [];
        StartCommand = new Command(async () => await StartAsync(), () => !IsRunning);
        PauseCommand = new Command(Pause, () => IsRunning);
        CancelCommand = new Command(Cancel, () => IsRunning);
    }

    /// <summary>日志列表。</summary>
    public ObservableCollection<string> Logs { get; }

    /// <summary>开始命令。</summary>
    public Command StartCommand { get; }

    /// <summary>暂停命令。</summary>
    public Command PauseCommand { get; }

    /// <summary>取消命令。</summary>
    public Command CancelCommand { get; }

    /// <summary>是否正在运行。</summary>
    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
            {
                StartCommand.ChangeCanExecute();
                PauseCommand.ChangeCanExecute();
                CancelCommand.ChangeCanExecute();
            }
        }
    }

    /// <summary>进度值。</summary>
    public double ProgressValue
    {
        get => _progressValue;
        private set => SetProperty(ref _progressValue, value);
    }

    /// <summary>当前页面标题。</summary>
    public string CurrentPageTitle
    {
        get => _currentPageTitle;
        private set => SetProperty(ref _currentPageTitle, value);
    }

    /// <summary>状态消息。</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    private async Task StartAsync()
    {
        if (string.IsNullOrWhiteSpace(_settingsViewModel.GitHubUrl))
        {
            StatusMessage = "GitHub URL is required";
            return;
        }

        await _settingsViewModel.SaveAsync();
        _cancellationTokenSource = new CancellationTokenSource();
        IsRunning = true;
        ProgressValue = 0;
        Logs.Clear();
        StatusMessage = "Running";

        var progress = new Progress<CrawlProgress>(OnProgress);
        var options = new CrawlOptions
        {
            GitHubUrl = _settingsViewModel.GitHubUrl,
            OutputRoot = _settingsViewModel.OutputRoot,
            OutputFormat = _settingsViewModel.SelectedOutputFormat,
            TranslationEnabled = _settingsViewModel.TranslationEnabled
        };

        try
        {
            var result = await _crawlOrchestrator.StartAsync(options, progress, _cancellationTokenSource.Token);
            StatusMessage = $"Completed: {result.SuccessCount}/{result.TotalPages}";
            ProgressValue = 1;
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Canceled";
            AddLog("Canceled by user");
        }
        catch (Exception ex)
        {
            StatusMessage = "Failed";
            AddLog(ex.Message);
        }
        finally
        {
            IsRunning = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private void Pause()
    {
        _cancellationTokenSource?.Cancel();
        StatusMessage = "Stopping after current operation";
    }

    private void Cancel()
    {
        _cancellationTokenSource?.Cancel();
        StatusMessage = "Canceling";
    }

    private void OnProgress(CrawlProgress progress)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (progress.TotalPages > 0)
            {
                ProgressValue = Math.Clamp((double)progress.CompletedPages / progress.TotalPages, 0, 1);
            }

            CurrentPageTitle = progress.CurrentPageTitle ?? CurrentPageTitle;
            if (!string.IsNullOrWhiteSpace(progress.LogMessage))
            {
                AddLog($"[{progress.Phase}] {progress.LogMessage}");
            }
        });
    }

    private void AddLog(string message)
    {
        Logs.Insert(0, $"{DateTimeOffset.Now:HH:mm:ss} {message}");
    }
}