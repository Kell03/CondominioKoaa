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
        public DbSet<FacturaMes> FacturaMes { get; set; }
        public DbSet<FacturaMesHijo> FacturaMesHijo { get; set; }
        public DbSet<FacturaMesCasa> FacturaMesCasa { get; set; }
        public DbSet<CuotaEspecialCasa> CuotaEspecialCasa { get; set; }
        public DbSet<CuotaEspecial> CuotaEspecial { get; set; }
        public DbSet<Payments> Payments { get; set; }


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


            modelBuilder.Entity<FacturaMes>(entity =>
            {

                entity.HasMany(fm => fm.FacturaMesHijos)  // Un FacturaMes tiene muchos hijos
                      .WithOne(fmh => fmh.facturaMes)     // Un hijo pertenece a un FacturaMes
                      .HasForeignKey(fmh => fmh.FacturaMesId)  // Clave foránea
                      .OnDelete(DeleteBehavior.Cascade);  // Al eliminar FacturaMes, se eliminan los hijos
            });

            modelBuilder.Entity<FacturaMesHijo>(entity =>
            {

                entity.HasOne(fmh => fmh.facturaMes)
                      .WithMany(fm => fm.FacturaMesHijos)
                      .HasForeignKey(fmh => fmh.FacturaMesId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<FacturaMesCasa>(entity =>
            {
                
                // ✅ RELACIÓN CON FacturaMes (N → 1)
                entity.HasOne(fc => fc.FacturaMes)
                    .WithMany()
                    .HasForeignKey(fc => fc.FacturaMesId)
                    .OnDelete(DeleteBehavior.Cascade);

                // ✅ RELACIÓN CON Houses (N → 1)
                entity.HasOne(fc => fc.House)
                    .WithMany()
                    .HasForeignKey(fc => fc.HouseId)
                    .OnDelete(DeleteBehavior.Cascade);

            });


            modelBuilder.Entity<CuotaEspecial>(entity =>
            {

                // ✅ RELACIÓN CON CUOTAESPECIALCASA (1 → N)
                entity.HasMany(ce => ce.CuotaEspecialCasas)
                    .WithOne(cec => cec.CuotaEspecial)
                    .HasForeignKey(cec => cec.CuotaEspecialId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CuotaEspecialCasa>(entity =>
            {
                
                // ✅ RELACIÓN CON CUOTAESPECIAL (N → 1)
                entity.HasOne(cec => cec.CuotaEspecial)
                    .WithMany(ce => ce.CuotaEspecialCasas)
                    .HasForeignKey(cec => cec.CuotaEspecialId)
                    .OnDelete(DeleteBehavior.Cascade);

                // ✅ RELACIÓN CON HOUSES (N → 1)
                entity.HasOne(cec => cec.House)
                    .WithMany()
                    .HasForeignKey(cec => cec.HouseId)
                    .OnDelete(DeleteBehavior.Cascade);
            });


            modelBuilder.Entity<Payments>(entity =>
            {

                // ✅ RELACIÓN CON FACTURA
                entity.HasOne(p => p.FacturaMesCasa)
                    .WithMany(fc => fc.Payments)
                    .HasForeignKey(p => p.FacturaMesCasaId)
                    .OnDelete(DeleteBehavior.Cascade);

                // ✅ RELACIÓN CON CUOTA ESPECIAL
                entity.HasOne(p => p.CuotaEspecialCasa)
                    .WithMany(cec => cec.Payments)
                    .HasForeignKey(p => p.CuotaEspecialCasaId)
                    .OnDelete(DeleteBehavior.Cascade);

               
            });
        }

    }
}
