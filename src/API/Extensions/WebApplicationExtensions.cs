using API.Middleware;
using Asp.Versioning.ApiExplorer;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseMiddleware<SecurityHeadersMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
                }
            });
        }

        // The container image (see src/API/Dockerfile) binds plain HTTP only on 8080 and expects TLS
        // to be terminated in front of it (a reverse proxy / ingress / load balancer), so HSTS and an
        // HTTPS redirect would be actively wrong there: every request, including the compose
        // healthcheck, would 307 to an HTTPS port that doesn't exist. `DOTNET_RUNNING_IN_CONTAINER` is
        // set automatically by Microsoft's own base images.
        var runningInContainer = app.Configuration.GetValue<bool>("DOTNET_RUNNING_IN_CONTAINER");
        if (!runningInContainer)
        {
            if (!app.Environment.IsDevelopment())
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
        }

        app.UseResponseCompression();
        app.UseCors(ServiceCollectionExtensions.CorsPolicyName);
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        // Liveness/readiness probe for orchestrators (Docker Compose healthcheck, Kubernetes, ...).
        // Anonymous: a load balancer or compose healthcheck has no JWT to present.
        app.MapHealthChecks("/health").AllowAnonymous();

        return app;
    }

    /// <summary>
    /// Applies pending EF Core migrations. Skipped in the <c>Testing</c> environment, where
    /// <c>API.Tests</c> creates its Sqlite schema directly via <c>EnsureCreatedAsync</c> instead.
    /// </summary>
    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        if (app.Environment.IsEnvironment("Testing"))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync(app.Lifetime.ApplicationStopping);
    }

    /// <summary>Seeds the admin user when <c>Seed:Admin:Password</c> is configured. Safe to call on every start.</summary>
    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync(app.Lifetime.ApplicationStopping);
    }
}
