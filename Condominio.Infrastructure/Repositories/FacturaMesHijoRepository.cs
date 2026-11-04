using Condominio.Domain.DB;
using Condominio.Domain.Entities;
using Condominio.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Condominio.Infrastructure.Repositories
{
    public class FacturaMesHijoRepository : GenericRepository<FacturaMesHijo>
    {
        public FacturaMesHijoRepository(IDbContextFactory<AppDbContext> factory) : base(factory) { }

        public override async Task<IEnumerable<FacturaMesHijo>> GetAllAsync()
        {
            await using var db = await _factory.CreateDbContextAsync();

            return await db.Set<FacturaMesHijo>()
                .Include(x => x.facturaMes)
                .Include(x => x.CategoriaGasto)
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<FacturaMesHijo>> GetAllFByFacturaAsync(int id)
        {
            await using var db = await _factory.CreateDbContextAsync();

            return await db.FacturaMesHijo
                .Where(x => x.FacturaMesId == id)
                .Include(x => x.CategoriaGasto)
                .AsNoTracking()
                .ToListAsync();
        }

        public override async Task AddAsync(FacturaMesHijo entity)
        {
            await using var db = await _factory.CreateDbContextAsync();
            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                // 1. Agregar el detalle (hijo)
                db.Set<FacturaMesHijo>().Add(entity);

                // 2. Buscar la factura padre
                var factura = await db.Set<FacturaMes>()
                    .FirstOrDefaultAsync(f => f.Id == entity.FacturaMesId);

                if (factura == null)
                    throw new InvalidOperationException($"Factura con Id {entity.FacturaMesId} no encontrada");

                // 3. Actualizar el total de la factura padre
                factura.MontoTotal += entity.Monto;

                // 4. Guardar TODOS los cambios (el hijo + la actualización del padre)
                await db.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}