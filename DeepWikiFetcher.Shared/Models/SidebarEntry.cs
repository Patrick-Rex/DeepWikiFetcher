namespace DeepWikiFetcher.Shared.Models;

/// <summary>
/// 静态站点侧边栏条目，兼容 VuePress sidebar.json。
/// </summary>
public sealed class SidebarEntry
{
    /// <summary>显示标题。</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>页面相对路径。</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>子条目集合。</summary>
    public List<SidebarEntry>? Children { get; set; }
}