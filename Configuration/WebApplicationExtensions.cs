namespace LabApi.Configuration;

public static class WebApplicationExtensions
{
    public static WebApplication ConfigureHttpPipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseForwardedHeaders();
        app.UseAuthorization();

        return app;
    }
}
