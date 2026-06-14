using Microsoft.EntityFrameworkCore;
using NetGuardGT.Api.Models;

namespace NetGuardGT.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<Technician> Technicians => Set<Technician>();
    public DbSet<IncidentHistory> IncidentHistories => Set<IncidentHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Incident>()
            .HasOne(i => i.Technician)
            .WithMany(t => t.Incidents)
            .HasForeignKey(i => i.TechnicianId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<IncidentHistory>()
            .HasOne(h => h.Incident)
            .WithMany(i => i.History)
            .HasForeignKey(h => h.IncidentId);

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Technician>().HasData(
            new Technician { Id = 1, Name = "Carlos Mendez",   Specialization = Specialization.FiberOptic },
            new Technician { Id = 2, Name = "Ana Lopez",       Specialization = Specialization.FiberOptic },
            new Technician { Id = 3, Name = "Luis Perez",      Specialization = Specialization.Microwave },
            new Technician { Id = 4, Name = "Maria Garcia",    Specialization = Specialization.Microwave },
            new Technician { Id = 5, Name = "Roberto Diaz",    Specialization = Specialization.Electrical },
            new Technician { Id = 6, Name = "Sofia Castro",    Specialization = Specialization.Electrical },
            new Technician { Id = 7, Name = "Diego Morales",   Specialization = Specialization.Network },
            new Technician { Id = 8, Name = "Laura Torres",    Specialization = Specialization.Network },
            new Technician { Id = 9, Name = "Pedro Jimenez",   Specialization = Specialization.General },
            new Technician { Id = 10, Name = "Elena Ruiz",     Specialization = Specialization.General },
            new Technician { Id = 11, Name = "Marco Salazar",  Specialization = Specialization.FiberOptic },
            new Technician { Id = 12, Name = "Claudia Reyes",  Specialization = Specialization.Microwave }
        );
    }
}
