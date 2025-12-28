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
        Expression<Func<User, bool>> predicate = I => !I.IsDeleted;
        Func<IQueryable<User>, IIncludableQueryable<User, object>> includePath = P => P.Include(P => P.UserRoles).ThenInclude(I => I.Role);
        Func<IQueryable<User>, IOrderedQueryable<User>> orderBy = O => O.OrderByDescending(I => I.CreatedDate);

        var filterRequest = new PagedFilterRequest<User>(request.FilterQuery)
        {
            Predicate = predicate.Filter(request.FilterQuery),
            IncludePaths = includePath,
            OrderBy = orderBy
        };

        var users = await _repo.UserRepository.GetAllPagedAsync(filterRequest);

        var response = _mapper.Map<IEnumerable<UserResponse>>(users.PagedData);

        return PagedHelper.CreatePagedResponse(response, request.FilterQuery, users.TotalRecords);
    }
}
