using ProjectBase.Core.Entities;
using ProjectBase.Core.Expressions;
using ProjectBase.Core.Pagination;
using ProjectBase.Core.Results;
using System.Linq.Expressions;

namespace ProjectBase.Core.Repositories.EfCore;

public interface IRepository<T> where T : BaseEntity
{
    #region Add
    Task<Result<T?>> AddAsync(T entity);
    Task<Result<IList<T>?>> AddRangeAsync(IList<T> entities);
    #endregion
    #region Update
    Task<Result<T?>> UpdateAsync(T entity);
    Task<Result<IList<T>?>> UpdateRangeAsync(IList<T> entities);
    #endregion
    #region Delete
    Task<Result<bool>> DeleteAsync(T entity);
    Task<Result<bool>> DeleteAsync(Expression<Func<T, bool>> predicate);
    Task<Result<bool>> DeleteRangeAsync(IList<T> entities);
    #endregion
    #region Get
    Task<T?> GetAsync(GenericExpression<T>? parameters = null);
    Task<IEnumerable<T>> GetAllAsync(GenericExpression<T>? parameters = null);
    Task<PagedModel<T>> GetAllPagedAsync(PagedFilterRequest<T> filterRequest);
    Task<int> GetCountAsync(GenericExpression<T>? parameters = null);
    #endregion
}
