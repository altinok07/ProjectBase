## ProjectBase

Bu repo; `ProjectBase.Core`, `ProjectBase.Application`, `ProjectBase.Infrastructure` ve `ProjectBase.Api` katmanlarından oluşan örnek bir ASP.NET Core çözümüdür.

## BaseController (`ProjectBase.Core/Api/Controllers/BaseController.cs`)

`BaseController`, API controller’larında **tek tip response** üretmek için kullanılan bir taban sınıftır. Uygulama katmanından dönen `Result` / `Result<T>` nesnelerini `IActionResult`’a çevirir ve **HTTP status code**’u `ResultType` üzerinden otomatik set eder.

### Nedir?

- **Amaç**: Controller içinde `Ok(...)`, `BadRequest(...)`, `NotFound(...)` gibi dönüşleri dağınık şekilde yazmak yerine, her endpoint’in **aynı response formatında** dönmesini sağlamak.
- **Nasıl çalışır**: `CreateActionResult(...)`, HTTP durum kodunu `result.ResponseType` (örn. `ResultType.Success = 200`, `ResultType.Created = 201`) değerinden alır ve body olarak `result` nesnesini döner.
- **Route standardı**: `BaseController` üzerinde `[Route("api/v{version:apiVersion}/[controller]")]` olduğu için, versiyonlama ile URL formatı örneğin `api/v1/User` şeklindedir. Versiyonlama altyapısı için `ProjectBase.Core/Extensions/ApiVersionExtension.cs`’e bakabilirsiniz.

> Not: `Result` içindeki `StatusCode` ve `ResponseType` alanları JSON’da serialize edilmez (client’a gitmez). Client, status code’u HTTP response üzerinden; `IsSuccess`, `Message`, `Errors` ve varsa `Data` alanlarını ise response body’den okur.

### Nasıl kullanılır?

- **Controller’ı BaseController’dan türetin**:
  - `public class XController : BaseController`
- **Action içinde Result dönen çağrıyı CreateActionResult ile dönün**:
  - `return CreateActionResult(result);`
  - `return CreateActionResult(await mediator.Send(request));`
- **Uygulama katmanında doğru ResultType üretin**:
  - Başarılı: `Result.Success(ResultType.Success)` / `Result<T>.Success(ResultType.Created, data)`
  - Hata: `Result.Fail(ResultType.BadRequest, "...")` vb.

### Örnek

Repo içindeki kullanım örneği: `Services/ProjectBase.Api/Controllers/v1/UserController.cs`

```csharp
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProjectBase.Core.Api.Controllers;
using ProjectBase.Application.Queries.Users;

namespace ProjectBase.Api.Controllers.v1;

[ApiVersion(1)]
public class UserController(IMediator mediator) : BaseController
{
    private readonly IMediator _mediator = mediator;

    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] UserLoginQuery request)
        => CreateActionResult(await _mediator.Send(request));
}
```

## BaseContext (`ProjectBase.Core/Data/Contexts/BaseContext.cs`)

`BaseContext`, EF Core `DbContext` için bir taban sınıftır ve `SaveChangesAsync` çağrısında otomatik olarak:

- **Audit alanlarını** doldurur (`CreatedDate/By`, `UpdatedDate/By`)
- **Soft delete** uygular (`IsDeleted`, `DeletedDate/By`)

Bu davranışlar yalnızca `BaseEntity`’den türeyen entity’ler için geçerlidir.

### Nedir?

- **Audit trail**: Entity `Added` ise hem Created hem Updated alanları set edilir; `Modified` ise Created alanlarının değişmesi engellenir ve sadece Updated alanları set edilir.
- **Soft delete**: Entity `Deleted` olduğunda, eğer entity `IHardDelete` *implement etmiyorsa* fiziksel silme yerine `IsDeleted=true` işaretlenir ve delete audit alanları set edilir.
- **Current user**: Audit için kullanıcı bilgisi `HttpContext.User` claim’lerinden okunur (`"UserName"`, `ClaimTypes.Name`, `ClaimTypes.NameIdentifier`).

### Nasıl kullanılır?

- **Kendi DbContext’inizi BaseContext’ten türetin** ve constructor’da `IHttpContextAccessor` geçin.
- **DI kaydı**: `IHttpContextAccessor` register edilmelidir (repo’da `TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>()` ile yapılıyor).
- **Entity’ler**: Audit/soft delete istiyorsanız entity’lerinizi `ProjectBase.Core/Entities/BaseEntity.cs` tabanından türetin. Fiziksel silme istiyorsanız entity’ye `ProjectBase.Core/Entities/IHardDelete` ekleyin.

### Örnek

Repo içindeki kullanım örneği: `Services/ProjectBase.Infrastructure/ApplicationContext.cs`

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ProjectBase.Core.Data.Contexts;

