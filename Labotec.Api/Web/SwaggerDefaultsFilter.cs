using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Labotec.Api.Web;

public class SwaggerDefaultsFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Parameters is null) return;
        foreach (var p in operation.Parameters)
        {
            if (p.Name.Equals("page", StringComparison.OrdinalIgnoreCase)) p.Schema.Default = new Microsoft.OpenApi.Any.OpenApiInteger(1);
            if (p.Name.Equals("pageSize", StringComparison.OrdinalIgnoreCase)) p.Schema.Default = new Microsoft.OpenApi.Any.OpenApiInteger(20);
            if (p.Name.Equals("sortDir", StringComparison.OrdinalIgnoreCase)) p.Schema.Default = new Microsoft.OpenApi.Any.OpenApiString("asc");
        }
    }
}
