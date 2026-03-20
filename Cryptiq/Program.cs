using CryptiqChat.Data;
using CryptiqChat.Hubs;
using CryptiqChat.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ?? CORS ????????????????????????????????????????????????????????????
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy => policy
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
        .SetIsOriginAllowed(_ => true));
});

// ?? JWT (un solo bloque, fusionado) ?????????????????????????????????
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

        // ? SignalR necesita leer el token desde query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs/chat"))
                    context.Token = token;
                return Task.CompletedTask;
            }
        };
    });

// ?? Redis ????????????????????????????????????????????????????????????
var multiplexer = ConnectionMultiplexer.Connect(new ConfigurationOptions
{
    EndPoints = { { "redis-18091.c278.us-east-1-4.ec2.cloud.redislabs.com", 18091 } },
    User = "default",
    Password = builder.Configuration["Redis:Password"]
});

builder.Services.AddSingleton<IConnectionMultiplexer>(multiplexer);
builder.Services.AddSingleton<PresenceService>();

// ?? Twilio ???????????????????????????????????????????????????????????
builder.Configuration["Twilio:AccountSid"] = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID");
builder.Configuration["Twilio:AuthToken"] = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN");
builder.Configuration["Twilio:FromPhone"] = Environment.GetEnvironmentVariable("TWILIO_FROM_PHONE");

// ?? Servicios ????????????????????????????????????????????????????????
builder.Services.AddAuthorization();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<SmsService>();

// ?? Base de datos ????????????????????????????????????????????????????
builder.Services.AddDbContext<CryptiqDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("CryptiqDB"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null
        )
    ));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        );
    });

// ?? SignalR y controladores ??????????????????????????????????????????
builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Cryptiq API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Formato: Bearer {token}"
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

// ?? Rate limiting ????????????????????????????????????????????????????
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("chat", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            // Clave por usuario autenticado, o por IP si no hay auth
            partitionKey: httpContext.User?.FindFirst("sub")?.Value
                       ?? httpContext.Connection.RemoteIpAddress?.ToString()
                       ?? "anonymous",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 30,   // máx 30 mensajes
                Window = TimeSpan.FromSeconds(60), // por minuto
                SegmentsPerWindow = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // Respuesta cuando se excede el límite
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsync("Demasiados mensajes. Espera un momento.", token);
    };
});
// ?? Pipeline ?????????????????????????????????????????????????????????
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CryptiqDbContext>();
    try
    {
        await db.Database.CanConnectAsync();
        Console.WriteLine("? Conectado a somee.com correctamente");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"? Error conectando a BD: {ex.Message}");
        throw; // detiene el arranque si no hay BD
    }
}
app.UseRouting();
app.UseCors("AllowAll");         // CORS siempre antes de Auth
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cryptiq API v1");
    c.RoutePrefix = "swagger";
});

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat")
    .RequireRateLimiting("chat");

app.Run();