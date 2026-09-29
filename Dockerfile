# ═══════════════════════════════════════════════════════════
# Multi-stage build for .NET 10
# Stage 1 (build):  SDK-image, stor — kompilerer og publiserer
# Stage 2 (runtime): aspnet-image, liten — inneholder KUN det
#                    som skal kjøre. SDK-en følger ikke med.
# ═══════════════════════════════════════════════════════════

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 1) Kopier prosjektfilene og restore FØRST.
#    Docker cacher dette laget — restore kjører kun når en
#    .csproj endres, ikke når du endrer kode. Raskere bygg.
#    Løsningsfilen MÅ være med: uten den restorer `dotnet restore`
#    bare LabApi.csproj, og testprosjektet mangler project.assets.json
#    når `dotnet test --no-restore` kjører lenger ned.
COPY LabApi.slnx ./
COPY LabApi.csproj ./
COPY tests/LabApi.Tests/LabApi.Tests.csproj tests/LabApi.Tests/
RUN dotnet restore

# 2) Kopier resten av koden og bygg + test + publiser.
#    Testene kjører i bygget: en rød test gir rødt image-bygg.
COPY . .
RUN dotnet test tests/LabApi.Tests/LabApi.Tests.csproj -c Release --no-restore
RUN dotnet publish LabApi.csproj -c Release -o /app/publish

# ── Runtime ──
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Npgsql vil ha GSSAPI-biblioteket. Merk pakkenavnet: det er
# libgssapi-krb5-2 som inneholder libgssapi_krb5.so.2 — IKKE libkrb5-3
# (det gir bare libkrb5.so.3). Feil pakke gir "Cannot load library
# libgssapi_krb5.so.2" på stderr ved oppstart.
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 wget && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Ikke kjør som root. appuser eier /app (og en logs-mappe), slik at
# fillogging (LOG_TO_FILE=true) også virker uten root-rettigheter.
RUN useradd -m appuser \
 && mkdir -p /app/logs \
 && chown -R appuser:appuser /app
USER appuser

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "LabApi.dll"]
