using Condominio.Domain.DB;
using Condominio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Condominio.Infrastructure.Repositories
{
    public class FacturaMesCasaRepository : GenericRepository<FacturaMesCasa>
    {

        public FacturaMesCasaRepository(AppDbContext context) : base(context) { }

        public override async Task<IEnumerable<FacturaMesCasa>> GetAllAsync()
        {
            return await _dbSet.Include(x => x.House).Include(x => x.FacturaMes).OrderByDescending(x => x.CreatedAt).ToListAsync();
        }



        public async Task<bool> ConfirmarPagoFacturaCasa(FacturaMesCasa item)
        {
            // ✅ INICIAR TRANSACCIÓN
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {

                // 1. Buscar la factura principal
                var factura = await _context.FacturaMes
                    .FirstOrDefaultAsync(x => x.Id == item.FacturaMesId);

                if (factura == null)
                    throw new Exception("Factura no encontrada");

                // 2. ✅ SUMAR el monto recaudado (NO asignar)
                factura.MontoRecaudado += item.MontoTotal;

                item.Estado = "Confirmada";

                _context.FacturaMesCasa.Update(item);

                _context.FacturaMes.Update(factura);


              
                await _context.SaveChangesAsync();

                // ✅ CONFIRMAR TRANSACCIÓN
                await transaction.CommitAsync();

                return true; // Éxito
            }
            catch (Exception ex)
            {
                // ❌ REVERTIR TRANSACCIÓN (ROLLBACK)
                await transaction.RollbackAsync();

                // Opcional: guardar el error en un log
                Console.WriteLine($"Error al guardar: {ex.Message}");

                return false; // Falló
            }
        }

    }
}
