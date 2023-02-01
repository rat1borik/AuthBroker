using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using Bogus;
using System.Reflection.Metadata;

namespace AuthBroker.Model;

public class Vehicle {
    public Guid Id { get; set; }
    public string VehicleIdentificationNumber { get; set; } = "";
    public string Model { get; set; } = "";
    public string Type { get; set; } = "";
    public string Fuel { get; set; } = "";
}

public class TestContext : DbContext {
    public DbSet<Vehicle> Vehicles { get; set; }

    public TestContext(DbContextOptions<TestContext> options)
        : base(options) {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    => optionsBuilder
        .UseSnakeCaseNamingConvention();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.HasPostgresExtension("uuid-ossp");
        modelBuilder.Entity<Vehicle>().HasIndex(i => i.VehicleIdentificationNumber).IsUnique();

    }

    //public static async Task InitializeAsync(TestContext db) {

    //    await db.Database.MigrateAsync();

    //    // already seeded
    //    if (db.Vehicles.Any())
    //        return;

    //    // sample data will be different due
    //    // to the nature of generating data
    //    var fake = new Faker<Vehicle>()
    //        .Rules((f, v) => v.VehicleIdentificationNumber = f.Vehicle.Vin())
    //        .Rules((f, v) => v.Model = f.Vehicle.Model())
    //        .Rules((f, v) => v.Type = f.Vehicle.Type())
    //        .Rules((f, v) => v.Fuel = f.Vehicle.Fuel());
    //    var vehicles = fake.Generate(100);

    //    db.Vehicles.AddRange(vehicles);
    //    await db.SaveChangesAsync();
    //}
}
public class VehicleStore {
    private TestContext cx;

    public VehicleStore(TestContext _cx) {
        cx = _cx;
    }

    public async Task<List<Vehicle>> GetListAsync() {
        return await cx.Vehicles.ToListAsync();
    }

    public async Task InitAsync() {

        // already seeded
        if (cx.Vehicles.Any())
            await cx.Vehicles.ExecuteDeleteAsync();

        // sample data will be different due
        // to the nature of generating data
        var faker = new Faker<Vehicle>()
            .Rules((f, v) => v.VehicleIdentificationNumber = f.Vehicle.Vin())
            .Rules((f, v) => v.Model = f.Vehicle.Model())
            .Rules((f, v) => v.Type = f.Vehicle.Type())
            .Rules((f, v) => v.Fuel = f.Vehicle.Fuel());
        var vehicles = faker.Generate(100);

        await cx.Vehicles.AddRangeAsync(vehicles);
        await cx.SaveChangesAsync();
    }
}