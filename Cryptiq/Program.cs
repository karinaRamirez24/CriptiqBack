using CryptiqChat.Data;
using CryptiqChat.Hubs;
using CryptiqChat.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
       policy => policy
       .AllowAnyHeader()
       .AllowAnyMethod()
       .AllowCredentials()
       .SetIsOriginAllowed(_ => true));

});
// Configuración JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });
// Twilio code
builder.Configuration["Twilio:AccountSid"] = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID");
builder.Configuration["Twilio:AuthToken"] = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN");
builder.Configuration["Twilio:FromPhone"] = Environment.GetEnvironmentVariable("TWILIO_FROM_PHONE");

builder.Services.AddAuthorization();
builder.Services.AddScoped<JwtService>();
// DbContext con SQL Server
builder.Services.AddDbContext<CryptiqDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CryptiqDB")));

// Registrar servicios
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<SmsService>();

// SignalR
builder.Services.AddSignalR();

// Controladores REST
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Cryptiq API", Version = "v1" });

    // Configuración para el botón Authorize
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Introduce el token JWT en el formato: Bearer {token}"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddHostedService<UserCleanupService>();

var app = builder.Build();

app.UseRouting();

// CORS primero
app.UseCors("AllowAll");

// Autenticación y autorización
app.UseAuthentication();
app.UseAuthorization();

// Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cryptiq API v1");
    c.RoutePrefix = "swagger";
});

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();

