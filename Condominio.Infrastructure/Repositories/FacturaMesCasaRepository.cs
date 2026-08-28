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
            // ✅ UNA SOLA CONSULTA CON JOIN (más eficiente)
            var query = from factura in _dbSet
                        join house in _context.Houses on factura.HouseId equals house.Id
                        join user in _context.Users on house.Id equals user.HouseId into ownerGroup
                        from owner in ownerGroup.DefaultIfEmpty()
                        select new FacturaMesCasa
                        {
                            Id = factura.Id,
                            FacturaMesId = factura.FacturaMesId,
                            HouseId = factura.HouseId,
                            MontoTotal = factura.MontoTotal,
                            Estado = factura.Estado,
                            Referencia = factura.Referencia,
                            MetodoPago = factura.MetodoPago,
                            FechaPago = factura.FechaPago,
                            Comentario = factura.Comentario,
                            CreatedAt = factura.CreatedAt,
                            UpdatedAt = factura.UpdatedAt,
                            House = house,
                            FacturaMes = factura.FacturaMes,
                            User = owner
                        };

            // ✅ Incluir hijos
            var result = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();

            // ✅ Cargar hijos por separado (1 consulta adicional)
            var facturaIds = result.Select(x => x.FacturaMesId).Distinct().ToList();
            if (facturaIds.Any())
            {
                var hijos = await _context.FacturaMesHijo
                    .Where(h => facturaIds.Contains(h.FacturaMesId))
                    .ToListAsync();

                foreach (var factura in result)
                {
                    factura.FacturaMes.FacturaMesHijos = hijos
                        .Where(h => h.FacturaMesId == factura.FacturaMesId)
                        .ToList();
                }
            }

            return result;
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
