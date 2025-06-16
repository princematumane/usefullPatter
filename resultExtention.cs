public static class ResultExtensions
{
    #region Ensure Methods
    
    public static Result Ensure(this Result result, Func<bool> predicate, string errorMessage, int? errorCode = null) => 
        result.IsSuccess && !predicate() 
            ? Result.Failure(errorMessage, errorCode) 
            : result;
    
    public static Result<T> Ensure<T>(this Result<T> result, Func<T, bool> predicate, string errorMessage, int? errorCode = null) => 
        result.IsSuccess && !predicate(result.Value) 
            ? Result.Failure<T>(errorMessage, errorCode) 
            : result;
    
    public static async Task<Result> EnsureAsync(this Result result, Func<Task<bool>> predicate, string errorMessage, int? errorCode = null) => 
        result.IsSuccess && !await predicate() 
            ? Result.Failure(errorMessage, errorCode) 
            : result;
    
    public static async Task<Result<T>> EnsureAsync<T>(this Result<T> result, Func<T, Task<bool>> predicate, string errorMessage, int? errorCode = null) => 
        result.IsSuccess && !await predicate(result.Value) 
            ? Result.Failure<T>(errorMessage, errorCode) 
            : result;

    #endregion

    #region Map Methods
    
    public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> mapFunc) => 
        result.IsSuccess 
            ? Result.Success(mapFunc(result.Value)) 
            : Result.Failure<TOut>(result.Error, result.ErrorCode);
    
    public static async Task<Result<TOut>> Map<TIn, TOut>(this Task<Result<TIn>> resultTask, Func<TIn, TOut> mapFunc)
    {
        var result = await resultTask;
        return result.Map(mapFunc);
    }
    
    public static async Task<Result<TOut>> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, Task<TOut>> mapFunc) => 
        result.IsSuccess 
            ? Result.Success(await mapFunc(result.Value)) 
            : Result.Failure<TOut>(result.Error, result.ErrorCode);
    
    public static async Task<Result<TOut>> Map<TIn, TOut>(this Task<Result<TIn>> resultTask, Func<TIn, Task<TOut>> mapFunc)
    {
        var result = await resultTask;
        return await result.Map(mapFunc);
    }

    #endregion

    #region Bind Methods
    
    public static Result<TOut> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Result<TOut>> bindFunc) => 
        result.IsSuccess ? bindFunc(result.Value) : Result.Failure<TOut>(result.Error, result.ErrorCode);
    
    public static async Task<Result<TOut>> Bind<TIn, TOut>(this Task<Result<TIn>> resultTask, Func<TIn, Result<TOut>> bindFunc)
    {
        var result = await resultTask;
        return result.Bind(bindFunc);
    }
    
    public static async Task<Result<TOut>> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Task<Result<TOut>>> bindFunc) => 
        result.IsSuccess ? await bindFunc(result.Value) : Result.Failure<TOut>(result.Error, result.ErrorCode);
    
    public static async Task<Result<TOut>> Bind<TIn, TOut>(this Task<Result<TIn>> resultTask, Func<TIn, Task<Result<TOut>>> bindFunc)
    {
        var result = await resultTask;
        return await result.Bind(bindFunc);
    }

    #endregion

    #region Tap Methods
    
    public static Result Tap(this Result result, Action action)
    {
        if (result.IsSuccess)
            action();
        
        return result;
    }
    
    public static Result<T> Tap<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsSuccess)
            action(result.Value);
        
        return result;
    }
    
    public static async Task<Result> TapAsync(this Result result, Func<Task> action)
    {
        if (result.IsSuccess)
            await action();
        
        return result;
    }
    
    public static async Task<Result<T>> TapAsync<T>(this Result<T> result, Func<T, Task> action)
    {
        if (result.IsSuccess)
            await action(result.Value);
        
        return result;
    }

    #endregion

    #region OnFailure Methods
    
    public static Result OnFailure(this Result result, Action<string, int?> onFailure)
    {
        if (result.IsFailure)
            onFailure(result.Error!, result.ErrorCode);
        
        return result;
    }
    
    public static Result<T> OnFailure<T>(this Result<T> result, Action<string, int?> onFailure)
    {
        if (result.IsFailure)
            onFailure(result.Error!, result.ErrorCode);
        
        return result;
    }
    
    public static async Task<Result> OnFailureAsync(this Task<Result> resultTask, Action<string, int?> onFailure)
    {
        var result = await resultTask;
        return result.OnFailure(onFailure);
    }
    
    public static async Task<Result<T>> OnFailureAsync<T>(this Task<Result<T>> resultTask, Action<string, int?> onFailure)
    {
        var result = await resultTask;
        return result.OnFailure(onFailure);
    }

    #endregion

    #region Try Methods
    
    public static Result Try(Action action, string? errorMessage = null)
    {
        try
        {
            action();
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(errorMessage ?? ex.Message);
        }
    }
    
    public static Result<T> Try<T>(Func<T> func, string? errorMessage = null)
    {
        try
        {
            return Result.Success(func());
        }
        catch (Exception ex)
        {
            return Result.Failure<T>(errorMessage ?? ex.Message);
        }
    }
    
    public static async Task<Result> TryAsync(Func<Task> asyncFunc, string? errorMessage = null)
    {
        try
        {
            await asyncFunc();
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(errorMessage ?? ex.Message);
        }
    }
    
    public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> asyncFunc, string? errorMessage = null)
    {
        try
        {
            return Result.Success(await asyncFunc());
        }
        catch (Exception ex)
        {
            return Result.Failure<T>(errorMessage ?? ex.Message);
        }
    }

    #endregion
}