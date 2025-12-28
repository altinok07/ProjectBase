using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectBase.Application.Queries.Users;
using ProjectBase.Core.Results;
using ProjectBase.Core.Security.Hashing;
using ProjectBase.Core.Security.Jwt;
using ProjectBase.Domain.Base;
using ProjectBase.Model.ResponseModels.Users;
using System.Security.Claims;

namespace ProjectBase.Application.Handlers.Users;

internal sealed class UserLoginQueryHandler(IUnitOfWork repo, IHashProperty hashProperty, JwtTokenGenerator tokenGenerator) : IRequestHandler<UserLoginQuery, Result<UserLoginResponse>>
{
    private readonly IUnitOfWork _repo = repo;
    private readonly IHashProperty _hashProperty = hashProperty;
    private readonly JwtTokenGenerator _tokenGenerator = tokenGenerator;

    public async Task<Result<UserLoginResponse>> Handle(UserLoginQuery request, CancellationToken cancellationToken)
    {
        var user = await _repo.UserRepository.GetAsync(
            new(I => I.Mail == request.Mail && !I.IsDeleted,
            new(P => P.Include(I => I.UserRoles).ThenInclude(I => I.Role))));
        if (user == null)
            return Result<UserLoginResponse>.Fail(ResultType.Unauthorized, "Mail ya da parola hatalı");

        var validatePassword = _hashProperty.Verify(request.Password, user.PasswordHash);

        if (!validatePassword)
            return Result<UserLoginResponse>.Fail(ResultType.Unauthorized, "Mail ya da parola hatalı");

        List<Claim> claims =
            [
            new Claim("UserId", user.Id.ToString()),
            new Claim("UserName", $"{user.Name} {user.Surname}"),
            new Claim("UserTypeId", user.UserTypeId.ToString()),
            ];

        foreach (var userRole in user.UserRoles)
        {
            var roleName = userRole.Role?.Name;
            if (!string.IsNullOrWhiteSpace(roleName))
                claims.Add(new Claim(ClaimTypes.Role, roleName));
        }

        var token = _tokenGenerator.GenerateTokenPair(claims);

        UserLoginResponse response = new()
        {
            Id = user.Id,
            Name = user.Name,
            Surname = user.Surname,
            Mail = user.Mail,
            AccessToken = token.AccessToken,
            AccessTokenExpires = token.AccessTokenExpires,
            RefreshToken = token.RefreshToken,
            RefreshTokenExpires = token.RefreshTokenExpires
        };

        return Result<UserLoginResponse>.Success(ResultType.Success, response, "Girşi Başarılı");
    }
}
