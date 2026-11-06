
using System.Text;
using DevTaskHubBackend.Data;
using DevTaskHubBackend.Middleware;
using DevTaskHubBackend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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


builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            if (!Uri.TryCreate(
                origin,
                UriKind.Absolute,
                out var uri))
            {
                return false;
            }

            return uri.Host is "localhost" or "127.0.0.1";
        })
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

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

        client.Timeout = TimeSpan.FromSeconds(15);
    });

var app = builder.Build();

// Global Exception Middleware
app.UseMiddleware<GlobalExceptionMiddleware>();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "DevTaskHub API v1");

        options.RoutePrefix = "swagger";
        options.EnablePersistAuthorization();
    });
}


app.UseHttpsRedirection();


app.UseCors("Frontend");


app.UseAuthentication();


app.UseAuthorization();


app.MapControllers();

app.Run();
