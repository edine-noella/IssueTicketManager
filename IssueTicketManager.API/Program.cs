using System.Text.Json.Serialization;
using Azure.Messaging.ServiceBus;
using IssueTicketManager.API.Configuration;
using IssueTicketManager.API.Data;
using IssueTicketManager.API.Repositories;
using IssueTicketManager.API.Repositories.Interfaces;
using IssueTicketManager.API.Services;
using IssueTicketManager.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


// Configure Service Bus
builder.Services.Configure<ServiceBusConfiguration>(
    builder.Configuration.GetSection(ServiceBusConfiguration.SectionName));

// Register Service Bus Client
builder.Services.AddSingleton<ServiceBusClient>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("ServiceBus") 
                           ?? configuration.GetValue<string>("ServiceBus:ConnectionString");
    
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("Service Bus connection string is not configured");
    }
    
    return new ServiceBusClient(connectionString);
});

// Register Service Bus Service
builder.Services.AddScoped<IServiceBusService, ServiceBusService>();



builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IIssueRepository, IssueRepository>();
builder.Services.AddScoped<ILabelRepository, LabelRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();


// Add Swagger for API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.MapControllers();

app.UseHttpsRedirection();



app.Run();