using StrongTypes.Api.Entities;

namespace StrongTypes.Api.IntegrationTests.Infrastructure;

public static class QueryableExtensions
{
    public static IQueryable<TEntity> FilterById<TEntity>(this IQueryable<TEntity> source, params TEntity[] entities)
        where TEntity : IEntity
    {
        var ids = entities.Select(entity => entity.Id).ToArray();
        return source.Where(entity => ids.Contains(entity.Id));
    }
}
