using System.Reflection;
using Folkie.Application.Common.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Folkie.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }

    private static IServiceCollection AddValidatorsFromAssembly(
        this IServiceCollection services,
        Assembly assembly)
    {
        var validatorType = typeof(FluentValidation.IValidator<>);
        var types = assembly.GetExportedTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Select(t => new
            {
                Type = t,
                Interface = t.GetInterfaces().FirstOrDefault(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition() == validatorType),
            })
            .Where(x => x.Interface is not null);

        foreach (var entry in types)
            services.AddScoped(entry.Interface!, entry.Type);

        return services;
    }
}
