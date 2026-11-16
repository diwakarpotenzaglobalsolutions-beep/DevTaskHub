
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using DevTaskHubBackend.Data;
using DevTaskHubBackend.Middleware;
using DevTaskHubBackend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();


builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DevTaskHub API",
        Version = "v1",
        Description = "Developer Task & Issue Tracker API"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter your JWT access token."
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(
                "Bearer", document)] = []
        });
});


builder.Services.AddMySql(
    builder.Configuration,
    builder.Environment);


builder.Services.AddScoped<IAuthServices, AuthServices>();
builder.Services.AddScoped<IRoleManageServices, RoleManageServices>();
builder.Services.AddScoped<IProjectServices, ProjectServices>();
builder.Services.AddScoped<ITaskServices, TaskServices>();
builder.Services.AddScoped<IDashboardServices, DashboardServices>();
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();

var token = builder.Configuration["AppSettings:Token"]
    ?? throw new InvalidOperationException(
        "JWT Token is missing.");

var issuer = builder.Configuration["AppSettings:Issuer"]
    ?? throw new InvalidOperationException(
        "JWT Issuer is missing.");

var audience = builder.Configuration["AppSettings:Audience"]
    ?? throw new InvalidOperationException(
        "JWT Audience is missing.");

if (Encoding.UTF8.GetByteCount(token) < 64)
{
    throw new InvalidOperationException(
        "JWT HS512 signing key must be at least 64 bytes.");
}

var key = new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes(token));


builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = key,

                ClockSkew = TimeSpan.Zero
            };
    });


builder.Services.AddAuthorization(options =>
{

    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});


var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (!builder.Environment.IsDevelopment() && allowedOrigins.All(string.IsNullOrWhiteSpace))
{
    throw new InvalidOperationException(
        "Cors:AllowedOrigins is missing. Set the production frontend origin in appsettings.Production.json or Cors__AllowedOrigins__0.");
}

var forwardedHeadersEnabled = builder.Configuration.GetValue("ForwardedHeaders:Enabled", false);
if (forwardedHeadersEnabled)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.SetIsOriginAllowed(origin => IsAllowedFrontendOrigin(origin, builder.Environment.IsDevelopment(), allowedOrigins))
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

builder.Services.AddSingleton<IGitHubOwnershipVerifier, PublicProfileGitHubOwnershipVerifier>();
builder.Services.AddHttpClient<IGitHubService, GitHubService>(
    client =>
    {
        client.BaseAddress = new Uri("https://api.github.com/");

        client.DefaultRequestHeaders.Add(
            "User-Agent", "DevTaskHub");

        client.DefaultRequestHeaders.Add(
            "Accept", "application/vnd.github+json");

        client.DefaultRequestHeaders.Add(
            "X-GitHub-Api-Version", "2026-03-10");

        var githubToken = builder.Configuration["GitHub:Token"];
        if (!string.IsNullOrWhiteSpace(githubToken))
        {
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", githubToken);
        }

        client.Timeout = TimeSpan.FromSeconds(15);
    });

var app = builder.Build();

if (forwardedHeadersEnabled)
{
    app.UseForwardedHeaders();
}

// Global Exception Middleware
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "DevTaskHub API v1");

    options.RoutePrefix = "swagger";
    options.EnablePersistAuthorization();
});


if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

if (app.Environment.IsDevelopment())
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up) continue;
            foreach (var unicast in nic.GetIPProperties().UnicastAddresses)
            {
                var address = unicast.Address;
                if (address.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(address))
                {
                    continue;
                }

                Console.WriteLine($"DevTaskHub phone URL: http://{address}:5187");
            }
        }
    });
}


app.UseCors("Frontend");


app.UseAuthentication();


app.UseAuthorization();


app.MapControllers();

app.Run();

static bool IsAllowedFrontendOrigin(string origin, bool isDevelopment, string[] allowedOrigins)
{
    if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
    {
        return false;
    }

    if (isDevelopment && uri.Host is "localhost" or "127.0.0.1")
    {
        return true;
    }

    foreach (var allowedOrigin in allowedOrigins)
    {
        if (string.IsNullOrWhiteSpace(allowedOrigin)
            || !Uri.TryCreate(allowedOrigin.Trim(), UriKind.Absolute, out var allowed))
        {
            continue;
        }

        if (string.Equals(allowed.Scheme, uri.Scheme, StringComparison.OrdinalIgnoreCase)
            && string.Equals(allowed.Host, uri.Host, StringComparison.OrdinalIgnoreCase)
            && allowed.Port == uri.Port)
        {
            return true;
        }
    }

    return false;
}
