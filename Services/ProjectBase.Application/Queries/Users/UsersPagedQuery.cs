using MediatR;
using ProjectBase.Core.Pagination;
using ProjectBase.Core.Results;
using ProjectBase.Model.ResponseModels.Users;

namespace ProjectBase.Application.Queries.Users;

public class UsersPagedQuery : IRequest<Result<IEnumerable<UserResponse>>>
{
    /// <summary>
    /// Pagination + filter + sort query coming from the API layer.
    /// Keep this DTO OpenAPI-friendly (no Expression/Func properties).
    /// </summary>
    public PaginationFilterQuery FilterQuery { get; set; } = new();
}
