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
            // 1. Obtener facturas
            var facturas = await _dbSet
                .Include(x => x.House)
                .Include(x => x.FacturaMes)
                .ThenInclude(x => x.FacturaMesHijos)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();  // ✅ 1 consulta

            // 2. Obtener HouseIds únicos
            var houseIds = facturas
                .Where(f => f.HouseId > 0)
                .Select(f => f.HouseId)
                .Distinct()
                .ToList();

            // 3. Obtener dueños (si hay casas)
            if (houseIds.Any())
            {
                var owners = await _context.Users
                    .Where(u => u.HouseId.HasValue && houseIds.Contains(u.HouseId.Value))
                    .ToDictionaryAsync(u => u.HouseId.Value, u => u);

                // 4. Asignar dueños
                foreach (var factura in facturas)
                {
                    if (owners.TryGetValue(factura.HouseId, out var owner))
                    {
                        factura.User = owner;
                    }
                }
            }

            // ✅ RETORNAR LA LISTA (SIN ToListAsync)
            return facturas;
        }


        public async  Task<IEnumerable<FacturaMesCasa>> GetAllForUserAsync(int id)
        {
            var idCasa = await _context.Users.Where(x => x.Id == id).Select(x => x.HouseId).FirstOrDefaultAsync();
            return await _dbSet.Include(x => x.House).Include(x => x.FacturaMes).ThenInclude(x => x.FacturaMesHijos).Where(x => x.House.Id == idCasa).OrderByDescending(x => x.CreatedAt).ToListAsync();
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
