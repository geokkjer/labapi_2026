# LabApi — felles kodebase for DevOps-mini

Et lite .NET 10 Web API (produktkatalog mot PostgreSQL). Koden er bevisst
enkel — den er bare en passasjer. Det vi egentlig studerer er
**Dockerfile, compose-filene og CI-pipelinen**.

```
git push ──► GitHub Actions (build → test → GHCR) ──► ghcr.io/<deg>/labapi:sha-a1b2c3d
                                                            │
      UKE 1: podman compose up -d      UKE 2: podman compose -f compose.prod.yml pull && up -d
```

## Prosjektet

| Fil | Hva | Økt |
|---|---|---|
| `Dockerfile` | Multi-stage build: SDK bygger + tester, runtime-image kjører | økt 2 |
| `compose.yml` | Dev: bygger api lokalt + postgres | økt 3–4 |
| `compose.prod.yml` | Prod-sim: puller imaget fra GHCR, bygger ingenting | økt 8 |
| `.env.example` | Kontrakten for miljøvariabler (kopier til `.env`) | økt 4 |
| `.dockerignore` | Hva som aldri havner i et image | økt 2 |
| `.github/workflows/ci.yml` | Pipeline: format → build+test → GHCR | økt 6–7 |
| `.github/workflows/ci-broken.yml` | Bevisst ødelagt workflow til feilsøkingsøkta | økt 9 |
| `tests/LabApi.Tests/` | xUnit-tester — kvalitetsporten i pipelinen | økt 6 |

## Runbook

### Dev (uke 1)

```bash
cp .env.example .env                # én gang
podman compose up -d --build        # start hele stacken
curl --fail http://localhost:8080/health
podman compose logs -f api          # følge med på loggene
podman compose --profile tools up -d   # valgfritt: pgAdmin på http://localhost:5050
podman compose down                 # stoppe (data i volumet overlever)
```

### "Deploy" (uke 2)

Pakken gjøres **public** først (GitHub → Packages → pakken → Package settings →
Change visibility), da trenger `pull` ingen login. På jobben er pakken privat —
da kjøres `podman login ghcr.io` med en PAT (`read:packages`) først.

```bash
# .env: LABAPI_IMAGE=ghcr.io/<deg>/labapi   IMAGE_TAG=<ny sha-fra pipelinen>
podman compose -f compose.prod.yml pull
podman compose -f compose.prod.yml up -d
curl --fail http://localhost:8080/health    # sjekk "version" i svaret!
podman inspect lab-api-prod --format '{{.Config.Image}}'   # beviset
```

PowerShell: `notepad .env` — eller `$env:IMAGE_TAG="sha-a1b2c3d"` i shellen
(gyldig i denne terminalvinduet).

### Rollback

```bash
# .env / $env:IMAGE_TAG sha-<forrige-kort-sha>
podman compose -f compose.prod.yml pull
podman compose -f compose.prod.yml up -d   # bytt container
```

Databasen (volumet) overlever — det er hele poenget med rollback.

## API

| Endpoint | Beskrivelse |
|---|---|
| `GET /health` | `{ "status": "ok", "version": "sha-a1b2c3d", "time": ... }` — version settes av CI, ellers `dev` |
| `GET /api/products` | alle produkter (seedes ved første oppstart) |
| `GET /api/products/{id}` | ett produkt |
| `POST /api/products` | nytt produkt (Name påkrevd, Price ≥ 0) |

## Miljøvariabler

| Variabel | Default | Formål |
|---|---|---|
| `POSTGRES_DB` / `POSTGRES_USER` / `POSTGRES_PASSWORD` | — | db-creds, kun i `.env` |
| `PGADMIN_DEFAULT_EMAIL` / `PGADMIN_DEFAULT_PASSWORD` | — | innlogging til pgAdmin (tools-profilen), kun i `.env` |
| `APP_VERSION` | `dev` | settes av CI til commit-SHA, vises på `/health` |
| `MIGRATE_ON_STARTUP` | `true` | appen kjører EF-migrasjoner ved oppstart (se kommentaren i `compose.prod.yml` om hva som gjelder på jobben) |
| `LOG_TO_FILE` | `false` | `true` skriver også til `logs/app-<dato>.txt` (av i container: stdout er kanalen) |
| `LABAPI_IMAGE` | — | (uke 2) fullt qualified navn på CI-imaget i GHCR |
| `IMAGE_TAG` | `latest` | (uke 2) tag fra grønn pipeline, f.eks. `sha-a1b2c3d` |
| `ConnectionStrings__DefaultConnection` | — | peker på `db`-servicen i nettverket |

## Lokalt uten containere (ikke anbefalt etter økt 3)

```bash
dotnet run           # krever en Postgres på localhost — compose-stakken er veien
dotnet test          # testene trenger ingen database
```
