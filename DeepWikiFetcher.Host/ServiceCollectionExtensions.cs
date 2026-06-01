using System.Reflection;
using DeepWikiFetcher.Services.Interfaces;
using DeepWikiFetcher.Services.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DeepWikiFetcher.Host;

/// <summary>
/// DI 自动扫描扩展：按约定（IXxx → Xxx，Singleton）自动注册服务。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 扫描指定程序集中所有接口与实现，按约定自动注册为 Singleton。
    /// 约定：接口 IXxx 对应实现 Xxx（去掉 I 前缀），两者在同一程序集或上层程序集中。
    /// </summary>
    public static IServiceCollection AddServicesFromAssembly(
        this IServiceCollection services,
        Assembly assembly)
    {
        var allTypes = assembly.GetExportedTypes();
        var interfaces = allTypes.Where(t => t.IsInterface && t.Name.StartsWith("I")).ToList();
        var implementations = allTypes.Where(t => t.IsClass && !t.IsAbstract).ToList();

        foreach (var iface in interfaces)
        {
            var expectedName = iface.Name[1..]; // 去掉 I 前缀
            var impl = implementations.FirstOrDefault(t =>
                t.Name == expectedName &&
                iface.IsAssignableFrom(t));

            if (impl is not null)
            {
                services.TryAddSingleton(iface, impl);
            }
        }

        return services;
    }

    /// <summary>
    /// 注册所有输出生成器实现。
    /// </summary>
    public static IServiceCollection AddOutputGenerators(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IOutputGenerator, MarkdownWriter>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IOutputGenerator, StaticSiteGenerator>());
        return services;
    }
}
