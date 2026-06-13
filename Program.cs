using Microsoft.EntityFrameworkCore;
using NetGuardGT.Api.BackgroundServices;
using NetGuardGT.Api.Data;
using NetGuardGT.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// SQLite — archivo en /data/netguardgt.db dentro del contenedor (volume mount en Render)
var dbPath = Path.Combine("/data", "netguardgt.db");
if (builder.Environment.IsDevelopment())
    dbPath = Path.Combine(Directory.GetCurrentDirectory(), "netguardgt.db");

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddScoped<IncidentService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddHostedService<EscalationBackgroundService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Aplicar migraciones y seed automáticamente al iniciar
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();
