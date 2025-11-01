using Labotec.Api.Auth;
using Labotec.Api.Data;
using Labotec.Api.Storage;
using Labotec.Api.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Globalization;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration).Enrich.FromLogContext().WriteTo.Console());

// DB MySQL
var cs = ResolveConnectionString(builder.Configuration);
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseMySql(cs, ServerVersion.AutoDetect(cs)));

// Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(opt =>
{
    opt.Password.RequiredLength = 8;
    opt.Password.RequireNonAlphanumeric = false;
}).AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();

// JWT
var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.Configure<JwtSettings>(jwtSection);
var key = Encoding.UTF8.GetBytes(jwtSection["Key"]!);
builder.Services.AddAuthentication(opts =>
{
    opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opts.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opts =>
{
    opts.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<AzureBlobService>();

// CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(opt => opt.AddPolicy("AppCors", p => p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Labotec API", Version = "v1" });
    var bearer = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Bearer token"
    };
    c.AddSecurityDefinition("Bearer", bearer);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { bearer, Array.Empty<string>() } });

    // Defaults for pagination params (Swagger UI presets)
    c.OperationFilter<SwaggerDefaultsFilter>();
});

ConfigurePort(builder);

var app = builder.Build();

if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseCors("AppCors");
app.UseAuthentication();
app.UseAuthorization();

// SortBy validation middleware
app.UseMiddleware<SortValidationMiddleware>();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    await Seed.Run(sp);
}

app.Run();

static string ResolveConnectionString(ConfigurationManager configuration)
{
    var explicitConnection = configuration.GetConnectionString("Default");

    var mysqlUrl = Environment.GetEnvironmentVariable("MYSQL_URL") ??
                   Environment.GetEnvironmentVariable("JAWSDB_URL") ??
                   Environment.GetEnvironmentVariable("CLEARDB_DATABASE_URL") ??
                   Environment.GetEnvironmentVariable("DATABASE_URL");

    if (!string.IsNullOrWhiteSpace(mysqlUrl))
    {
        var normalized = NormalizeMySqlUrl(mysqlUrl);
        if (!string.IsNullOrEmpty(normalized))
        {
            return normalized;
        }
    }

    var mysqlHost = Environment.GetEnvironmentVariable("MYSQLHOST");
    if (!string.IsNullOrWhiteSpace(mysqlHost))
    {
        var mysqlPort = Environment.GetEnvironmentVariable("MYSQLPORT") ?? "3306";
        var mysqlUser = Environment.GetEnvironmentVariable("MYSQLUSER") ?? Environment.GetEnvironmentVariable("MYSQLUSERNAME");
        var mysqlPassword = Environment.GetEnvironmentVariable("MYSQLPASSWORD") ?? Environment.GetEnvironmentVariable("MYSQLPW");
        var mysqlDatabase = Environment.GetEnvironmentVariable("MYSQLDATABASE") ?? Environment.GetEnvironmentVariable("MYSQL_DB");

        var parts = new List<string>
        {
            $"Server={mysqlHost}",
            $"Port={mysqlPort}",
            $"Database={mysqlDatabase ?? "LabotecDb"}"
        };

        if (!string.IsNullOrWhiteSpace(mysqlUser))
        {
            parts.Add($"User={mysqlUser}");
        }

        if (!string.IsNullOrWhiteSpace(mysqlPassword))
        {
            parts.Add($"Password={mysqlPassword}");
        }

        return string.Join(';', parts) + ';';
    }

    return explicitConnection ?? throw new InvalidOperationException("Missing database configuration");
}

static string? NormalizeMySqlUrl(string url)
{
    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
    {
        return null;
    }

    var userInfo = uri.UserInfo?.Split(':', 2, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
    var user = userInfo.ElementAtOrDefault(0);
    var password = userInfo.ElementAtOrDefault(1);
    var database = uri.AbsolutePath.Trim('/');

    if (string.IsNullOrEmpty(database))
    {
        database = "LabotecDb";
    }

    var builder = new List<string>
    {
        $"Server={uri.Host}",
        $"Port={(uri.IsDefaultPort ? 3306 : uri.Port)}",
        $"Database={Uri.UnescapeDataString(database)}"
    };

    if (!string.IsNullOrWhiteSpace(user))
    {
        builder.Add($"User={Uri.UnescapeDataString(user)}");
    }

    if (!string.IsNullOrEmpty(password))
    {
        builder.Add($"Password={Uri.UnescapeDataString(password)}");
    }

    var query = QueryHelpers.ParseQuery(uri.Query);
    foreach (var (key, value) in query)
    {
        if (string.IsNullOrWhiteSpace(key) || value.Count == 0)
        {
            continue;
        }

        var normalizedKey = key switch
        {
            var k when string.Equals(k, "sslmode", StringComparison.OrdinalIgnoreCase) => "SslMode",
            var k when string.Equals(k, "ssl-mode", StringComparison.OrdinalIgnoreCase) => "SslMode",
            _ => key
        };

        var normalizedValue = value[^1];
        if (string.Equals(normalizedKey, "SslMode", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(normalizedValue, "require", StringComparison.OrdinalIgnoreCase))
        {
            normalizedValue = "Required";
        }

        builder.Add($"{normalizedKey}={normalizedValue}");
    }

    return string.Join(';', builder) + ';';
}

static void ConfigurePort(WebApplicationBuilder builder)
{
    var portValue = Environment.GetEnvironmentVariable("PORT");
    if (string.IsNullOrWhiteSpace(portValue))
    {
        return;
    }

    if (!int.TryParse(portValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var port))
    {
        return;
    }

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(port);
    });
}
