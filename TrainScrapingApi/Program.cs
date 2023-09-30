using TrainScrapingApi.Extensions.DependencyInjection;
using TrainScrapingApi.Extensions.Middlewares;
using TrainScrapingApi.Middlewares;
using TrainScrapingApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddSingleton<IAppConfigService, AppEnvConfigService>();
builder.Services.AddSqlServices();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(option =>
{
    option.AddPolicy("openrailwaymap", builder =>
    {
        builder
            .WithOrigins("https://www.openrailwaymap.org")
            .WithHeaders("authorization")
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseGlobalErrorHandler();
app.UseHttpsRedirection();
app.UseCors("openrailwaymap");
app.UseMiddleware<BasicAuthorizationMiddleware>();
app.MapControllers();

app.Run();
