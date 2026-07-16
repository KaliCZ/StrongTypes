namespace StrongTypes;

// Four classes because the generated accessor signatures would otherwise collide between the struct- and class-constrained branches.

public static class ResultSuccessStructExtensions
{
    extension<T, TError>(Result<T, TError> r)
        where T : struct
        where TError : notnull
    {
        public T? Success => r.IsSuccess ? r.InternalValue : null;
    }
}

public static class ResultSuccessClassExtensions
{
    extension<T, TError>(Result<T, TError> r)
        where T : class
        where TError : notnull
    {
        public T? Success => r.IsSuccess ? r.InternalValue : null;
    }
}

public static class ResultErrorStructExtensions
{
    extension<T, TError>(Result<T, TError> r)
        where T : notnull
        where TError : struct
    {
        public TError? Error => r.IsError ? r.InternalError : null;
    }
}

public static class ResultErrorClassExtensions
{
    extension<T, TError>(Result<T, TError> r)
        where T : notnull
        where TError : class
    {
        public TError? Error => r.IsError ? r.InternalError : null;
    }
}
