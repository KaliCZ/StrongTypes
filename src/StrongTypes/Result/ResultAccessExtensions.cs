#nullable enable

namespace StrongTypes;

// C# 14 extension members: `Result<T, TError>.Success` and `.Error` surface the
// branch values as nullables (`Nullable<T>` for structs, `T?` for references),
// enabling the `if (result.Success is {} s)` pattern to unwrap in one
// expression. Split into four classes because the generated accessor signatures
// would otherwise collide between the struct- and class-constrained branches.

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
