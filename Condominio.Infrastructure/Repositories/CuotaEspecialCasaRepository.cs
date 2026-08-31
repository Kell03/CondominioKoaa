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
                            MontoTotal = item.MontoTotal,
                            MontoPagado = item.MontoPagado,
                            SaldoPendiente = item.SaldoPendiente,
                            MontoBs = item.MontoBs,
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
                cuota.MontoRecaudado += item.MontoTotal;

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


        public async Task<bool> RegistrarPagoCuota(Payments pago)
        {
            try
            {

                var payment = new Payments
                {
                    CuotaEspecialCasaId = pago.CuotaEspecialCasaId,
                    Monto = pago.Monto,
                    MontoBs = pago.MontoBs,
                    Tasa = pago.Tasa,
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



        public async Task<IEnumerable<Payments>> GetPaymentsForUserAsync(int userId, int idCuotaCasaMes)
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
                    .Include(p => p.CuotaEspecialCasa)
                        .ThenInclude(c => c.CuotaEspecial)
                    .Include(p => p.CuotaEspecialCasa)
                        .ThenInclude(c => c.House)
                    .Where(p =>
                        (p.CuotaEspecialCasa != null && p.CuotaEspecialCasa.HouseId == houseId && p.CuotaEspecialCasaId == idCuotaCasaMes)
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


        public async Task<IEnumerable<Payments>> GetPaymentsForInvoiceAsync(int idCuotaCasaMes)
        {
            try
            {

                var payments = await _context.Payments
                    .Include(p => p.CuotaEspecialCasa)
                        .ThenInclude(c => c.CuotaEspecial)
                    .Include(p => p.CuotaEspecialCasa)
                        .ThenInclude(c => c.House)
                    .Where(p =>
                         p.CuotaEspecialCasaId == idCuotaCasaMes
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


        public async Task<bool> ConfirmPayment(Payments payment)
        {
            // ✅ INICIAR TRANSACCIÓN
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {

                var cuotaCasa = await _context.CuotaEspecialCasa.FirstOrDefaultAsync(x => x.Id == payment.CuotaEspecialCasaId);

                if (cuotaCasa == null)
                    throw new Exception("Cuota no encontrada");



                var cuota = await _context.CuotaEspecial
                    .FirstOrDefaultAsync(x => x.Id == cuotaCasa.CuotaEspecialId);

                if (cuota == null)
                    throw new Exception("Cuota no encontrada");



                // 2. ✅ SUMAR el monto recaudado (NO asignar)
                cuotaCasa.MontoPagado = cuotaCasa.MontoPagado + payment.Monto;


                cuotaCasa.SaldoPendiente = cuotaCasa.MontoTotal - cuotaCasa.MontoPagado;

                cuota.MontoRecaudado += payment.Monto;

                cuotaCasa.Estado = cuotaCasa.SaldoPendiente <= 0 ? "Confirmada" : "Pendiente";

                payment.Estado = "Confirmada";


                _context.Payments.Update(payment);
                _context.CuotaEspecialCasa.Update(cuotaCasa);

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
