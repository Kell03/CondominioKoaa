using Condominio.Domain.DB;
using Condominio.Domain.Entities;
using Condominio.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Condominio.Infrastructure.Repositories
{
    public class GastosRepository : GenericRepository<Gasto>
    {
        private readonly MovimientosRepository _movimientoRepo;

        public GastosRepository(
            IDbContextFactory<AppDbContext> factory,
            MovimientosRepository movimientoRepo) : base(factory)
        {
            _movimientoRepo = movimientoRepo;
        }

        public override async Task<IEnumerable<Gasto>> GetAllAsync()
        {
            await using var db = await _factory.CreateDbContextAsync();

            return await db.Set<Gasto>()
                .Include(x => x.FacturaMesHijo).ThenInclude(x => x.CategoriaGasto)
                .Include(x => x.CuotaEspecial)
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public override async Task AddAsync(Gasto entity)
        {
            await using var db = await _factory.CreateDbContextAsync();
            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                if (entity.CuotaEspecialId > 0)
                {
                    var cuota = await db.CuotaEspecial.FirstOrDefaultAsync(x => x.Id == entity.CuotaEspecialId)
                        ?? throw new Exception("Cuota Especial no encontrada");
                    cuota.Pagado = true;
                    db.CuotaEspecial.Update(cuota);
                }
                else if (entity.FacturaMesHijoId > 0)
                {
                    var factura = await db.FacturaMesHijo.FirstOrDefaultAsync(x => x.Id == entity.FacturaMesHijoId)
                        ?? throw new Exception("Factura hijo no encontrada");
                    factura.Pagado = true;
                    db.FacturaMesHijo.Update(factura);

                    var detallesFactura = await db.FacturaMesHijo
                        .Where(x => x.FacturaMesId == factura.FacturaMesId)
                        .ToListAsync();

                    if (!detallesFactura.Any(x => x.Pagado == false))
                    {
                        var recibo = await db.FacturaMes.FirstOrDefaultAsync(x => x.Id == factura.FacturaMesId)
                            ?? throw new Exception("Factura padre no encontrada");
                        recibo.EstadoPago = EstadoPagoFactura.Pagada;
                        db.FacturaMes.Update(recibo);
                    }
                }

                db.Set<Gasto>().Add(entity);

                await db.SaveChangesAsync(); // gasto obtiene Id

                await _movimientoRepo.SaveMovimiento(
                    db,                         // ← ¡CLAVE! Pasar el mismo contexto
                    monto: entity.Monto,
                    montoBs: entity.MontoBs,
                    tasa: entity.Tasa,
                    gasto: entity
                );

                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"❌ Error en AddAsync: {ex.Message}");
                throw;
            }
        }
    }
}