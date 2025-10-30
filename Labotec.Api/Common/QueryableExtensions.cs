using System.Linq.Expressions;
namespace Labotec.Api.Common;
public static class QueryableExtensions
{
    public static IQueryable<T> ApplyOrdering<T>(this IQueryable<T> source, string? sortBy, string sortDir)
    {
        if (string.IsNullOrWhiteSpace(sortBy)) return source;
        var p = Expression.Parameter(typeof(T), "x");
        var prop = Expression.PropertyOrField(p, sortBy);
        var lambda = Expression.Lambda(prop, p);
        var method = (sortDir?.ToLower()=="desc") ? "OrderByDescending" : "OrderBy";
        var call = Expression.Call(typeof(Queryable), method, new[] { typeof(T), prop.Type }, source.Expression, Expression.Quote(lambda));
        return source.Provider.CreateQuery<T>(call);
    }
    public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> source, int page, int pageSize)
    { if (page<1) page=1; if (pageSize<1) pageSize=20; return source.Skip((page-1)*pageSize).Take(pageSize); }
}
