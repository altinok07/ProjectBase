## ProjectBase.Core — Kullanım Rehberi (ASP.NET Core 10 / Clean Architecture)

Bu doküman, `ProjectBase.Core` kütüphanesini **hiç bilmeyen birinin** kendi `Api / Application / Domain / Infrastructure` çözümüne güvenle entegre edebilmesi için hazırlandı.

`ProjectBase.Core` size şunları “tek yerden, tek standartla” sağlar:

- **Result modeli** (tek tip API response)
- **BaseController** (Result → `IActionResult` çevirimi)
- **JWT + BasicAuth** (tek “smart scheme” ile)
- **OpenAPI** (çoklu doküman + Scalar UI uyumlu security scheme’ler)
- **Global error handling** (middleware)
- **HTTP request/response logging + correlation id** (middleware)
- **MediatR pipeline behaviors** (logging/validation/performance/exception)
- **EF Core için BaseContext** (audit + soft delete)
- **Generic repository + pagination/filter/sort yardımcıları**

> Bu repo içinde ayrıca örnek bir çözüm de var (`Services/*`). İsterseniz birebir aynı kurgu ile ilerleyebilirsiniz; ama `ProjectBase.Core` tek başına da kullanılabilir.

---

## Hızlı Başlangıç (Core’u kendi projene ekle)

### Ön Koşullar

- **.NET SDK**: `net10.0`
- **ASP.NET Core 10**
- (EF kullanacaksanız) bir veritabanı sağlayıcısı (repo örneği: SQL Server)

### 1) Dosyaları/Projeleri çözümüne ekle

Bu repo’da `ProjectBase.Core` paketlerini **`ProjectBase.Packages`** üzerinden alıyor.

- **Önerilen yöntem (Project Reference)**:
  - Çözümünüze `ProjectBase.Core` ve `ProjectBase.Packages` projelerini ekleyin.
  - Root’taki `Directory.Build.props` dosyasını da çözüm köküne taşıyın/kopyalayın. (OpenAPI source generator interceptor namespace’i için)

### 2) Referansları ver

Bu core’u “sarmal” (chain) referans yapısıyla kullanabilirsiniz. Önerilen Clean Architecture referans zinciri:

- **Domain → Core**
- **Infrastructure → Domain**
- **Application → Infrastructure**
- **Api → Application**

Bu sayede **Api projesi Core’u doğrudan referanslamadan** (transitive) Core tiplerine ulaşabilir.

Örnek `csproj` referansları:

```xml
<ItemGroup>
  <!-- Domain.csproj -->
  <ProjectReference Include="..\ProjectBase.Core\ProjectBase.Core.csproj" />
</ItemGroup>
```

```xml
<ItemGroup>
  <!-- Infrastructure.csproj -->
  <ProjectReference Include="..\ProjectBase.Domain\ProjectBase.Domain.csproj" />
</ItemGroup>
```

```xml
<ItemGroup>
  <!-- Application.csproj -->
  <ProjectReference Include="..\ProjectBase.Infrastructure\ProjectBase.Infrastructure.csproj" />
</ItemGroup>
```

```xml
<ItemGroup>
  <!-- Api.csproj -->
  <ProjectReference Include="..\ProjectBase.Application\ProjectBase.Application.csproj" />
</ItemGroup>
```

> Not: Repo örneğinde API, bazı extension’ları doğrudan çağırdığı için (`ProjectBase.Core.Extensions`) API projesi Core’u da referanslıyor. Siz zincir yapıyı tercih ediyorsanız, API’de çağırdığınız extension’ların Application (veya API’ye en yakın) katmanda “wrapper” metotlarla dışarı açılması daha temiz olur.

### 3) `Program.cs` minimum kurulum

Aşağıdaki akış, repo’daki gerçek kurulumla aynıdır:

```csharp
using ProjectBase.Core.Extensions;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .ConfigureLogging()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog(Log.Logger);

var openApiDocuments = builder.Configuration.GetSection("OpenApi:Documents").Get<string[]>() ?? ["v1"];

builder.Services.AddControllers();
builder.Services.AddApiVersion();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddOpenApi(builder.Configuration, openApiDocuments);

var app = builder.Build();

app.MapOpenApi("/openapi/{documentName}.json");
app.MapScalarApiReference(o => o.AddDocuments(openApiDocuments));

app.UseMiddlewares();      // HttpLogging + ExceptionHandling
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### 4) Application katmanı için minimum DI (önerilen)

Bu core, MediatR pipeline ve HTTP logging option’larını da desteklediği için Application tarafında tipik kurulum şöyle olur (repo’da birebir örneği: `Services/ProjectBase.Application/DependencyInjection.cs`):

```csharp
using FluentValidation;
using MediatR;
using ProjectBase.Core.Extensions;
using ProjectBase.Core.Logging.Models;

