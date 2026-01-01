using ProjectBase.Core.Results;
using System.Diagnostics;

namespace ProjectBase.Core.Repositories.Dapper;

public static class DapperRepositoryExtensions
{
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

        // Keep stack trace details limited (similar to EfCore.RepositoryExtensions)
        var st = new StackTrace(ex, true);
        if (st?.GetFrames() is { Length: > 0 } frames)
        {
            var relevantFrames = frames
                .Where(f => f.GetFileLineNumber() > 0)
                .Take(5)
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


