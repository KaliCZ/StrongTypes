using System.Linq;
using BenchmarkDotNet.Attributes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StrongTypes.EfCore;

namespace StrongTypes.Benchmarks;

// Compares persisting an interval (convention-mapped, integrity interceptor active) against a
// hand-rolled entity that stores the two endpoints as plain columns, over in-memory SQLite so the
// interceptor's in-process cost shows instead of being lost under a real database's I/O.

public sealed class PlainRow
{
    public int Id { get; set; }
    public int Start { get; set; }
    public int End { get; set; }
    public int Tag { get; set; }
}

public sealed class IntervalRow
{
    public int Id { get; set; }
    public FiniteInterval<int> Window { get; set; }
    public int Tag { get; set; }
}

file sealed class PlainContext(SqliteConnection connection) : DbContext
{
    public DbSet<PlainRow> Rows => Set<PlainRow>();
    protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlite(connection);
}

file sealed class IntervalDefaultContext(SqliteConnection connection) : DbContext
{
    public DbSet<IntervalRow> Rows => Set<IntervalRow>();
    protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlite(connection).UseStrongTypes();
}

file sealed class IntervalStoredContext(SqliteConnection connection) : DbContext
{
    public DbSet<IntervalRow> Rows => Set<IntervalRow>();
    protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlite(connection).UseStrongTypes();
    protected override void OnModelCreating(ModelBuilder builder) => builder.Entity<IntervalRow>()
        .HasIntervalColumns(e => e.Window, startBound: IntervalBoundMode.Stored, endBound: IntervalBoundMode.Stored);
}

// Same two endpoint columns as the default, but no UseStrongTypes, so the integrity interceptor
// never runs — the gap to IntervalDefault is exactly the interceptor's per-row read cost.
file sealed class IntervalNoInterceptorContext(SqliteConnection connection) : DbContext
{
    public DbSet<IntervalRow> Rows => Set<IntervalRow>();
    protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlite(connection);
    protected override void OnModelCreating(ModelBuilder builder) => builder.Entity<IntervalRow>().HasIntervalColumns(e => e.Window);
}

