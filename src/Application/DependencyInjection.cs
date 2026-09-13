using System.Globalization;
using Application.Interfaces;
using Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ValidatorOptions.Global.LanguageManager.Culture = CultureInfo.InvariantCulture;
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IItemService, ItemService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
