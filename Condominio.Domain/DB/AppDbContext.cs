using Condominio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Condominio.Domain.DB
{
    public class AppDbContext: DbContext
    {

        public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
        {
        }

        // DbSets = tablas de tu base de datos
        public DbSet<Users> Users { get; set; }
        public DbSet<Houses> Houses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ✅ RELACIÓN 1:1 (SIN [ForeignKey])
            modelBuilder.Entity<Users>(entity =>
            {
                // Un usuario tiene UNA casa
                entity.HasOne(u => u.House)
                      .WithOne()                           // Una casa tiene UN usuario
                      .HasForeignKey<Users>(u => u.HouseId) // ← Aquí se define la clave foránea
                      .OnDelete(DeleteBehavior.SetNull);    // ON DELETE SET NULL
            });
        }

    }
}
