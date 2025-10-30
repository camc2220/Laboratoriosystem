using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Labotec.Api.Web;

public class SortValidationMiddleware
{
    private readonly RequestDelegate _next;
    public SortValidationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (context.Request.Method == "GET" && path.StartsWith("/api/"))
        {
            StringValues sortByVals;
            if (context.Request.Query.TryGetValue("sortBy", out sortByVals))
            {
                var sortBy = sortByVals.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(sortBy))
                {
                    // Map endpoint to allowed DTO props quickly
                    var allowed = GetAllowedSortFields(path);
                    if (allowed is not null && !allowed.Contains(sortBy))
                    {
                        context.Response.StatusCode = 400;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "sortBy inválido", allowed }));
                        return;
                    }
                }
            }
        }
        await _next(context);
    }

    private static string[]? GetAllowedSortFields(string path)
    {
        if (path.Contains("/patients"))
            return new[] { "FullName", "DocumentId", "BirthDate" };
        if (path.Contains("/appointments"))
            return new[] { "ScheduledAt", "Type", "Status" };
        if (path.Contains("/results"))
            return new[] { "ReleasedAt", "TestName" };
        if (path.Contains("/invoices"))
            return new[] { "IssuedAt", "Number", "Amount", "Paid" };
        return null;
    }
}
