using Serilog;
using Serilog.Events;
using LabApi.Configuration;
using LabApi.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices(builder.Configuration);

// ═══════════════════════════════════════════════════════════
// Konfigurer Serilog
// ═══════════════════════════════════════════════════════════
// I container logger vi til stdout (12-factor): `podman compose logs api`
// er kanalen. Fillogging skrus på med LOG_TO_FILE=true — den er av som
// standard, fordi containeren kjører non-root og (på torsdag) read-only.
var logToFile = builder.Configuration["LOG_TO_FILE"] is "true";

var loggerConfiguration = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");

if (logToFile)
{
    loggerConfiguration.WriteTo.File(
        "logs/app-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7);
}

Log.Logger = loggerConfiguration.CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();

app.ConfigureHttpPipeline();
app.MapSystemEndpoints();
app.MapControllers();

// ═══════════════════════════════════════════════════════════
// Kjør database-migrasjoner ved oppstart
// ═══════════════════════════════════════════════════════════
// Styreres med miljøvariabelen MIGRATE_ON_STARTUP ("true"/"false").
// Dev og prod-sim: true — container-Postgres starter tom, appen migrerer
// selv. På en ekte jobb settes den til false og migrasjoner kjøres som
// ett eget steg i pipelinen (én runner, én lås, ingen kappløp) — se
// kommentaren i compose.prod.yml.
var migrateOnStartup = builder.Configuration["MIGRATE_ON_STARTUP"] is not "false";
if (migrateOnStartup)
{
    // MIGRATE_MAX_ATTEMPTS: hvor mange forsøk oppstartsmigreringen tåler.
    //
    // Default 1 = ett forsøk, altså uendret atferd: en forbigående DNS-
    // eller nettverksfeil (EAI_AGAIN) gir unhandled exception → exit 139
    // → restart. Det er tilsiktet: torsdagens kappløpsøving leser kappløpet
    // av RestartCount > 0 mens stacken ellers ser grønn ut.
    //
    // Sett MIGRATE_MAX_ATTEMPTS=5 og kappløpet *forsvinner* fra RestartCount.
    // Stacken er fortsatt grønn, men nå har appen begynt å skjule
    // rekkefølgefeilen sin selv — eneste gjenværende vitne er advarselen i
    // loggen. Det er den kontrasten som er poenget.
    var maxAttempts =
        int.TryParse(builder.Configuration["MIGRATE_MAX_ATTEMPTS"], out var parsed) && parsed > 0 ? parsed : 1;

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<LabApi.Data.AppDbContext>();

    for (var attempt = 1; ; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            await LabApi.Data.DbInitializer.Seed(db);
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            var delay = TimeSpan.FromSeconds(Math.Min(attempt * 2, 10));
            Log.Warning(
                ex,
                "Databasen er ikke klar ennå (forsøk {Attempt}/{Max}) — venter {Delay}",
                attempt,
                maxAttempts,
                delay);
            await Task.Delay(delay);
        }
    }
}

app.Run();

// ═══════════════════════════════════════════════════════════
// STEG 3: Tøm bufferet ved shutdown
// ═══════════════════════════════════════════════════════════
Log.CloseAndFlush();
