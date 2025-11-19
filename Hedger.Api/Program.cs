using Hedger.Core.Interfaces;
using Hedger.Core.Repositories.Interfaces;
using Hedger.Core.Repositories.RepositoryImpl;
using Hedger.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



// Register Core services
builder.Services.AddSingleton<IExchangeService, ExchangeService>();

// Register repository: use order_books_data from a Data folder in the API project
builder.Services.AddSingleton<IExchangeRepository>(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();

    var filePath = Path.Combine(env.ContentRootPath, @"order_books_data");
    // make sure this file exists and is set to "Copy to Output Directory"

    return new ExchangeRepository(filePath);
});

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
   app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();


app.MapControllers();

app.Run();