services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
services.Configure<HttpLoggingOptions>(configuration.GetSection("LoggingOptions"));
services.AddPipelineBehaviors();
```

---

## Zorunlu / Opsiyonel Konfigürasyonlar (appsettings)

### JWT (Zorunlu)

`AddJwtAuthentication(...)` çağrısı `JWT` section’ını zorunlu bekler.

```json
{
  "JWT": {
    "Audience": "YourAudience",
    "Issuer": "YourIssuer",
    "AccessTokenExpiration": 60,
    "RefreshTokenExpiration": 60,
    "AccessTokenSecurityKey": "YOUR_ACCESS_KEY_32+_CHARS",
    "RefreshTokenSecurityKey": "YOUR_REFRESH_KEY_32+_CHARS",
    "ProviderKey": "Your.Auth"
  }
}
```

### BasicAuth (Opsiyonel)

`Authorization: Basic ...` geldiğinde otomatik Basic’e düşer. (Korumalı internal endpoint’ler için pratik.)

```json
{
  "BasicAuth": {
    "User": { "Username": "admin", "Password": "secret" }
  }
}
```

### OpenAPI Documents (Opsiyonel)

Çoklu doküman istiyorsanız:

```json
{ "OpenApi": { "Documents": [ "v1", "v2" ] } }
```

### HTTP Logging Options (Opsiyonel ama önerilir)

`HttpLoggingMiddleware` davranışını `LoggingOptions` ile kontrol edersiniz. Repo’da binding örneği `Services/ProjectBase.Application/DependencyInjection.cs` içindedir.

Örnek:

```json
{
  "LoggingOptions": {
    "enableRequestLogging": true,
    "enableResponseLogging": true,
    "maskSensitiveData": true,
    "maxBodyLength": 65536,
    "correlationHeaderName": "X-Correlation-Id",
    "truncateRequestBody": true,
    "maxRequestBodyBytes": 65536,
    "maxResponseBodyBytes": 65536,
    "responseLogLevel": "Information",
    "errorLogLevel": "Error",
    "sensitiveFieldMatchMode": "Contains",
    "maskWith": "****",
    "sensitiveFields": [ "password", "token", "authorization" ],
    "excludePaths": [ "/openapi", "/health" ]
  }
}
```

---

## “Kullanman gereken” ana metotlar ve ne işe yararlar

### `BaseController.CreateActionResult(...)`

Dosya: `ProjectBase.Core/Api/Controllers/BaseController.cs`

- **Ne sağlar**: Handler’dan dönen `Result` / `Result<T>`’yi HTTP response’a çevirir.
- **Nasıl**: HTTP status code’u `ResultType` üzerinden set eder, body olarak `result` döner.
- **Kullanım**: Controller’ı `BaseController`’dan türetin ve `return CreateActionResult(await _mediator.Send(...));` yazın.

### `Result.Success(...)` / `Result.Fail(...)`

Dosyalar: `ProjectBase.Core/Results/Result.cs`, `ProjectBase.Core/Results/Result {T}.cs`

- **Ne sağlar**: Uygulama katmanında “başarılı/başarısız” standardı.
- **Kritik kural**: `Success(...)` sadece `Success/Created/NoContent` gibi success tipleriyle; `Fail(...)` ise failure tipleriyle çağrılabilir (aksi halde exception fırlatır).

### `services.AddApiVersion()`

Dosya: `ProjectBase.Core/Extensions/ApiVersionExtension.cs`

- **Ne sağlar**: URL segment (`api/v1/...`) + query (`?version=1.0`) + header (`X-Version`) + media-type param (`ver`) ile versioning.
- **OpenAPI entegrasyonu**: `GroupNameFormat = "v1", "v2", ...` gibi doküman gruplarını üretir.

### `services.AddJwtAuthentication(configuration)`

Dosya: `ProjectBase.Core/Extensions/JwtAuthenticationExtension.cs`

- **Ne sağlar**: Tek bir policy scheme (`JWT_OR_BASIC`) ile:
  - Header `Basic ...` ise **BasicAuth**
  - Değilse **JWT Bearer**
- **JWT üretimi**: `JwtTokenGenerator` + `IJwtTokenGenerator` DI’a eklenir.
- **Standart hata body**: 401/403 durumlarında `Result` formatında JSON döner.

### `services.AddOpenApi(configuration, documents)`

Dosya: `ProjectBase.Core/Extensions/OpenApiExtension.cs`

- **Ne sağlar**: Doküman bazlı OpenAPI kaydı (ör. `v1`, `v2`) ve Scalar UI için **Bearer + Basic** security scheme tanımı.
- **Not**: Dokümana global security requirement ekler; anonymous endpoint’leriniz için action bazında `[AllowAnonymous]` kullanabilirsiniz.

### `app.UseMiddlewares()`

Dosya: `ProjectBase.Core/Extensions/MiddlewareExtension.cs`

- **Sıra**:
  - `HttpLoggingMiddleware`: request/response body (uygunsa) loglar, correlation id üretir/taşır.
  - `ExceptionHandlingMiddleware`: exception’ları yakalar ve JSON `Result` döner.

### `services.AddPipelineBehaviors()` (MediatR)

Dosya: `ProjectBase.Core/Extensions/PipelineBehaviorExtension.cs`

- **Ne sağlar**: Handler’larınızda tekrar eden cross-cutting işleri otomatikleştirir:
  - `LoggingBehaviour<,>`
  - `ValidationBehaviour<,>` (FluentValidation)
  - `PerformanceBehaviour<,>`
  - `UnhandledExceptionBehaviour<,>`

### `new LoggerConfiguration().ConfigureLogging()` (Serilog)

Dosya: `ProjectBase.Core/Extensions/ConfigureLoggingExtension.cs`

- **Ne sağlar**: Console + Seq sink’leri ve temel filtreler.
- **Not**: Seq URL’i şu an sabit: `http://localhost:5341` (isterseniz extension içinde config’e bağlayarak geliştirebilirsiniz).

