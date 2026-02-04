using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MEDICSYS.Api.Data;

public class AcademicDbContextFactory : IDesignTimeDbContextFactory<AcademicDbContext>
{
    public AcademicDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AcademicDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=medicsys_academico;Username=postgres;Password=030762");

        return new AcademicDbContext(optionsBuilder.Options);
    }
}
