using System.Linq;
using BenchmarkDotNet.Attributes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StrongTypes.EfCore;

namespace StrongTypes.Benchmarks;

// A plain entity with no interval and no strong type. Guards that UseStrongTypes adds no per-row read
// overhead to entities it never touches — it registers no materialization interceptor.
public sealed class ScalarRow
{
    public int Id { get; set; }
    public int A { get; set; }
    public int B { get; set; }
}

file sealed class PlainScalarContext(SqliteConnection connection) : DbContext
{
    public DbSet<ScalarRow> Rows => Set<ScalarRow>();
    protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlite(connection);
}

file sealed class StrongTypesScalarContext(SqliteConnection connection) : DbContext
{
    public DbSet<ScalarRow> Rows => Set<ScalarRow>();
    protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlite(connection).UseStrongTypes();
}

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class NonIntervalReadBenchmarks
{
    [Params(100000)]
    public int N;

    private SqliteConnection _plain = null!;
    private SqliteConnection _strongTypes = null!;

    [GlobalSetup]
    public void Setup()
    {
        _plain = IntervalBenchmarkDb.OpenMemory();
        _strongTypes = IntervalBenchmarkDb.OpenMemory();
        Seed(new PlainScalarContext(_plain));
        Seed(new StrongTypesScalarContext(_strongTypes));
    }

    private void Seed(DbContext ctx)
    {
        using (ctx)
        {
            ctx.Database.EnsureCreated();
            for (var i = 0; i < N; i++) ctx.Add(new ScalarRow { A = i, B = i + 1 });
            ctx.SaveChanges();
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _plain.Dispose();
        _strongTypes.Dispose();
    }

    [Benchmark(Baseline = true)]
    public long Plain()
    {
        using var ctx = new PlainScalarContext(_plain);
        long sum = 0;
        foreach (var row in ctx.Set<ScalarRow>().AsNoTracking()) sum += row.A + row.B;
        return sum;
    }

    [Benchmark]
    public long WithUseStrongTypes()
    {
        using var ctx = new StrongTypesScalarContext(_strongTypes);
        long sum = 0;
        foreach (var row in ctx.Set<ScalarRow>().AsNoTracking()) sum += row.A + row.B;
        return sum;
    }
}
