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
                            MontoBs = factura.MontoBs,
                            Estado = factura.Estado,
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


        public async Task<IEnumerable<FacturaMesCasa>> GetAllForUserAsync(int id)
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


        public async Task<bool> RegistrarPagoFactura(Payments pago)
        {
            try
            {

                var payment = new Payments
                {
                    FacturaMesCasaId = pago.FacturaMesCasaId,
                    Monto = pago.Monto,
                    MetodoPago = pago.MetodoPago,
                    Referencia = pago.Referencia,
                    Estado = "Pendiente"
                };

                await _context.Payments.AddAsync(payment);
                await _context.SaveChangesAsync();

                // ✅ Actualizar saldo de la factura
                // ... (código anterior)

                return true;

            }
            catch (Exception ex)
            {

                // Opcional: guardar el error en un log
                Console.WriteLine($"Error al guardar: {ex.Message}");

                return false; // Falló
            }
        }

        public async Task<IEnumerable<Payments>> GetPaymentsForUserAsync(int userId)
        {
            try
            {
                // 1. Obtener la casa del usuario
                var houseId = await _context.Users
                    .Where(u => u.Id == userId)
                    .Select(u => u.HouseId)
                    .FirstOrDefaultAsync();

                if (houseId == null || houseId == 0)
                    return new List<Payments>();

                // 2. Obtener todos los pagos de esa casa
                // Un pago puede estar asociado a FacturaMesCasa o CuotaEspecialCasa
                // Ambos tienen HouseId

                var payments = await _context.Payments
                    .Include(p => p.FacturaMesCasa)
                        .ThenInclude(f => f.FacturaMes)
                    .Include(p => p.FacturaMesCasa)
                        .ThenInclude(f => f.House)
                    .Include(p => p.CuotaEspecialCasa)
                        .ThenInclude(c => c.CuotaEspecial)
                    .Include(p => p.CuotaEspecialCasa)
                        .ThenInclude(c => c.House)
                    .Where(p =>
                        (p.FacturaMesCasa != null && p.FacturaMesCasa.HouseId == houseId) ||
                        (p.CuotaEspecialCasa != null && p.CuotaEspecialCasa.HouseId == houseId)
                    )
                    .OrderByDescending(p => p.FechaPago)
                    .ToListAsync();

                return payments;

            }
            catch (Exception ex)
            {
                throw;
            }

        }

    }
}
