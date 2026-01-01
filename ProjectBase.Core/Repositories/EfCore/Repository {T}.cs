using Microsoft.EntityFrameworkCore;
using ProjectBase.Core.Data.Contexts;
using ProjectBase.Core.Entities;
using ProjectBase.Core.Expressions;
using ProjectBase.Core.Pagination;
using ProjectBase.Core.Results;
using System.Linq.Expressions;

namespace ProjectBase.Core.Repositories.EfCore;

public class Repository<T>(BaseContext context) : IRepository<T> where T : BaseEntity
{
    private readonly BaseContext _context = context;

    #region Add
    public virtual async Task<Result<T?>> AddAsync(T entity)
    {
        try
        {
            _context.Set<T>().Add(entity);

            await _context.SaveChangesAsync();

            return Result<T>.Success(ResultType.Created, entity, "Created")!;
        }
        catch (Exception ex)
        {
            return RepositoryExtensions.ExceptionError<T>(ex, "NotCreated")!;
        }
    }

    public virtual async Task<Result<IList<T>?>> AddRangeAsync(IList<T> entities)
    {
        try
        {
            _context.Set<T>().AddRange(entities);

            await _context.SaveChangesAsync();


            return Result<IList<T>>.Success(ResultType.Created, entities, "Created")!;
        }
        catch (Exception ex)
        {
            return RepositoryExtensions.ExceptionError<IList<T>?>(ex, "NotCreated")!;
        }
    }
    #endregion
    #region Update
    public virtual async Task<Result<T?>> UpdateAsync(T entity)
    {
        try
        {
            _context.Set<T>().Update(entity);
            await _context.SaveChangesAsync();

            return Result<T>.Success(ResultType.Success, entity, "Updated")!;
        }
        catch (Exception ex)
        {
            return RepositoryExtensions.ExceptionError<T>(ex, "NotUpdated")!;
        }
    }

    public virtual async Task<Result<IList<T>?>> UpdateRangeAsync(IList<T> entities)
    {
        try
        {
            _context.Set<T>().UpdateRange(entities);
            await _context.SaveChangesAsync();

            return Result<IList<T>?>.Success(ResultType.Success, entities, "Updated")!;
        }
        catch (Exception ex)
        {
            return RepositoryExtensions.ExceptionError<IList<T>?>(ex, "NotUpdated")!;
        }
    }
    #endregion
    #region Delete
    public virtual async Task<Result<bool>> DeleteAsync(T entity)
    {
        try
        {
            _context.Set<T>().Remove(entity);
            var result = await _context.SaveChangesAsync() > 0;

            return Result<bool>.Success(ResultType.NoContent, result, "Deleted");
        }
        catch (Exception ex)
        {
            return RepositoryExtensions.ExceptionError<bool>(ex, "NotDeleted")!;
        }
    }
    public async Task<Result<bool>> DeleteAsync(Expression<Func<T, bool>> predicate)
    {
        try
        {
            T? entity = await _context.Set<T>().Where(predicate).FirstOrDefaultAsync();
            if (entity == null)
                return Result<bool>.Fail(ResultType.NotFound, "NotDeleted");

            _context.Set<T>().Remove(entity);
            var result = await _context.SaveChangesAsync() > 0;

            return Result<bool>.Success(ResultType.NoContent, result, "Deleted");
        }
        catch (Exception ex)
        {
            return RepositoryExtensions.ExceptionError<bool>(ex, "NotDeleted")!;
        }
    }

    public virtual async Task<Result<bool>> DeleteRangeAsync(IList<T> entities)
    {
        try
        {
            _context.Set<T>().RemoveRange(entities);
            var result = await _context.SaveChangesAsync() > 0;

            return Result<bool>.Success(ResultType.NoContent, result, "Deleted");
        }
        catch (Exception ex)
        {
            return RepositoryExtensions.ExceptionError<bool>(ex, "NotDeleted")!;
        }
    }
    #endregion
    #region Get
    public async Task<IEnumerable<T>> GetAllAsync(GenericExpression<T>? expression)
        => await _context.ToQuery(expression).AsNoTracking().ToListAsync();

    public async Task<T?> GetAsync(GenericExpression<T>? expression)
        => await _context.ToQuery(expression).AsNoTracking().FirstOrDefaultAsync();

    public virtual async Task<PagedModel<T>> GetAllPagedAsync(PagedFilterRequest<T> filterRequest)
    {
        IQueryable<T> query = _context.ToQuery(filterRequest);
        var totalRecords = await query.CountAsync();

        var pageNumber = filterRequest.FilterQuery.PageNumber;
        var pageSize = filterRequest.FilterQuery.PageSize;

        var pagedData = await query
             .AsNoTracking()
             .Skip((pageNumber - 1) * pageSize)
             .Take(pageSize)
             .ToListAsync();

        return new PagedModel<T>(pagedData, totalRecords);
    }

    public virtual async Task<int> GetCountAsync(GenericExpression<T>? expression = null)
    {
        IQueryable<T> query = _context.ToQuery(expression);

        return await query.CountAsync();
    }
    #endregion
}