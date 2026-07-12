using System.Linq;
using BenchmarkDotNet.Attributes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StrongTypes.EfCore;

namespace StrongTypes.Benchmarks;

// Guards interval persistence parity: a convention-mapped interval (two endpoint columns,
// validated in its constructor on read) against a hand-rolled entity storing the two endpoints as
// plain columns, over in-memory SQLite so any per-row cost shows instead of being lost under a real
// database's I/O. Interval reads carry no materialization interceptor, so they should track plain.

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

file sealed class IntervalContext(SqliteConnection connection) : DbContext
{
    public DbSet<IntervalRow> Rows => Set<IntervalRow>();
    protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlite(connection).UseStrongTypes();
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
    private SqliteConnection _interval = null!;

    [GlobalSetup]
    public void Setup()
    {
        _plain = IntervalBenchmarkDb.OpenMemory();
        _interval = IntervalBenchmarkDb.OpenMemory();
        using (var ctx = new PlainContext(_plain)) ctx.Database.EnsureCreated();
        using (var ctx = new IntervalContext(_interval)) ctx.Database.EnsureCreated();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _plain.Dispose();
        _interval.Dispose();
    }

    [IterationSetup]
    public void ClearTables()
    {
        IntervalBenchmarkDb.Clear(_plain);
        IntervalBenchmarkDb.Clear(_interval);
    }

    [Benchmark(Baseline = true)]
    public void PlainColumns()
    {
        using var ctx = new PlainContext(_plain);
        for (var i = 0; i < N; i++) ctx.Rows.Add(new PlainRow { Start = i, End = i + 10 });
        ctx.SaveChanges();
    }

    [Benchmark]
    public void Interval()
    {
        using var ctx = new IntervalContext(_interval);
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
    private SqliteConnection _interval = null!;

    [GlobalSetup]
    public void Setup()
    {
        _plain = IntervalBenchmarkDb.OpenMemory();
        _interval = IntervalBenchmarkDb.OpenMemory();

        using (var ctx = new PlainContext(_plain))
        {
            ctx.Database.EnsureCreated();
            for (var i = 0; i < N; i++) ctx.Rows.Add(new PlainRow { Start = i, End = i + 10 });
            ctx.SaveChanges();
        }
        using (var ctx = new IntervalContext(_interval))
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
        _interval.Dispose();
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
    public long Interval()
    {
        using var ctx = new IntervalContext(_interval);
        long sum = 0;
        foreach (var row in ctx.Rows.AsNoTracking()) sum += row.Window.Start + row.Window.End;
        return sum;
    }
}
