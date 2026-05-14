using RehabAI.Application.Chatbot;
using RehabAI.Infrastructure;
using RehabAI.Infrastructure.Database;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();
builder.Services.AddScoped<IChatbotService, ChatbotService>();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

await app.Services.SeedDatabaseAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
