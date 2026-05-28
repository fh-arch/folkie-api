# Folkie API

ASP.NET Core 8 backend for the Folkie platform — markaları nano TikTok creator'larıyla buluşturan B2B pazar yeri.

## Stack

- **ASP.NET Core 8** Web API (Minimal APIs)
- **EF Core 8 + Npgsql** → PostgreSQL (Supabase Free)
- **Clerk** auth (JWT validation + webhook sync)
- **Hangfire** background jobs
- **Serilog + Seq** logging
- **MediatR + FluentValidation** (CQRS pattern)
- **Cloudflare R2** (AWS S3 SDK) — dosya depolama
- **ASP.NET Data Protection** — IBAN şifreleme

## Solution yapısı (Clean Architecture)

```
src/
├── Folkie.Api/                # Web API + endpoints + DI composition root
├── Folkie.Application/        # Use cases (MediatR), interface'ler, validation
├── Folkie.Domain/             # Entity'ler, value object'ler, enum'lar — saf C#
└── Folkie.Infrastructure/     # EF Core, Clerk, R2, Resend, Hangfire impl.
```

Bağımlılık yönü: `Api → Application → Domain` ve `Infrastructure → Application + Domain`. Domain hiçbir framework'e bağlı değildir.

## Lokal Geliştirme

### Önkoşullar
- .NET 8 SDK (mevcut: `~/.dotnet/dotnet`)
- Docker Desktop (postgres + seq için)
- (opsiyonel) `dotnet-ef` global tool: `dotnet tool install -g dotnet-ef --version 8.0.10`

### Kurulum
```bash
# 1. PATH'i ayarla (her terminal session'ında)
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"

# 2. PostgreSQL + Seq başlat
docker compose up -d

# 3. Migration uygula
dotnet ef database update \
  --project src/Folkie.Infrastructure \
  --startup-project src/Folkie.Api

# 4. API'yi çalıştır
dotnet run --project src/Folkie.Api
```

API → http://localhost:5050 (veya launchSettings'teki port)
Swagger → http://localhost:5050/swagger
Seq UI → http://localhost:5341

### Konfigürasyon

`src/Folkie.Api/appsettings.Development.json` lokal ortam içindir. **Gerçek secret'lar oraya commit'lenmez** — production için Azure Key Vault / env vars kullan.

Doldurman gereken alanlar:

| Alan | Nereden alınır |
|---|---|
| `Clerk:Issuer` | Clerk Dashboard → API Keys → "Frontend API URL" (örn. `https://xxx.clerk.accounts.dev`) |
| `Clerk:WebhookSecret` | Clerk Dashboard → Webhooks → endpoint → "Signing Secret" (`whsec_...`) |
| `CloudflareR2:*` | Cloudflare Dashboard → R2 → API Tokens |
| `Resend:ApiKey` | Resend Dashboard → API Keys (`re_...`) |

## EF Core Migration

```bash
# Yeni migration ekle
dotnet ef migrations add MigrationName \
  --project src/Folkie.Infrastructure \
  --startup-project src/Folkie.Api \
  --output-dir Persistence/Migrations

# Veritabanına uygula
dotnet ef database update \
  --project src/Folkie.Infrastructure \
  --startup-project src/Folkie.Api

# Geri al
dotnet ef migrations remove \
  --project src/Folkie.Infrastructure \
  --startup-project src/Folkie.Api
```

## Endpoint'ler (Sprint 0 sonu)

| Path | Auth | Açıklama |
|---|---|---|
| `GET /` | — | Servis bilgisi |
| `GET /healthz` | — | DB sağlık kontrolü |
| `GET /swagger` | dev only | OpenAPI UI |
| `POST /api/v1/webhooks/clerk` | — (Svix imzası ile) | Clerk user.created/updated/deleted senkronizasyonu |
| `GET /api/v1/me` | JWT | Mevcut kullanıcının Folkie kaydı |
| `GET /hangfire` | Admin | Hangfire dashboard |

Sprint 1+'da influencer/brand/admin endpoint'leri eklenir.

## Dağıtım

Production: **Contabo VPS M** (Docker + Caddy + Postgres). Detay için `folkie_web/docs/ARCHITECTURE.md` Bölüm 11.