---

## EF Core: Audit + Soft Delete kullanımı

### `BaseContext`

Dosya: `ProjectBase.Core/Data/Contexts/BaseContext.cs`

- **Audit**: `Created*` / `Updated*` alanlarını `SaveChangesAsync` sırasında otomatik doldurur.
- **Soft delete**: `Remove(...)` çağrısında entity `IHardDelete` değilse fiziksel silmez, `IsDeleted=true` yapar.
- **Current user**: `HttpContext.User` claim’lerinden `UserName` → `ClaimTypes.Name` → `ClaimTypes.NameIdentifier` sırasıyla okur.

### `BaseEntity` / `BaseEntity<TKey>`

Dosya: `ProjectBase.Core/Entities/BaseEntity.cs`

- **Ne sağlar**: Audit alanları + `IsDeleted` + (generic tipte) `Id`.

---

## Repository: Core’un sunduğu yüzey (CRUD + paging)

### `IRepository<T>`

Dosya: `ProjectBase.Core/Repositories/EfCore/IRepository.cs`

- **Add**: `AddAsync`, `AddRangeAsync`
- **Update**: `UpdateAsync`, `UpdateRangeAsync`
- **Delete**: `DeleteAsync(entity)`, `DeleteAsync(predicate)`, `DeleteRangeAsync`
- **Get**: `GetAsync`, `GetAllAsync`, `GetAllPagedAsync`, `GetCountAsync`

### `Repository<T>`

Dosya: `ProjectBase.Core/Repositories/EfCore/Repository {T}.cs`

- **Ne sağlar**: EF Core `DbSet<T>` üzerinden generic implementasyon.
- **Okuma**: `AsNoTracking()` kullanır.
- **Paging**: `CountAsync` + `Skip/Take`.

---

## Dapper: Handler içinde hızlı SQL (opsiyonel)

Bu repo’da Dapper entegrasyonu, “EF repository desenini bozmadan” **handler içinde** gerektiğinde ham SQL çalıştırabilmeniz için tasarlandı.

### `IDapperExecutor`

Dosya: `ProjectBase.Core/Repositories/Dapper/IDapperExecutor.cs`

- **Ne sağlar**: Handler’a inject edip `QueryAsync<T> / QuerySingleOrDefaultAsync<T> / ExecuteAsync` ile Dapper sorgusu çalıştırmanızı sağlar.

### `IDbConnectionProvider` + `EfCoreDbConnectionProvider<TContext>`

Dosyalar:

- `ProjectBase.Core/Repositories/Dapper/IDbConnectionProvider.cs`
- `ProjectBase.Core/Repositories/Dapper/EfCoreDbConnectionProvider.cs`

