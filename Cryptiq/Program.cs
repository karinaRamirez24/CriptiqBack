using CryptiqChat.Data;
using CryptiqChat.Hubs;
using CryptiqChat.Services;
using Microsoft.EntityFrameworkCore;

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

// DbContext con SQL Server
builder.Services.AddDbContext<CryptiqDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CryptiqDB")));

// Registrar servicios
builder.Services.AddScoped<ChatService>();

// SignalR
builder.Services.AddSignalR();

// Controladores REST
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<UserCleanupService>();

var app = builder.Build();

// Swagger siempre habilitado
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cryptiq API v1");
    c.RoutePrefix = "swagger";
});

// Activar CORS
app.UseCors("AllowAll");

app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();
