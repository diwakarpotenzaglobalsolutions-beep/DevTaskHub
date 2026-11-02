<<<<<<< Updated upstream
=======

using System.Text;
>>>>>>> Stashed changes
using DevTaskHubBackend.Data;

using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
<<<<<<< Updated upstream
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DevTaskHub API",
        Version = "v1"
    });
});

builder.Services.AddMySql(builder.Configuration, builder.Environment);


var app = builder.Build();

=======


builder.Services.AddMySql(
    builder.Configuration,
    builder.Environment
);



builder.Services.AddScoped<IAuthServices, AuthServices>();



var token = builder.Configuration["AppSettings:Token"]
    ?? throw new InvalidOperationException(
        "JWT Token is missing.");

var issuer = builder.Configuration["AppSettings:Issuer"]
    ?? throw new InvalidOperationException(
        "JWT Issuer is missing.");

var audience = builder.Configuration["AppSettings:Audience"]
    ?? throw new InvalidOperationException(
        "JWT Audience is missing.");


// Validate JWT signing key length for HS512
if (Encoding.UTF8.GetByteCount(token) < 64)
{
    throw new InvalidOperationException(
        "JWT HS512 signing key must be at least 64 bytes.");
}

var key = new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes(token)
);




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




builder.Services.AddAuthorization();




builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DevTaskHub API",
        Version = "v1",
        Description = "Developer Task & Issue Tracker API"
    });

  
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
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
                "Bearer",
                document)] = []
        });
});




var app = builder.Build();



app.UseGlobalExceptionHandling();




>>>>>>> Stashed changes
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "DevTaskHub API v1");
        options.RoutePrefix = "swagger";

      
        options.EnablePersistAuthorization();
    });
}




app.UseHttpsRedirection();

<<<<<<< Updated upstream
=======



app.UseAuthentication();

>>>>>>> Stashed changes
app.UseAuthorization();




app.MapControllers();

<<<<<<< Updated upstream
app.Run();

 
=======


app.Run();
>>>>>>> Stashed changes
