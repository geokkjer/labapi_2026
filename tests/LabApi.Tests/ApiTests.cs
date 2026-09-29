using LabApi.Models;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LabApi.Tests;

// Små tester som gir store læringsgevinster i kurset:
// De er grønne i CI — og i økt 9 (failure injection) gjør vi én av dem
// rød for å se at en feilende test stopper pipelinen før deploy.
public class ApiTests
{
    // ── Domene: produktmodellen oppfører seg som forventet ──
    [Fact]
    public void Product_DefaultsAreSane()
    {
        var product = new Product { Name = "Laptop", Price = 12999.99m };

        Assert.Equal(0, product.Id);                      // Id settes av databasen
        Assert.Equal("Laptop", product.Name);
        Assert.True(product.Price >= 0);                  // priser skal aldri være negative
    }

    // ── 12-factor config: APP_VERSION leses fra miljø, ikke kode ──
    [Fact]
    public void AppVersion_ComesFromEnvironment()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["APP_VERSION"] = "sha-a1b2c3d"
            })
            .Build();

        Assert.Equal("sha-a1b2c3d", config["APP_VERSION"]);
    }

    // ── /health rapporterer "dev" når CI ikke har satt APP_VERSION ──
    [Fact]
    public void AppVersion_FallsBackToDev_WhenUnset()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var version = config["APP_VERSION"] ?? "dev";

        Assert.Equal("dev", version);
    }

    // ── MIGRATE_ON_STARTUP: prod kan slå av auto-migrering ──
    [Theory]
    [InlineData("false", false)]
    [InlineData("true", true)]
    [InlineData(null, true)]   // default: migrer ved oppstart (dev)
    public void MigrateOnStartup_EnvVarControlsBehavior(string? value, bool expected)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MIGRATE_ON_STARTUP"] = value
            })
            .Build();

        var migrateOnStartup = config["MIGRATE_ON_STARTUP"] is not "false";

        Assert.Equal(expected, migrateOnStartup);
    }
}
