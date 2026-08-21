using Condominio.Domain.DB;
using Condominio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Condominio.Infrastructure.Repositories
{
    public class FacturaMesRepository : GenericRepository<FacturaMes>
    {

        public FacturaMesRepository(AppDbContext context) : base(context) { }

        public override async Task<IEnumerable<FacturaMes>> GetAllAsync()
        {
            return await _dbSet.Include(x => x.FacturaMesHijos).OrderByDescending(x => x.CreatedAt).ToListAsync();
        }


        public async Task<bool> SaveWithFacturaHijo(FacturaMes item, List<FacturaMesHijo> children)
        {
            // ✅ INICIAR TRANSACCIÓN
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Guardar la factura
                await _context.FacturaMes.AddAsync(item);
                await _context.SaveChangesAsync(); // Guardar para obtener el ID

                // 2. Asignar el ID de la factura a los hijos
                foreach (var child in children)
                {
                    child.FacturaMesId = item.Id;
                }

                // 3. Guardar los hijos
                await _context.FacturaMesHijo.AddRangeAsync(children);
                await _context.SaveChangesAsync();

                // 4. Actualizar el MontoTotal de la factura
                item.MontoTotal = children.Sum(c => c.Monto);
                _context.FacturaMes.Update(item);
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
