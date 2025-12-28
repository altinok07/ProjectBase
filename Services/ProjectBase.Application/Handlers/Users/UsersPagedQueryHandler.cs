using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using ProjectBase.Application.Queries.Users;
using ProjectBase.Core.Helpers;
using ProjectBase.Core.Pagination;
using ProjectBase.Core.Results;
using ProjectBase.Domain.Base;
using ProjectBase.Domain.Entities.Users;
using ProjectBase.Model.ResponseModels.Users;
using System.Linq.Expressions;

namespace ProjectBase.Application.Handlers.Users;

internal sealed class UsersPagedQueryHandler(IUnitOfWork repo, IMapper mapper) : IRequestHandler<UsersPagedQuery, Result<IEnumerable<UserResponse>>>
{
    private readonly IUnitOfWork _repo = repo;
    private readonly IMapper _mapper = mapper;

    public async Task<Result<IEnumerable<UserResponse>>> Handle(UsersPagedQuery request, CancellationToken cancellationToken)
    {
        // UI should send these keys (not entity/DB column names). We map keys -> entity paths (allow-list).
        // This prevents exposing internal model details and limits query surface area.
        var fieldMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // root scalar fields
            ["id"] = nameof(User.Id),
            ["name"] = nameof(User.Name),
            ["surname"] = nameof(User.Surname),
            ["mail"] = nameof(User.Mail),
            ["createdDate"] = nameof(User.CreatedDate),

            // nested (filter-only): UserRoles.Any(ur => ur.Role.Name ...)
            ["roleName"] = "UserRoles.Role.Name"
        };

        Expression<Func<User, bool>> predicate = I => !I.IsDeleted;
        Func<IQueryable<User>, IIncludableQueryable<User, object>> includePath = P => P.Include(P => P.UserRoles).ThenInclude(I => I.Role);
        Func<IQueryable<User>, IOrderedQueryable<User>> orderBy = O => O.OrderByDescending(I => I.CreatedDate);

        var filterRequest = new PagedFilterRequest<User>(request.FilterQuery)
        {
            Predicate = predicate.Filter(request.FilterQuery, fieldMap),
            IncludePaths = includePath,
            OrderBy = orderBy.Sort(request.FilterQuery, fieldMap, defaultSort: orderBy)
        };

        var users = await _repo.UserRepository.GetAllPagedAsync(filterRequest);

        var response = _mapper.Map<IEnumerable<UserResponse>>(users.PagedData);

        return PagedHelper.CreatePagedResponse(response, request.FilterQuery, users.TotalRecords);
    }
}
