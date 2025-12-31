using AutoMapper;
using MediatR;
using ProjectBase.Application.Commands.Users;
using ProjectBase.Core.Repositories.Dapper;
using ProjectBase.Core.Results;
using ProjectBase.Core.Security.Hashing;
using ProjectBase.Domain.Base;
using ProjectBase.Domain.Entities.Users;
using ProjectBase.Domain.Enums;

namespace ProjectBase.Application.Handlers.Users;

internal sealed class UserCreateCommadHandlerDapper(IUnitOfWork repo, IDapperExecutor dapper, IMapper mapper, IHashProperty hashProperty) : IRequestHandler<UserCreateCommandDapper, Result>
{
    private readonly IUnitOfWork _repo = repo;
    private readonly IDapperExecutor _dapper = dapper;
    private readonly IMapper _mapper = mapper;
    private readonly IHashProperty _hashProperty = hashProperty;

    public async Task<Result> Handle(UserCreateCommandDapper request, CancellationToken cancellationToken)
    {
        await _repo.BeginTransactionAsync();

        try
        {
            const string existSql = """
                                    SELECT TOP(1) 1
                                    FROM Users
                                    WHERE Mail = @Mail AND IsDeleted = 0
                                    """;

            var exist = await _dapper.QuerySingleOrDefaultAsync<int?>(
                existSql,
                new { Mail = request.Mail.ToLowerInvariant() },
                cancellationToken: cancellationToken);

            if (!exist.IsSuccess)
            {
                await _repo.RollbackAsync();
                return Result.Fail(ResultType.InternalServerError, "Kullanıcı kontrolü sırasında hata oluştu");
            }

            if (exist.Data is not null)
            {
                await _repo.RollbackAsync();
                return Result.Fail(ResultType.Conflict, "Kullanıcı Sistemde kayıtlı");
            }

            var userId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            var mapped = _mapper.Map<User>(request);
            mapped.Id = userId;
            mapped.PasswordHash = _hashProperty.Hash(request.Password);

            const string insertUserSql = """
                                         INSERT INTO Users
                                         (
                                             Id, Name, Surname, Mail, PasswordHash, UserTypeId,
                                             CreatedDate, CreatedBy, UpdatedDate, UpdatedBy,
                                             IsDeleted, DeletedDate, DeletedBy
                                         )
                                         VALUES
                                         (
                                             @Id, @Name, @Surname, @Mail, @PasswordHash, @UserTypeId,
                                             @CreatedDate, @CreatedBy, @UpdatedDate, @UpdatedBy,
                                             @IsDeleted, @DeletedDate, @DeletedBy
                                         )
                                         """;

            var insertUser = await _dapper.ExecuteAsync(
                insertUserSql,
                new
                {
                    Id = mapped.Id,
                    mapped.Name,
                    mapped.Surname,
                    Mail = mapped.Mail, // already lowercased in mapping
                    mapped.PasswordHash,
                    mapped.UserTypeId,
                    CreatedDate = now,
                    CreatedBy = (string?)null,
                    UpdatedDate = now,
                    UpdatedBy = (string?)null,
                    IsDeleted = false,
                    DeletedDate = (DateTime?)null,
                    DeletedBy = (string?)null
                },
                cancellationToken: cancellationToken);

            if (!insertUser.IsSuccess)
            {
                await _repo.RollbackAsync();
                return Result.Fail(ResultType.InternalServerError, "Kullanıcı Eklenirken bir hata oluştu");
            }

            const string insertUserRoleSql = """
                                             INSERT INTO UserRoles
                                             (
                                                 UserId, RoleId,
                                                 CreatedDate, CreatedBy, UpdatedDate, UpdatedBy,
                                                 IsDeleted, DeletedDate, DeletedBy
                                             )
                                             VALUES
                                             (
                                                 @UserId, @RoleId,
                                                 @CreatedDate, @CreatedBy, @UpdatedDate, @UpdatedBy,
                                                 @IsDeleted, @DeletedDate, @DeletedBy
                                             )
                                             """;

            var insertRole = await _dapper.ExecuteAsync(
                insertUserRoleSql,
                new
                {
                    UserId = mapped.Id,
                    RoleId = (int)UserRoleEnum.TenantAdmin,
                    CreatedDate = now,
                    CreatedBy = (string?)null,
                    UpdatedDate = now,
                    UpdatedBy = (string?)null,
                    IsDeleted = false,
                    DeletedDate = (DateTime?)null,
                    DeletedBy = (string?)null
                },
                cancellationToken: cancellationToken);

            if (!insertRole.IsSuccess)
            {
                await _repo.RollbackAsync();
                return Result.Fail(ResultType.InternalServerError, "Kullanıcı rolü eklenirken bir hata oluştu");
            }

            await _repo.CommitAsync();
            return Result.Success(ResultType.Created, "Kullanıcı Kaydı başarılı");
        }
        catch
        {
            await _repo.RollbackAsync();
            return Result.Fail(ResultType.InternalServerError, "Kullanıcı Eklenirken bir hata oluştu");
        }
    }
}
