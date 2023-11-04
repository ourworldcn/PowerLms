using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLmsServer.EfData
{
    public static class MigrateDbInitializer
    {
        public static void Initialize(PowerLmsUserDbContext context)
        {
            if (context.Database.GetPendingMigrations().Any())
            {
                context.Database.Migrate();
            }

        }
    }

    public class PowerLmsUserDbContext : DbContext
    {
        /// <summary>
        /// 
        /// </summary>
        public PowerLmsUserDbContext()
        {
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        public PowerLmsUserDbContext(DbContextOptions options) : base(options)
        {
            //Database.SetInitializer<Context>(new MigrateDatabaseToLatestVersion<Context, ReportingDbMigrationsConfiguration>());
        }

        public DbSet<Multilingual> Multilinguals { get; set; }

    }
}
