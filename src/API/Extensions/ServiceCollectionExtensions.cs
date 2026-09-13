using System.Text;
using API.Filters;
using API.Services;
using API.Swagger;
using Application.Interfaces;
using Asp.Versioning;
using Infrastructure.Data;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace API.Extensions;

public static class ServiceCollectionExtensions
{
    public const string CorsPolicyName = "Default";

    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, HttpContextCurrentUserService>();

        services.AddControllers(options =>
        {
            options.Filters.Add<ValidationFilter>();
        });

        services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddMvc()
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        services.AddEndpointsApiExplorer();
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        services.AddSwaggerGen();

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        services.AddCors(options =>
        {
            var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            options.AddPolicy(CorsPolicyName, policy =>
            {
                if (origins.Length == 0)
                {
                    policy.AllowAnyOrigin();
                }
                else
                {
                    policy.WithOrigins(origins).AllowCredentials();
                }

                policy.AllowAnyHeader().AllowAnyMethod();
            });
        });

        services.AddApiAuthentication(configuration);

        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>(name: "database");

        return services;
    }

    private static IServiceCollection AddApiAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                  ?? throw new InvalidOperationException($"Configuration section '{JwtSettings.SectionName}' is missing.");

        if (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey must be at least 32 characters.");
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddAuthorization();

        return services;
    }
}
