using Microsoft.EntityFrameworkCore;
using UMNPhotographers.Distribution.Domain.Entities;

namespace UMNPhotographers.Distribution.Domain;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> dbContextOptions) : base(dbContextOptions) {}
    
    public DbSet<Activity> Activities { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Photographer> Photographers { get; set; }
    public DbSet<PhotographerFreetime> PhotographerFreetimes { get; set; }
    public DbSet<PhotographerSchedule> PhotographerSchedules { get; set; }
    public DbSet<PhotographerZoneInfo> PhotographerZoneInfos { get; set; }
    public DbSet<SchedulePart> ScheduleParts { get; set; }
    public DbSet<Zone> Zones { get; set; }
    public DbSet<AllocationEvent> AllocationEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<long>("Id");
        
        modelBuilder.Entity<SchedulePart>()
            .Property(o => o.Id)
            .ValueGeneratedOnAdd();
        
        modelBuilder.Entity<AllocationEvent>()
            .Property(o => o.Id)
            .ValueGeneratedOnAdd();
        
        base.OnModelCreating(modelBuilder);
    }
}