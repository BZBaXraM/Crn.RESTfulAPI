using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace API.Swagger;

/// <summary>Emits one Swagger document per discovered API version and adds the JWT bearer scheme.</summary>
public sealed class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
    {
        _provider = provider;
    }

    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = "CRN Products API",
                Version = description.ApiVersion.ToString(),
                Description = description.IsDeprecated ? "This API version has been deprecated." : "Products and items CRUD API."
            });
        }

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter the JWT access token."
        });

        options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });

        var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{typeof(ConfigureSwaggerOptions).Assembly.GetName().Name}.xml");
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    }
}