namespace ProjectBase.Infrastructure;

public class ApplicationContext(DbContextOptions options, IHttpContextAccessor httpContextAccessor)
    : BaseContext(options, httpContextAccessor)
{
    // DbSet<User> Users { get; set; }
}
```

## Entities (`ProjectBase.Core/Entities/*`)

Bu klasör, **tüm entity’lerde ortak kullanılacak audit + soft delete alanlarını** standardize eden base tipleri içerir.

### `BaseEntity` (`ProjectBase.Core/Entities/BaseEntity.cs`)

- **Ne sağlar**: `CreatedDate/By`, `UpdatedDate/By`, `IsDeleted`, `DeletedDate/By` alanları.
- **Ne zaman dolar**: Bu alanlar `BaseContext.SaveChangesAsync` sırasında otomatik set edilir.
- **Ne zaman kullanılır**: ID’si farklı bir yapıda olan (veya key’i başka yerden gelen) entity’lerde sadece audit/soft delete tabanı istenirse.

### `BaseEntity<TKey>` (`ProjectBase.Core/Entities/BaseEntity.cs`)

- **Ne sağlar**: `BaseEntity` alanlarına ek olarak **primary key** alanı `Id` (generic).
- **Ne zaman kullanılır**: Çoğu entity için önerilen taban tip; `TKey` olarak `int`, `Guid`, `long`, `string` vb. kullanılabilir.

### `IHardDelete` (`ProjectBase.Core/Entities/EntityInterfaces.cs`)

- **Ne sağlar**: Marker interface’tir. Entity `Deleted` olduğunda `BaseContext`, bu interface’i implement eden entity’ler için **soft delete uygulamaz**; EF Core’un **fiziksel silmesine** izin verir.

### Örnek

Repo içindeki örnek entity: `Services/ProjectBase.Domain/Entities/Users/User.cs`

```csharp
using ProjectBase.Core.Entities;

namespace ProjectBase.Domain.Entities.Users;

public class User : BaseEntity<Guid>
{
    public string Name { get; set; } = null!;
    public string Surname { get; set; } = null!;
}
```

## GenericExpression (`ProjectBase.Core/Expressions/GenericExpression.cs`)

`GenericExpression<T>`, repository katmanına **dinamik sorgu parametreleri** taşımak için kullanılan küçük bir DTO/taşıyıcıdır. `IRepository<T>` içindeki `GetAsync`, `GetAllAsync`, `GetCountAsync` gibi metotlara verilir; içerdiği alanlar `RepositoryExtensions.ToQuery(...)` içinde sırayla uygulanır:

- **`Predicate`**: `Where(...)` filtresi (`Expression<Func<T,bool>>`)
- **`IncludePaths`**: `Include/ThenInclude` zinciri (navigation property’leri eager-load etmek için)
- **`OrderBy`**: sıralama (`OrderBy/OrderByDescending` vb.)

### Nasıl kullanılır?

- En pratik kullanım `GenericExpression<T>.Create(...)` ile tek satırda oluşturmaktır.
- Repository çağrısında `null` geçerseniz ekstra filtre/include/order uygulanmaz.

### Örnek

```csharp
using Microsoft.EntityFrameworkCore;
using ProjectBase.Core.Expressions;
using ProjectBase.Domain.Entities.Users;

var expr = GenericExpression<User>.Create(
    predicate: u => u.Mail == mail && !u.IsDeleted,
    includePaths: q => q.Include(u => u.UserRoles).ThenInclude(ur => ur.Role),
    orderBy: q => q.OrderBy(u => u.CreatedDate)
);

var user = await userRepository.GetAsync(expr);
```

## Paginations / Pagination (`ProjectBase.Core/Pagination/*`)

Bu klasör; UI/API’dan gelen **sayfalama + filtreleme + sıralama** parametrelerini standardize eder ve repository katmanında `GetAllPagedAsync(...)` ile birlikte çalışacak DTO’ları içerir.

### Nedir?

- **`PaginationFilterQuery`** (`ProjectBase.Core/Pagination/PaginationFilterQuery.cs`): İstekten gelen query modelidir.
  - `PageNumber`, `PageSize`
  - `FilterItems` + `FilterLogic` (**and/or**) veya daha gelişmiş kullanım için `FilterGroup` (**parantez + nested group + negate**)
  - `Sorts`: `OrderBy` + `ThenBy` zinciri için çoklu sort desteği
- **`FilterColumnQuery` / `FilterGroupQuery` / `SortQuery`**: Filtre ve sort detaylarını taşır.
- **`PagedFilterRequest<T>`** (`ProjectBase.Core/Pagination/PagedFilterRequest.cs`): Repository’ye giden “hazır” istek modelidir.
  - `Predicate` (Expression), `IncludePaths` (Include/ThenInclude), `OrderBy` (Func)
  - `FilterQuery` (sayfalama + filter/sort DTO’su)
- **`PagedModel<T>`**: Repository paging sonucudur (`PagedData` + `TotalRecords`).
- **`PagedResponse<T>` + `PagedHelper`**: API’ya dönen response modelidir.
  - `PagedResponse<T>`, `Result<T>` pattern’ini genişletir ve `TotalPages/TotalRecords/PageNumber/PageSize` ekler.
  - `PagedHelper.CreatePagedResponse(...)`, total page hesabını yapar.

> Paging implementasyonu: `ProjectBase.Core/Repositories/EfCore/Repository {T}.cs` içindeki `GetAllPagedAsync(...)` metodu; önce `CountAsync()` ile `TotalRecords` bulur, sonra `Skip/Take` uygular.

### Nasıl kullanılır?

- **API Layer**: Client’tan `PaginationFilterQuery` alıp query/command içine koyun.
  - Örn: `UsersPagedQuery.FilterQuery`
- **Handler Layer**:
  - Base predicate’inizi kurun (örn. soft-delete filtreleri).
  - UI’nın göndereceği alanları bir **allow-list** olarak `fieldMap` ile map’leyin.
    - Amaç: UI’nın entity/DB kolon isimlerini bilmesini engellemek ve istenmeyen alanlara query yüzeyi açmamak.
  - `FilterQueryHelper.Filter(...)` ile predicate’i genişletin.
  - `SortQueryHelper.Sort(...)` ile `OrderBy` fonksiyonunu `Sorts`’a göre dinamikleştirin.
  - `PagedFilterRequest<T>` oluşturup `_repo.XRepository.GetAllPagedAsync(...)` çağırın.
  - Dönüşü `PagedHelper.CreatePagedResponse(...)` ile `PagedResponse`’a çevirin.

### `UsersPagedQueryHandler` örnek kullanım

Repo içindeki akış:

- **Endpoint**: `Services/ProjectBase.Api/Controllers/v1/UserController.cs` → `POST api/v1/User/UsersPaged`
- **Query DTO**: `Services/ProjectBase.Application/Queries/Users/UsersPagedQuery.cs`
- **Handler**: `Services/ProjectBase.Application/Handlers/Users/UsersPagedQueryHandler.cs`

**Örnek request body** (client’ın gönderdiği alanlar handler içindeki `fieldMap` allow-list’i ile eşleşmelidir):

```json
{
  "pageNumber": 1,
  "pageSize": 10,
  "filterLogic": "and",
  "filterItems": [
    { "columnField": "name", "operatorValue": "contains", "value": "Ali" },
    { "columnField": "roleName", "operatorValue": "equals", "value": "Admin" }
  ],
  "sorts": [
    { "field": "createdDate", "sort": "desc" }
  ]
}
```

**Örnek request body (OrderBy + ThenBy)**: `sorts` listesinde **ilk eleman `OrderBy`**, sonraki elemanlar **`ThenBy/ThenByDescending`** olarak zincirlenir.

```json
{
  "pageNumber": 2,
  "pageSize": 10,
  "filterLogic": "or",
  "filterItems": [
    { "columnField": "surname", "operatorValue": "startswith", "value": "A" },
    { "columnField": "mail", "operatorValue": "contains", "value": "@company.com" }
  ],
  "sorts": [
    { "field": "surname", "sort": "asc" },
    { "field": "name", "sort": "asc" },
    { "field": "createdDate", "sort": "desc" }
  ]
}
```

**Aynı isteğin C# karşılığı** (handler’a giden `UsersPagedQuery`):

```csharp
using MediatR;
using ProjectBase.Application.Queries.Users;
using ProjectBase.Core.Pagination;

var filter = new PaginationFilterQuery
{
    PageNumber = 1,
    PageSize = 10,
    FilterLogic = "and",
    FilterItems = new()
    {
        new FilterColumnQuery { ColumnField = "name", OperatorValue = "contains", Value = "Ali" },
        new FilterColumnQuery { ColumnField = "roleName", OperatorValue = "equals", Value = "Admin" }
    },
    Sorts = new()
    {
        new SortQuery { Field = "createdDate", Sort = "desc" }
    }
};

var result = await mediator.Send(new UsersPagedQuery { FilterQuery = filter });
```

**Handler tarafında kritik nokta**: UI’dan gelen `columnField/field` değerleri doğrudan entity alanlarına gitmez; önce bir allow-list map’lenir.
Örn. `roleName` → `"UserRoles.Role.Name"` gibi (nested + collection `Any(...)` desteği var).