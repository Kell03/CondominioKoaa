using Condominio.Domain.DB;
using Condominio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Condominio.Infrastructure.Repositories
{
    public class CuotaEspecialRepository : GenericRepository<CuotaEspecial>
    {

        public CuotaEspecialRepository(AppDbContext context) : base(context) { }

        public override async Task<IEnumerable<CuotaEspecial>> GetAllAsync()
        {
            return await _dbSet.Include(x => x.CuotaEspecialCasas).OrderByDescending(x => x.CreatedAt).ToListAsync();
        }

        public async Task DeleteWithHijos(CuotaEspecial entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Buscar la cuota especial con sus asignaciones a casas
                var cuota = await _context.CuotaEspecial
                    .Include(c => c.CuotaEspecialCasas)
                    .FirstOrDefaultAsync(c => c.Id == entity.Id);

                if (cuota == null)
                    throw new Exception("Cuota especial no encontrada");

                // 2. Eliminar TODAS las asignaciones a casas (CuotaEspecialCasa)
                if (cuota.CuotaEspecialCasas != null && cuota.CuotaEspecialCasas.Any())
                {
                    _context.CuotaEspecialCasa.RemoveRange(cuota.CuotaEspecialCasas);
                }

                // 3. Eliminar la cuota especial
                _context.CuotaEspecial.Remove(cuota);

                // 4. Guardar cambios
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($" Error al eliminar cuota especial: {ex.Message}");
                throw;
            }
        }


        public async Task DistribuirCuotaEspecialEntreCasas(CuotaEspecial entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Buscar la cuota especial
                var cuota = await _context.CuotaEspecial
                    .FirstOrDefaultAsync(c => c.Id == entity.Id);

                if (cuota == null)
                    throw new Exception("Cuota especial no encontrada");

                if (cuota.MontoTotal == 0)
                    throw new Exception("El monto total de la cuota es 0");

                // 2. Obtener SOLO IDs de casas activas
                var casasIds = await _context.Houses
                    .Where(h => h.IsActive)
                    .Select(h => h.Id)
                    .ToListAsync();

                int totalCasas = casasIds.Count;

                if (totalCasas == 0)
                    throw new Exception("No hay casas activas");

                // 3. ✅ TODOS PAGAN EL MISMO MONTO (SIN CONVERSIONES INNECESARIAS)
                decimal montoPorCasa = decimal.Round((decimal)cuota.MontoTotal / totalCasas, 2);

                // 4. CREAR REGISTROS (TODOS CON EL MISMO MONTO)
                var cuotasCasas = new List<CuotaEspecialCasa>(totalCasas);

                foreach (var houseId in casasIds)
                {
                    cuotasCasas.Add(new CuotaEspecialCasa
                    {
                        CuotaEspecialId = cuota.Id,
                        CuotaEspecial = cuota,
                        HouseId = houseId,
                        MontoTotal = montoPorCasa,
                        MontoPagado = 0,
                        SaldoPendiente = montoPorCasa,
                        Estado = "Pendiente",
                    });
                }

                // 5. INSERTAR TODOS DE UNA VEZ (UNA SOLA LLAMADA A BD)
                await _context.CuotaEspecialCasa.AddRangeAsync(cuotasCasas);

                // 6. Actualizar estado de la cuota
                cuota.Enviado = true;
                cuota.Motivo = entity.Motivo; // Actualizar el motivo si es necesario
                _context.CuotaEspecial.Update(cuota);

                // 7. GUARDAR TODOS LOS CAMBIOS (UNA SOLA VEZ)
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

             }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($" Error al distribuir cuota especial: {ex.Message}");
                throw;
            }
        }

    }
}
