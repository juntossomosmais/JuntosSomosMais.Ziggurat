using Microsoft.EntityFrameworkCore;

namespace JuntosSomosMais.Ziggurat.SqlServer.Tests.Support;

public class TestContextWithoutMessages : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase("Test");
    }
}
