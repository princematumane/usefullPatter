using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace YourApplication.Core.Utilities.Results;

/// <summary>
/// Functional operation result pattern for robust error handling and method chaining
/// </summary>
[DebuggerDisplay("{IsSuccess} {Error}")]
public class Result
{
    private static readonly Result _successResult = new(true, null, null);
    
    [MemberNotNullWhen(false, nameof(Error))]
    [MemberNotNullWhen(false, nameof(ErrorCode))]
    public bool IsSuccess { get; }
    
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }
    public int? ErrorCode { get; }
    public IReadOnlyCollection<ErrorDetail>? ErrorDetails { get; }

    protected Result(bool isSuccess, string? error, int? errorCode, IReadOnlyCollection<ErrorDetail>? errorDetails = null)
    {
        if (isSuccess)
        {
            if (!string.IsNullOrWhiteSpace(error))
                throw new InvalidOperationException("Successful result cannot have an error message");
            
            if (errorCode.HasValue)
                throw new InvalidOperationException("Successful result cannot have an error code");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(error))
                throw new InvalidOperationException("Failed result must have an error message");
        }

        IsSuccess = isSuccess;
        Error = error;
        ErrorCode = errorCode;
        ErrorDetails = errorDetails;
    }

    #region Factory Methods
    
    public static Result Success() => _successResult;
    
    public static Result<T> Success<T>(T value) => new(value, true, null, null);
    
    public static Result Failure(string error, int? errorCode = null) => 
        new(false, error ?? throw new ArgumentNullException(nameof(error)), errorCode);
    
    public static Result Failure(string error, IReadOnlyCollection<ErrorDetail> errorDetails, int? errorCode = null)
    {
        if (errorDetails == null || !errorDetails.Any())
            throw new ArgumentException("Error details collection cannot be null or empty", nameof(errorDetails));
        
        return new Result(false, error ?? throw new ArgumentNullException(nameof(error)), errorCode, errorDetails);
    }
    
    public static Result<T> Failure<T>(string error, int? errorCode = null) => 
        new(default!, false, error ?? throw new ArgumentNullException(nameof(error)), errorCode);
    
    public static Result<T> Failure<T>(string error, IReadOnlyCollection<ErrorDetail> errorDetails, int? errorCode = null) => 
        new(default!, false, error ?? throw new ArgumentNullException(nameof(error)), errorCode, errorDetails);
    
    public static Result NotFound(string entityName) => 
        Failure($"{entityName?.Trim() ?? "Entity"} not found", 404);
    
    public static Result ValidationError(string message) => 
        Failure(message, 400);
    
    public static Result ValidationError(IReadOnlyCollection<ErrorDetail> errorDetails) => 
        Failure("Validation failed", errorDetails, 400);
    
    public static Result Unauthorized(string message = "Unauthorized access") => 
        Failure(message, 401);
    
    public static Result Forbidden(string message = "Forbidden access") => 
        Failure(message, 403);
    
    public static Result Conflict(string message) => 
        Failure(message, 409);
    
    public static Result InternalError(string message = "An internal error occurred") => 
        Failure(message, 500);
    
    public static Result ServiceUnavailable(string message = "Service temporarily unavailable") => 
        Failure(message, 503);

    #endregion

    #region Combine Methods
    
    public static Result Combine(params Result[] results)
    {
        var failedResults = results.Where(r => r.IsFailure).ToList();
        
        if (!failedResults.Any())
            return Success();
        
        if (failedResults.Count == 1)
            return failedResults[0];
        
        var combinedError = string.Join(Environment.NewLine, failedResults.Select(r => r.Error));
        var combinedDetails = failedResults
            .Where(r => r.ErrorDetails != null)
            .SelectMany(r => r.ErrorDetails!)
            .ToList();
        
        return combinedDetails.Any() 
            ? Failure(combinedError, combinedDetails, failedResults[0].ErrorCode) 
            : Failure(combinedError, failedResults[0].ErrorCode);
    }
    
    public static async Task<Result> CombineAsync(params Task<Result>[] resultTasks)
    {
        var results = await Task.WhenAll(resultTasks);
        return Combine(results);
    }

    #endregion

    #region Conversion Methods
    
    public Result<T> ToResult<T>(T value) => 
        IsSuccess 
            ? Success(value) 
            : ErrorDetails != null 
                ? Failure<T>(Error, ErrorDetails, ErrorCode) 
                : Failure<T>(Error, ErrorCode);
    
    public Result<T> ToResult<T>() => 
        IsSuccess 
            ? Failure<T>("Cannot convert success result without a value") 
            : ErrorDetails != null 
                ? Failure<T>(Error, ErrorDetails, ErrorCode) 
                : Failure<T>(Error, ErrorCode);
    
    public Result<T> Bind<T>(Func<Result<T>> bindingFunc) => 
        IsSuccess ? bindingFunc() : ToResult<T>();

    #endregion

    #region Match Methods
    
    public void Match(Action onSuccess, Action<string, int?> onFailure)
    {
        if (IsSuccess)
            onSuccess();
        else
            onFailure(Error!, ErrorCode);
    }
    
    public T Match<T>(Func<T> onSuccess, Func<string, int?, T> onFailure) => 
        IsSuccess ? onSuccess() : onFailure(Error!, ErrorCode);

    #endregion

    #region Implicit Operators
    
    public static implicit operator Result(string error) => Failure(error);
    public static implicit operator Result((string error, int code) errorTuple) => 
        Failure(errorTuple.error, errorTuple.code);

    #endregion
}

/// <summary>
/// Generic version of Result for returning values
/// </summary>
[DebuggerDisplay("{IsSuccess} {Value} {Error}")]
public class Result<T> : Result
{
    private readonly T _value;
    
    public T Value => IsSuccess 
        ? _value 
        : throw new InvalidOperationException($"Cannot access {nameof(Value)} of a failed result");
    
    protected internal Result(T value, bool isSuccess, string? error, int? errorCode, 
        IReadOnlyCollection<ErrorDetail>? errorDetails = null)
        : base(isSuccess, error, errorCode, errorDetails)
    {
        _value = value;
    }

    #region Match Methods
    
    public void Match(Action<T> onSuccess, Action<string, int?> onFailure)
    {
        if (IsSuccess)
            onSuccess(_value);
        else
            onFailure(Error!, ErrorCode);
    }
    
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<string, int?, TOut> onFailure) => 
        IsSuccess ? onSuccess(_value) : onFailure(Error!, ErrorCode);

    #endregion

    #region Implicit Operators
    
    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(string error) => Failure<T>(error);
    public static implicit operator Result<T>((string error, int code) errorTuple) => 
        Failure<T>(errorTuple.error, errorTuple.code);

    #endregion
}

/// <summary>
/// Detailed error information for validation or complex error scenarios
/// </summary>
public sealed record ErrorDetail(string Code, string Message, string? Target = null);