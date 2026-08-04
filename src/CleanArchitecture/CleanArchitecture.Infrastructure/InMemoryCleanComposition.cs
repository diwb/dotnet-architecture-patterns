using ArchitecturePatterns.SharedKernel;
using CleanArchitecture.Application;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchitecture.Infrastructure;

public static class InMemoryCleanComposition
{
    public static IServiceCollection AddCleanArchitecture(this IServiceCollection services)
    {
        services.AddSingleton<CommerceState>();
        services.AddSingleton<CleanCommerce>();
        return services;
    }
}
