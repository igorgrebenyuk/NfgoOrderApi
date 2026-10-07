using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using NfgoOrderApi.Common;
using NfgoOrderApi.Data;
using NfgoOrderApi.DTOs;
using NfgoOrderApi.Models;
using NfgoOrderApi.Repositories;
using NfgoOrderApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=nfgo.db"));

// Репозитории
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IAssignmentRepository, AssignmentRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Сервисы
builder.Services.AddScoped<ICrudService<EmployeeDto>, EmployeeService>();
builder.Services.AddScoped<ICrudService<UnitDto>, UnitService>();
builder.Services.AddScoped<ICrudService<SizTypeDto>, SizTypeService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IAssignmentService, AssignmentService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddSingleton<IOrderDocumentBuilder, OrderDocumentBuilder>();

var app = builder.Build();

// База создаётся автоматически при первом запуске
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    if (app.Configuration.GetValue<bool>("SeedDemoData"))
        DemoDataSeeder.Seed(db);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

app.Run();