- **Ne sağlar**: Dapper’ın kullanacağı `DbConnection` ve (varsa) `DbTransaction` bilgisini verir.
- **Kritik özellik**: EF Core ile `BeginTransactionAsync()` açıldıysa, Dapper da **aynı connection/transaction** üzerinde çalışır. (Tek `Commit/Rollback` ile yönetim.)

### DI kaydı

Dosya: `ProjectBase.Core/Repositories/DependencyInjection.cs`

Repo örneğinde, Infrastructure katmanında `DbContext` kurulumundan sonra şöyle bağlanır:

```csharp
services.AddRepositories<ApplicationContext>(); // Dapper: IDbConnectionProvider + IDapperExecutor
```

> Not: `AddRepositories<TContext>` generic olduğu için kendi context’inizi verirsiniz (ör. `MyAppContext`).

### Handler’da kullanım örneği (transaction ile)

Transaction gerekiyorsa standardınız `IUnitOfWork` olduğu için, sadece Dapper çalıştıracak olsanız bile transaction’ı UoW ile açıp yönetebilirsiniz:

```csharp
await uow.BeginTransactionAsync();
try
{
    var r = await dapper.ExecuteAsync("UPDATE Users SET Name=@Name WHERE Id=@Id", new { Id = userId, Name = "X" }, cancellationToken: ct);
    if (!r.IsSuccess) { await uow.RollbackAsync(); return Result.Fail(ResultType.InternalServerError, "Dapper failed"); }

    await uow.CommitAsync();
    return Result.Success(ResultType.Success, "OK");
}
catch
{
    await uow.RollbackAsync();
    return Result.Fail(ResultType.InternalServerError, "Transaction failed");
}
```

> Not: Transaction açmazsanız Dapper komutları transaction’sız çalışır (SQL Server’da statement bazında atomik; ama “birden fazla statement tek commit/rollback” olmaz).

---

## Pagination / Filter / Sort (liste ekranları için)

Core içinde liste ekranlarını standartlaştırmak için hazır modeller ve yardımcılar var:

- **İstek modelleri**: `ProjectBase.Core/Pagination/*`
  - `PaginationFilterQuery` (page/filter/sort DTO)
  - `PagedFilterRequest<T>` (repository’ye giden hazır istek)
- **Sonuç modelleri**:
  - `PagedModel<T>` (repository’nin döndürdüğü ham veri + total)
  - `PagedResponse<T>` ve `PagedHelper` (API response formatına çevirme)
- **Helper’lar**: `ProjectBase.Core/Helpers/*`
  - `FilterQueryHelper` (filter’ı `Expression<Func<T,bool>>` predicate’e çevirme)
  - `SortQueryHelper` (sort listesini `OrderBy/ThenBy` zincirine çevirme)

Pratik kullanım kuralı:

- **Client’tan gelen alan isimlerini doğrudan entity alanlarına bağlamayın.** Handler içinde bir **allow-list / fieldMap** oluşturup sadece izinli alanları filtre/sort’a açın.

---

## Migrations (örnek akış)

Bu repo’da Infrastructure altında hazır komut dosyaları var:

- `Services/ProjectBase.Infrastructure/_01-migrations.cmd`: migration ekler (startup project: API)
- `Services/ProjectBase.Infrastructure/_02-update-database.cmd`: database update
- `Services/ProjectBase.Infrastructure/_03-migrations-remove.cmd`: son migration’ı siler

Kendi projenizde aynı yaklaşımı kullanacaksanız, startup project ve context adını kendi isimlerinize göre güncelleyin.

---

## Sık Sorulan Sorular / Sorun Giderme

### 401 dönüyor ama neden?

- `JWT` section yok/eksik olabilir (AddJwtAuthentication bunu zorunlu bekler).
- Token `Issuer/Audience/Key` uyumsuz olabilir.
- `[Authorize]` endpoint’ine token’sız istek atılıyor olabilir.

### Soft delete istemiyorum, fiziksel silsin

- Entity’nize `IHardDelete` marker interface’ini ekleyin. (Dosya: `ProjectBase.Core/Entities/EntityInterfaces.cs`)

### OpenAPI dokümanımda Bearer/Basic çıkmıyor

- `services.AddOpenApi(..., documents)` çağrısını yaptığınızdan emin olun.
- `MapOpenApi("/openapi/{documentName}.json")` route’unu map’leyin.
- Scalar UI için `MapScalarApiReference(...)` ile dokümanları ekleyin.