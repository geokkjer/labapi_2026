namespace LabApi.Endpoints;

public static class SystemEndpoints
{
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", (ILogger<Program> logger) =>
        {
            logger.LogInformation("Root endpoint called. App={AppName}, Time={Time}",
                "LabApi", DateTimeOffset.UtcNow);

            return Results.Ok(new
            {
                app = "LabApi",
                message = "Lab API is running",
                endpoints = new[] { "/health", "/api/products" }
            });
        });

        endpoints.MapGet("/health", (IConfiguration config, ILogger<Program> logger) =>
        {
            var now = DateTimeOffset.UtcNow;

            // Logg kun ved Debug-nivå — nyttig for feilsøking, men støy i produksjon
            logger.LogDebug("Health check executed at {HealthCheckTime}", now);

            // APP_VERSION settes av CI til commit-SHA (sha-a1b2c3d).
            // Uten den (lokal dotnet run) vises "dev".
            // Rollback-økta bruker denne til å bevise hvilken versjon som kjører.
            return Results.Ok(new
            {
                status = "ok",
                version = config["APP_VERSION"] ?? "dev",
                time = now
            });
        });

        return endpoints;
    }
}
