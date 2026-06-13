using Microsoft.EntityFrameworkCore;
using NetGuardGT.Api.BackgroundServices;
using NetGuardGT.Api.Data;
using NetGuardGT.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseInMemoryDatabase("NetGuardGT"));

builder.Services.AddScoped<IncidentService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddHostedService<EscalationBackgroundService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Ensure DB is seeded
using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();
