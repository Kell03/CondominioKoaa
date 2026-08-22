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



        public  async Task DeleteWithHijos(FacturaMes entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Buscar la factura con sus hijos
                var factura = await _context.FacturaMes
                    .Include(f => f.FacturaMesHijos)
                    .FirstOrDefaultAsync(f => f.Id == entity.Id);

                if (factura == null)
                    throw new Exception("Factura no encontrada");

                // 2. Eliminar TODOS los hijos
                if (factura.FacturaMesHijos != null && factura.FacturaMesHijos.Any())
                {
                    _context.FacturaMesHijo.RemoveRange(factura.FacturaMesHijos);
                }

                // 3. Eliminar la factura
                _context.FacturaMes.Remove(factura);

                // 4. Guardar cambios
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($" Error al eliminar: {ex.Message}");
                throw;
            }
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
