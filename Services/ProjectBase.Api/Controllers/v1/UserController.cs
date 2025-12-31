using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectBase.Application.Commands.Users;
using ProjectBase.Application.Queries.Users;
using ProjectBase.Core.Api.Controllers;
using ProjectBase.Core.Entities;
using ProjectBase.Core.Helpers;
using ProjectBase.Core.Pagination;
using ProjectBase.Core.Security.BasicAuth;
using System.Linq.Expressions;

namespace ProjectBase.Api.Controllers.v1;

[ApiVersion(1)]
public class UserController(IMediator mediator) : BaseController
{
    private readonly IMediator _mediator = mediator;

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost("Register")]
    public async Task<IActionResult> Create([FromBody] UserCreateCommand request)
        => CreateActionResult(await _mediator.Send(request));

    [HttpPost("RegisterDapper")]
    public async Task<IActionResult> RegisterDapper([FromBody] UserCreateCommandDapper request)
    => CreateActionResult(await _mediator.Send(request));

    [AllowAnonymous]
    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] UserLoginQuery request)
        => CreateActionResult(await _mediator.Send(request));

    [Authorize(AuthenticationSchemes = BasicAuthenticationDefaults.AuthenticationScheme)]
    [HttpGet("Users")]
    public async Task<IActionResult> GetAllUser()
    {
        var result = await _mediator.Send(new UsersQuery());
        return CreateActionResult(result);
    }

    [HttpPost("UsersPaged")]
    public async Task<IActionResult> Paged([FromBody] PaginationFilterQuery query)
    {
        var result = await _mediator.Send(new UsersPagedQuery { FilterQuery = query });
        return CreateActionResult(result);
    }

    #region PagedDemo
    /// <summary>
    /// Demo endpoint (POST) to test advanced filter/group/sort with a JSON body.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("FilterDemoSearch")]
    public IActionResult FilterDemoSearch([FromBody] PaginationFilterQuery query)
    {
        var fieldMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // root
            ["orderCode"] = "Code",
            ["total"] = "Total",

            // navigation: Customer.Profile.Address.City
            ["customerName"] = "Customer.Profile.Name",
            ["city"] = "Customer.Profile.Address.City",

            // collection Any: Offers.* (nested supported)
            ["offerPrice"] = "Offers.Price",
            ["offerProductName"] = "Offers.Product.Name",
            ["offerCategoryName"] = "Offers.Product.Category.Name",
            ["offerCount"] = "Offers.Count"
        };

        var data = SeedDemoOrders();

        Expression<Func<DemoOrder, bool>> basePredicate = o => !o.IsDeleted;
        var predicate = basePredicate.Filter(query, fieldMap);

        var q = data.AsQueryable().Where(predicate);

        Func<IQueryable<DemoOrder>, IOrderedQueryable<DemoOrder>> defaultSort =
            x => x.OrderByDescending(o => o.CreatedDate);

        var orderBy = defaultSort.Sort(query, fieldMap, defaultSort: defaultSort);
        q = orderBy(q);

        var totalRecords = q.Count();

        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize < 1 ? 10 : query.PageSize;
        pageSize = Math.Min(pageSize, 50);

        var items = q
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(new
        {
            pageNumber,
            pageSize,
            totalRecords,
            availableFields = fieldMap.Keys.OrderBy(x => x).ToArray(),
            items
        });
    }

    private static List<DemoOrder> SeedDemoOrders()
    {
        var now = DateTime.UtcNow;

        var categories = new[]
        {
            new DemoCategory { Id = Guid.NewGuid(), Name = "CatA" },
            new DemoCategory { Id = Guid.NewGuid(), Name = "CatB" },
            new DemoCategory { Id = Guid.NewGuid(), Name = "CatC" }
        };

        var products = new[]
        {
            new DemoProduct { Id = Guid.NewGuid(), Name = "Keyboard", Category = categories[0] },
            new DemoProduct { Id = Guid.NewGuid(), Name = "Mouse", Category = categories[0] },
            new DemoProduct { Id = Guid.NewGuid(), Name = "Monitor", Category = categories[1] },
            new DemoProduct { Id = Guid.NewGuid(), Name = "Laptop", Category = categories[1] },
            new DemoProduct { Id = Guid.NewGuid(), Name = "Desk", Category = categories[2] },
            new DemoProduct { Id = Guid.NewGuid(), Name = "Chair", Category = categories[2] }
        };

        var names = new[] { "Name42", "Ali", "Veli", "Ayse", "Fatma", "Mehmet", "Ahmet", "Zeynep" };
        var cities = new[] { "Istanbul", "Ankara", "Izmir", "Bursa", "Antalya" };

        var customers = Enumerable.Range(0, 12)
            .Select(i => new DemoCustomer
            {
                Id = Guid.NewGuid(),
                Profile = new DemoProfile
                {
                    Name = names[i % names.Length],
                    Address = new DemoAddress { City = cities[i % cities.Length] }
                }
            })
            .ToArray();

        // deterministic-ish seed (no Random needed)
        var orders = new List<DemoOrder>();
        for (var i = 0; i < 30; i++)
        {
            var cust = customers[i % customers.Length];
            var offerCount = i % 4; // 0..3

            var offers = new List<DemoOffer>();
            for (var j = 0; j < offerCount; j++)
            {
                var prod = products[(i + j) % products.Length];
                var price = 50m + ((i * 13 + j * 7) % 20) * 25m; // 50..525 step 25

                offers.Add(new DemoOffer
                {
                    Id = Guid.NewGuid(),
                    CreatedDate = now.AddDays(-(30 - i)),
                    Price = price,
                    Product = prod
                });
            }

            orders.Add(new DemoOrder
            {
                Id = Guid.NewGuid(),
                CreatedDate = now.AddDays(-(30 - i)),
                Code = $"ORD-{1000 + i}",
                Total = offers.Sum(o => o.Price) + (i % 5) * 100m,
                Customer = cust,
                Offers = offers,
                IsDeleted = i % 17 == 0 // some are deleted; basePredicate excludes them
            });
        }

        return orders;
    }

    // Demo entities (in-memory) - mimic DB structure for filter/sort testing
    private sealed class DemoOrder : BaseEntity<Guid>
    {
        public string Code { get; set; } = "";
        public decimal Total { get; set; }
        public DemoCustomer Customer { get; set; } = new();
        public List<DemoOffer> Offers { get; set; } = new();
    }

    private sealed class DemoCustomer : BaseEntity<Guid>
    {
        public DemoProfile Profile { get; set; } = new();
    }

    private sealed class DemoProfile
    {
        public string Name { get; set; } = "";
        public DemoAddress Address { get; set; } = new();
    }

    private sealed class DemoAddress
    {
        public string City { get; set; } = "";
    }

    private sealed class DemoOffer : BaseEntity<Guid>
    {
        public decimal Price { get; set; }
        public DemoProduct Product { get; set; } = new();
    }

    private sealed class DemoProduct : BaseEntity<Guid>
    {
        public string Name { get; set; } = "";
        public DemoCategory Category { get; set; } = new();
    } 

    private sealed class DemoCategory : BaseEntity<Guid>
    {
        public string Name { get; set; } = "";
    }
    #endregion
}
