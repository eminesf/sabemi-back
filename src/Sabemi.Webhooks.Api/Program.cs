using Microsoft.EntityFrameworkCore;
using Sabemi.Webhooks.Service.DependencyInjection;
using Sabemi.Webhooks.Repository.DependencyInjection;
using Sabemi.Webhooks.Repository.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddServiceLayer(builder.Configuration);
builder.Services.AddRepositoryLayer(builder.Configuration);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

// Swagger habilitado também em produção (decisão proposital deste teste,
// para facilitar a avaliação do backend sem precisar rodar localmente).
app.UseSwagger();
app.UseSwaggerUI();

// garante que as tabelas existam ao subir (EnsureCreated é suficiente para o
// escopo deste teste; em um projeto maior isso seria feito via EF migrations
// aplicadas em um passo de deploy separado).
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SabemiDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

app.UseCors("Frontend");
app.UseAuthorization();
app.MapControllers();

app.Run();
