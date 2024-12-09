using Asp.Versioning;
using HackerNewsWrapper.Story.API.Handlers;
using HackerNewsWrapper.Story.API.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

IConfiguration configuration = builder.Configuration;

builder.Services.AddControllers();

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ExceptionToProblemDetailsHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Include XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Story API - V1", Version = "v1.0" });
});

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration["CacheSettings:ConnectionString"];
});

builder.Services.AddTransient<IBestStoriesService, BestStoriesService>();
builder.Services.AddTransient<ICacheService, CacheService>();

builder.Services.AddHttpClient<IHackerNewsService, HackerNewsService>((serviceProvider, httpClient) =>
{
    httpClient.BaseAddress = new Uri("https://hacker-news.firebaseio.com/v0/");
});

var app = builder.Build();

app.UseStatusCodePages();
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
