using Microsoft.EntityFrameworkCore;
using Xarajat.Data.Entities;

namespace Xarajat.Data.Context
{
    public class XarajatDbContext:DbContext
    {
       public DbSet<User> Users { get; set; }
       public DbSet<Room> Rooms { get; set; }
       public DbSet<Outlay> Outlays { get; set; }

       public XarajatDbContext(DbContextOptions options) : base(options)
       {

       }

       protected override void OnModelCreating(ModelBuilder modelBuilder)
       {
           base.OnModelCreating(modelBuilder);

           new OutlayConfiguration().Configure(modelBuilder.Entity<Outlay>());

           modelBuilder.ApplyConfigurationsFromAssembly(typeof(XarajatDbContext).Assembly);
       }
    }
}
