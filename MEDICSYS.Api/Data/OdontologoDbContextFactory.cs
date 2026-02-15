using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MEDICSYS.Api.Data;

public class OdontologoDbContextFactory : IDesignTimeDbContextFactory<OdontologoDbContext>
{
    public OdontologoDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OdontologoDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=medicsys_odontologia;Username=postgres;Password=030762");

        return new OdontologoDbContext(optionsBuilder.Options);
    }
}
