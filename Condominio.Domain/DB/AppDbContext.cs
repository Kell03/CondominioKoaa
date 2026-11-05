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
        public DbSet<ConfiguracionCondominio> ConfiguracionCondominio { get; set; }
        public DbSet<FacturaMes> FacturaMes { get; set; }
        public DbSet<FacturaMesHijo> FacturaMesHijo { get; set; }
        public DbSet<FacturaMesCasa> FacturaMesCasa { get; set; }
        public DbSet<CuotaEspecialCasa> CuotaEspecialCasa { get; set; }
        public DbSet<CuotaEspecial> CuotaEspecial { get; set; }
        public DbSet<Payments> Payments { get; set; }
        public DbSet<ConfiguracionMoneda> ConfiguracionMoneda { get; set; }
        public DbSet<Notificaciones> Notificaciones { get; set; }
        public DbSet<ImagenesFacturas> ImagenesFacturas { get; set; }
        public DbSet<Movimientos> Movimientos { get; set; }
        public DbSet<CategoriasGasto> CategoriasGasto { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Gasto> Gastos { get; set; }


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

                entity.Property(fm => fm.EstadoPago)
                      .HasConversion<string>()
                      .HasMaxLength(50);
            });

            modelBuilder.Entity<FacturaMesHijo>(entity =>
            {

                entity.HasOne(fmh => fmh.facturaMes)
                      .WithMany(fm => fm.FacturaMesHijos)
                      .HasForeignKey(fmh => fmh.FacturaMesId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(fh => fh.CategoriaGasto)
                      .WithMany()
                      .HasForeignKey(fh => fh.IdCategoria)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(fh => fh.Proveedor)
                      .WithMany()
                      .HasForeignKey(fh => fh.IdProveedor)
                      .OnDelete(DeleteBehavior.Restrict);
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

                entity.HasOne(ce => ce.Proveedor)
                      .WithMany()
                      .HasForeignKey(ce => ce.IdProveedor)
                      .OnDelete(DeleteBehavior.Restrict);
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

            modelBuilder.Entity<Notificaciones>(entity =>
            {
                // ... configuración existente

                // ✅ RELACIÓN CON USERS
                entity.HasOne(n => n.User)
                    .WithMany()
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });


            modelBuilder.Entity<Movimientos>(entity =>
            {


                entity.HasOne(m => m.Factura)
                      .WithMany()
                      .HasForeignKey(m => m.IdFactura)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(m => m.CuotaEspecial)
                      .WithMany()
                      .HasForeignKey(m => m.IdCuotaEspecial)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(m => m.Gasto)
                      .WithMany()
                      .HasForeignKey(m => m.IdGasto)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(m => m.House)
         .WithMany()
         .HasForeignKey(m => m.IdCasa)        // ← USA IdCasa, no HouseId
         .OnDelete(DeleteBehavior.Restrict);
            });


            modelBuilder.Entity<CategoriasGasto>(entity =>
            {
                entity.HasOne(c => c.Proveedor)
                      .WithMany()
                      .HasForeignKey(c => c.IdProveedor)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Gasto>(entity =>
            {
                // ✅ RELACIÓN CON FacturaMesHijo (N → 1)
                entity.HasOne(g => g.FacturaMesHijo)
                      .WithMany()
                      .HasForeignKey(g => g.FacturaMesHijoId)
                      .OnDelete(DeleteBehavior.Restrict);

                // ✅ RELACIÓN CON CuotaEspecial (N → 1)
                entity.HasOne(g => g.CuotaEspecial)
                      .WithMany()
                      .HasForeignKey(g => g.CuotaEspecialId)
                      .OnDelete(DeleteBehavior.Restrict);
            });



            // Usuarios: login por email (único) y filtro por casa
            modelBuilder.Entity<Users>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.HouseId);
            });

            // Facturas: orden por año/mes y filtro por fecha
            modelBuilder.Entity<FacturaMes>(entity =>
            {
                entity.HasIndex(f => new { f.Year, f.Mes });
                entity.HasIndex(f => f.CreatedAt);
            });

            // FacturaMesCasa: los dos filtros más usados + compuesto por casa/estado
            modelBuilder.Entity<FacturaMesCasa>(entity =>
            {
                entity.HasIndex(f => f.HouseId);
                entity.HasIndex(f => f.FacturaMesId);
                entity.HasIndex(f => new { f.HouseId, f.Estado });
            });

            // Cuotas especiales: por casa, por cuota, y compuesto casa/estado
            modelBuilder.Entity<CuotaEspecialCasa>(entity =>
            {
                entity.HasIndex(c => c.HouseId);
                entity.HasIndex(c => c.CuotaEspecialId);
                entity.HasIndex(c => new { c.HouseId, c.Estado });
            });

            // Payments: por factura, por cuota, y compuesto estado/fecha
            modelBuilder.Entity<Payments>(entity =>
            {
                entity.HasIndex(p => p.FacturaMesCasaId);
                entity.HasIndex(p => p.CuotaEspecialCasaId);
                entity.HasIndex(p => new { p.Estado, p.FechaPago });
            });

            // Movimientos: por fecha y por gasto
            modelBuilder.Entity<Movimientos>(entity =>
            {
                entity.HasIndex(m => m.CreatedAt);
                entity.HasIndex(m => m.IdGasto);
            });

            // Notificaciones: no leídas por usuario (el caso más común) + orden por fecha
            modelBuilder.Entity<Notificaciones>(entity =>
            {
                entity.HasIndex(n => new { n.UserId, n.IsRead });
                entity.HasIndex(n => n.FechaCreacion);
            });

        }

    }
}
