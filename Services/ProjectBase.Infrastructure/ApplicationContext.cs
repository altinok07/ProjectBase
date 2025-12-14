using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ProjectBase.Core.Data.Contexts;
using ProjectBase.Domain.Entities.Users;

namespace ProjectBase.Infrastructure;

public class ApplicationContext(DbContextOptions options, IHttpContextAccessor httpContextAccessor) : BaseContext(options, httpContextAccessor)
{
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Configurations();

        base.OnModelCreating(modelBuilder);
    }
}