internal static class IntervalBenchmarkDb
{
    public static SqliteConnection OpenMemory()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    public static void Clear(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM \"Rows\"";
        command.ExecuteNonQuery();
    }
}

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class IntervalInsertBenchmarks
{
    [Params(1000, 100000)]
    public int N;

    private SqliteConnection _plain = null!;
    private SqliteConnection _default = null!;
    private SqliteConnection _stored = null!;

    [GlobalSetup]
    public void Setup()
    {
        _plain = IntervalBenchmarkDb.OpenMemory();
        _default = IntervalBenchmarkDb.OpenMemory();
        _stored = IntervalBenchmarkDb.OpenMemory();
        using (var ctx = new PlainContext(_plain)) ctx.Database.EnsureCreated();
        using (var ctx = new IntervalDefaultContext(_default)) ctx.Database.EnsureCreated();
        using (var ctx = new IntervalStoredContext(_stored)) ctx.Database.EnsureCreated();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _plain.Dispose();
        _default.Dispose();
        _stored.Dispose();
    }

    [IterationSetup]
    public void ClearTables()
    {
        IntervalBenchmarkDb.Clear(_plain);
        IntervalBenchmarkDb.Clear(_default);
        IntervalBenchmarkDb.Clear(_stored);
    }

    [Benchmark(Baseline = true)]
    public void PlainColumns()
    {
        using var ctx = new PlainContext(_plain);
        for (var i = 0; i < N; i++) ctx.Rows.Add(new PlainRow { Start = i, End = i + 10 });
        ctx.SaveChanges();
    }

    [Benchmark]
    public void IntervalDefault()
    {
        using var ctx = new IntervalDefaultContext(_default);
        for (var i = 0; i < N; i++) ctx.Rows.Add(new IntervalRow { Window = FiniteInterval.Create(i, i + 10) });
        ctx.SaveChanges();
    }

    [Benchmark]
    public void IntervalStored()
    {
        using var ctx = new IntervalStoredContext(_stored);
        for (var i = 0; i < N; i++) ctx.Rows.Add(new IntervalRow { Window = FiniteInterval.Create(i, i + 10) });
        ctx.SaveChanges();
    }
}

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class IntervalReadBenchmarks
{
    [Params(1000, 100000)]
    public int N;

    private SqliteConnection _plain = null!;
    private SqliteConnection _default = null!;
    private SqliteConnection _stored = null!;
    private SqliteConnection _noInterceptor = null!;

    [GlobalSetup]
    public void Setup()
    {
        _plain = IntervalBenchmarkDb.OpenMemory();
        _default = IntervalBenchmarkDb.OpenMemory();
        _stored = IntervalBenchmarkDb.OpenMemory();
        _noInterceptor = IntervalBenchmarkDb.OpenMemory();

        using (var ctx = new PlainContext(_plain))
        {
            ctx.Database.EnsureCreated();
            for (var i = 0; i < N; i++) ctx.Rows.Add(new PlainRow { Start = i, End = i + 10 });
            ctx.SaveChanges();
        }
        using (var ctx = new IntervalDefaultContext(_default))
        {
            ctx.Database.EnsureCreated();
            for (var i = 0; i < N; i++) ctx.Rows.Add(new IntervalRow { Window = FiniteInterval.Create(i, i + 10) });
            ctx.SaveChanges();
        }
        using (var ctx = new IntervalStoredContext(_stored))
        {
            ctx.Database.EnsureCreated();
            for (var i = 0; i < N; i++) ctx.Rows.Add(new IntervalRow { Window = FiniteInterval.Create(i, i + 10) });
            ctx.SaveChanges();
        }
        using (var ctx = new IntervalNoInterceptorContext(_noInterceptor))
        {
            ctx.Database.EnsureCreated();
            for (var i = 0; i < N; i++) ctx.Rows.Add(new IntervalRow { Window = FiniteInterval.Create(i, i + 10) });
            ctx.SaveChanges();
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _plain.Dispose();
        _default.Dispose();
        _stored.Dispose();
        _noInterceptor.Dispose();
    }

    [Benchmark(Baseline = true)]
    public long PlainColumns()
    {
        using var ctx = new PlainContext(_plain);
        long sum = 0;
        foreach (var row in ctx.Rows.AsNoTracking()) sum += row.Start + row.End;
        return sum;
    }

    [Benchmark]
    public long IntervalDefault()
    {
        using var ctx = new IntervalDefaultContext(_default);
        long sum = 0;
        foreach (var row in ctx.Rows.AsNoTracking()) sum += row.Window.Start + row.Window.End;
        return sum;
    }

    [Benchmark]
    public long IntervalStored()
    {
        using var ctx = new IntervalStoredContext(_stored);
        long sum = 0;
        foreach (var row in ctx.Rows.AsNoTracking()) sum += row.Window.Start + row.Window.End;
        return sum;
    }

    [Benchmark]
    public long IntervalNoInterceptor()
    {
        using var ctx = new IntervalNoInterceptorContext(_noInterceptor);
        long sum = 0;
        foreach (var row in ctx.Rows.AsNoTracking()) sum += row.Window.Start + row.Window.End;
        return sum;
    }
}

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class IntervalUpdateBenchmarks
{
    [Params(1000, 100000)]
    public int N;

    private SqliteConnection _plain = null!;
    private SqliteConnection _skip = null!;
    private SqliteConnection _check = null!;

    [GlobalSetup]
    public void Setup()
    {
        _plain = IntervalBenchmarkDb.OpenMemory();
        _skip = IntervalBenchmarkDb.OpenMemory();
        _check = IntervalBenchmarkDb.OpenMemory();
        using (var ctx = new PlainContext(_plain))
        {
            ctx.Database.EnsureCreated();
            for (var i = 0; i < N; i++) ctx.Rows.Add(new PlainRow { Start = i, End = i + 10 });
            ctx.SaveChanges();
        }
        SeedIntervals(_skip);
        SeedIntervals(_check);
    }

    private void SeedIntervals(SqliteConnection connection)
    {
        using var ctx = new IntervalDefaultContext(connection);
        ctx.Database.EnsureCreated();
        for (var i = 0; i < N; i++) ctx.Rows.Add(new IntervalRow { Window = FiniteInterval.Create(i, i + 10) });
        ctx.SaveChanges();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _plain.Dispose();
        _skip.Dispose();
        _check.Dispose();
    }

    [Benchmark(Baseline = true)]
    public void Plain_UpdateScalar()
    {
        using var ctx = new PlainContext(_plain);
        var rows = ctx.Rows.ToList();
        foreach (var row in rows) row.Tag++;
        ctx.SaveChanges();
    }

    // Interval untouched: the modified-skip should keep this near plain.
    [Benchmark]
    public void Interval_UpdateScalar_Skips()
    {
        using var ctx = new IntervalDefaultContext(_skip);
        var rows = ctx.Rows.ToList();
        foreach (var row in rows) row.Tag++;
        ctx.SaveChanges();
    }

    // Interval changed: the bound check actually runs.
    [Benchmark]
    public void Interval_UpdateInterval_Checks()
    {
        using var ctx = new IntervalDefaultContext(_check);
        var rows = ctx.Rows.ToList();
        foreach (var row in rows) row.Window = FiniteInterval.Create(row.Window.Start + 1, row.Window.End + 1);
        ctx.SaveChanges();
    }
}
