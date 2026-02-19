using Microsoft.EntityFrameworkCore;

namespace Movement.WebApp.Models
{
    public class MovementEntities : DbContext
    {
        public MovementEntities(DbContextOptions<MovementEntities> options) : base(options)
        {
        }

        public DbSet<DataEntity> DataEntries { get; set; }
    }
}
