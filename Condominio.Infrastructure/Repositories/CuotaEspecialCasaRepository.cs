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
    public class CuotaEspecialCasaRepository : GenericRepository<CuotaEspecialCasa>
    {

        public CuotaEspecialCasaRepository(AppDbContext context) : base(context) { }


        public override async Task<IEnumerable<CuotaEspecialCasa>> GetAllAsync()
        {
            var query = from item in _dbSet
                        join user in _context.Users on item.HouseId equals user.HouseId into ownerGroup
                        from owner in ownerGroup.DefaultIfEmpty()
                        select new CuotaEspecialCasa
                        {
                            Id = item.Id,
                            CuotaEspecialId = item.CuotaEspecialId,
                            HouseId = item.HouseId,
                            Monto = item.Monto,
                            Estado = item.Estado,
                            Referencia = item.Referencia,
                            MetodoPago = item.MetodoPago,
                            FechaPago = item.FechaPago,
                            Comentario = item.Comentario,
                            CreatedAt = item.CreatedAt,
                            UpdatedAt = item.UpdatedAt,
                            House = item.House,
                            CuotaEspecial = item.CuotaEspecial,
                            User = owner  // ✅ ASIGNACIÓN DIRECTA
                        };

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }


        public async Task<IEnumerable<CuotaEspecialCasa>> GetAllForUserAsync(int id)
        {
            var idCasa = await _context.Users.Where(x => x.Id == id).Select(x => x.HouseId).FirstOrDefaultAsync();
            return await _dbSet.Include(x => x.House).Where(x => x.House.Id == idCasa).OrderByDescending(x => x.CreatedAt).ToListAsync();
        }


        public async Task<bool> ConfirmarPagoCuotaCasa(CuotaEspecialCasa item)
        {
            // ✅ INICIAR TRANSACCIÓN
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {

                // 1. Buscar la factura principal
                var cuota = await _context.CuotaEspecial
                    .FirstOrDefaultAsync(x => x.Id == item.CuotaEspecialId);

                if (cuota == null)
                    throw new Exception("Cuota Especial no encontrada");

                // 2. ✅ SUMAR el monto recaudado (NO asignar)
                cuota.MontoRecaudado += item.Monto;

                item.Estado = "Confirmada";

                _context.CuotaEspecialCasa.Update(item);

                _context.CuotaEspecial.Update(cuota);



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
