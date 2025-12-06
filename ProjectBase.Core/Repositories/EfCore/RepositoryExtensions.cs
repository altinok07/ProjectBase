using ProjectBase.Core.Data.Contexts;
using ProjectBase.Core.Entities;
using ProjectBase.Core.Expressions;
using ProjectBase.Core.Pagination;
using ProjectBase.Core.Results;
using System.Diagnostics;
using System.IO;

namespace ProjectBase.Core.Repositories.EfCore;

public static class RepositoryExtensions
{
    public static IQueryable<T> ToQuery<T>(this BaseContext context, GenericExpression<T>? expression = null) where T : BaseEntity
    {
        var query = context.Set<T>().AsQueryable();

        if (expression == null)
            return query;
        if (expression.Predicate != null)
            query = query.Where(expression.Predicate);
        if (expression.IncludePaths != null)
            query = expression.IncludePaths(query);
        if (expression.OrderBy != null)
            query = expression.OrderBy(query);

        return query;
    }

    public static IQueryable<T> ToQuery<T>(this BaseContext context, PagedFilterRequest<T>? filterRequest = null) where T : BaseEntity
    {
        var query = context.Set<T>().AsQueryable();

        if (filterRequest == null)
            return query;
        if (filterRequest.IncludePaths != null)
            query = filterRequest.IncludePaths(query);
        if (filterRequest.Predicate != null)
            query = query.Where(filterRequest.Predicate);
        if (filterRequest.OrderBy != null)
            query = filterRequest.OrderBy(query);

        return query;
    }


    public static Result<T> ExceptionError<T>(Exception ex, string message)
    {
        var baseException = ex.GetBaseException();
        var errors = new List<ErrorResult>
        {
            new ErrorResult(
                propertyName: null,
                errorMessage: baseException.Message,
                errorCode: baseException.GetType().Name)
        };

        // Stack trace detaylarını sadece geliştirme ortamında ekle
        // Production'da güvenlik riski oluşturabilir
        var st = new StackTrace(ex, true);
        if (st?.GetFrames() is { Length: > 0 } frames)
        {
            var relevantFrames = frames
                .Where(f => f.GetFileLineNumber() > 0)
                .Take(5) // İlk 5 frame'i al, çok fazla detay olmasın
                .ToList();

            foreach (var frame in relevantFrames)
            {
                var file = frame.GetFileName();
                var method = frame.GetMethod()?.Name;
                var line = frame.GetFileLineNumber();
                
                errors.Add(new ErrorResult(
                    propertyName: "StackTrace",
                    errorMessage: $"{Path.GetFileName(file)}:{line} in {method}",
                    errorCode: "STACK_TRACE"));
            }
        }

        return Result<T>.Fail(ResultType.InternalServerError, message, errors);
    }
}