using Condominio.Domain.DB;
using Condominio.Domain.Entities;
using Condominio.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Condominio.Infrastructure.Repositories
{
    public class CuotaEspecialCasaRepository : GenericRepository<CuotaEspecialCasa>
    {


        private readonly INotificacionRealTimeService _notificacionService;
        private readonly NotificacionRepository _notifRepo;

        private readonly MovimientosRepository _movimientoRepo;   // ← NUEVO



        public CuotaEspecialCasaRepository(
     IDbContextFactory<AppDbContext> factory,
      NotificacionRepository notifRepo,
      INotificacionRealTimeService notificacionService,
      MovimientosRepository movimientoRepo) : base(factory)  // ← NUEVO
        {
            _notifRepo = notifRepo;
            _notificacionService = notificacionService;
            _movimientoRepo = movimientoRepo;   // ← NUEVO
        }
        public override async Task<IEnumerable<CuotaEspecialCasa>> GetAllAsync()
        {
            await using var db = await _factory.CreateDbContextAsync();

            var query = from item in db.Set<CuotaEspecialCasa>()
                        join user in db.Users on item.HouseId equals user.HouseId into ownerGroup
                        from owner in ownerGroup.DefaultIfEmpty()
                        join payments in db.Payments on item.Id equals payments.CuotaEspecialCasaId into paymentGroup
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
                            Payments = paymentGroup.ToList(),
                            User = owner
                        };

            return await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
        }


        public async Task<IEnumerable<CuotaEspecialCasa>> GetAllForUserAsync(int id)
        {
            await using var db = await _factory.CreateDbContextAsync();

            var idCasa = await db.Users.Where(x => x.Id == id).Select(x => x.HouseId).FirstOrDefaultAsync();

            return await db.Set<CuotaEspecialCasa>()
                .Include(x => x.House)
                .Include(x => x.CuotaEspecial)
                .Where(x => x.House.Id == idCasa)
                .OrderByDescending(x => x.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }


        public async Task<bool> ConfirmarPagoCuotaCasa(CuotaEspecialCasa item)
        {
            await using var db = await _factory.CreateDbContextAsync();
            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                var cuota = await db.CuotaEspecial.FirstOrDefaultAsync(x => x.Id == item.CuotaEspecialId);
                if (cuota == null)
                    throw new Exception("Cuota Especial no encontrada");

                cuota.MontoRecaudado += item.MontoTotal;
                item.Estado = EstadoCuotaCasa.Confirmada;

                db.CuotaEspecialCasa.Update(item);
                db.CuotaEspecial.Update(cuota);

                await db.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error al guardar: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RegistrarPagoCuota(Payments pago)
        {
            await using var db = await _factory.CreateDbContextAsync();
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

                db.Payments.Add(payment);
                await db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al guardar: {ex.Message}");
                return false;
            }
        }


        public async Task<List<Payments>> RegistrarMultiplesPagoFactura(List<Payments> pagos)
        {
            if (pagos == null || !pagos.Any()) return new List<Payments>();

            await using var db = await _factory.CreateDbContextAsync();
            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                var listaPayments = pagos.Select(item => new Payments
                {
                    CuotaEspecialCasaId = item.CuotaEspecialCasaId,
                    Monto = item.Monto,
                    MontoBs = item.MontoBs,
                    Tasa = item.Tasa,
                    MetodoPago = item.MetodoPago,
                    Referencia = item.Referencia,
                    Estado = "Pendiente"
                }).ToList();

                await db.Payments.AddRangeAsync(listaPayments);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
                return listaPayments;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"❌ Error: {ex.Message}");
                throw;
            }
        }
        public async Task<IEnumerable<Payments>> GetPaymentsForUserAsync(int userId, int idCuotaCasaMes)
        {
            await using var db = await _factory.CreateDbContextAsync();

            var houseId = await db.Users
                .Where(u => u.Id == userId)
                .Select(u => u.HouseId)
                .FirstOrDefaultAsync();

            if (houseId == null || houseId == 0)
                return new List<Payments>();

            return await db.Payments
                .Include(p => p.CuotaEspecialCasa).ThenInclude(c => c.CuotaEspecial)
                .Include(p => p.CuotaEspecialCasa).ThenInclude(c => c.House)
                .Where(p => p.CuotaEspecialCasa != null
                         && p.CuotaEspecialCasa.HouseId == houseId
                         && p.CuotaEspecialCasaId == idCuotaCasaMes)
                .OrderByDescending(p => p.FechaPago)
                .AsNoTracking()
                .ToListAsync();
        }


        public async Task<IEnumerable<Payments>> GetPaymentsForInvoiceAsync(int idCuotaCasaMes)
        {
            await using var db = await _factory.CreateDbContextAsync();

            return await db.Payments
                .Include(p => p.CuotaEspecialCasa).ThenInclude(c => c.CuotaEspecial)
                .Include(p => p.CuotaEspecialCasa).ThenInclude(c => c.House)
                .Where(p => p.CuotaEspecialCasaId == idCuotaCasaMes)
                .OrderByDescending(p => p.FechaPago)
                .AsNoTracking()
                .ToListAsync();
        }


        public async Task<bool> ConfirmPayment(Payments payment)
        {
            await using var db = await _factory.CreateDbContextAsync();
            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                int usuarioId = await PaymentConfirm(db, payment, transaction);

                if (usuarioId > 0)
                {
                    await transaction.CommitAsync();
                    await _notificacionService.EnviarNotificacionAsync(usuarioId);
                    return true;
                }
                else
                {
                    throw new Exception("Falló al confirmar");
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error al guardar: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> MasiveConfirmPayment(List<Payments> payments)
        {
            if (payments == null || !payments.Any()) return false;

            await using var db = await _factory.CreateDbContextAsync();
            await using var transaction = await db.Database.BeginTransactionAsync();
            var usuariosA_Notificar = new List<int>();

            try
            {
                foreach (var payment in payments)
                {
                    int usuarioId = await PaymentConfirm(db, payment, transaction);
                    usuariosA_Notificar.Add(usuarioId);

                }

                await transaction.CommitAsync();

                foreach (var userId in usuariosA_Notificar.Distinct())
                    await _notificacionService.EnviarNotificacionAsync(userId);

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error al guardar: {ex.Message}");
                return false;
            }
        }



        private async Task<int> PaymentConfirm(AppDbContext db, Payments payment, IDbContextTransaction transaction)
        {
            var cuotaCasa = await db.CuotaEspecialCasa.FirstOrDefaultAsync(x => x.Id == payment.CuotaEspecialCasaId);
            if (cuotaCasa == null)
                throw new Exception("Cuota no encontrada");

            var cuota = await db.CuotaEspecial.FirstOrDefaultAsync(x => x.Id == cuotaCasa.CuotaEspecialId);
            if (cuota == null)
                throw new Exception("Cuota no encontrada");

            cuotaCasa.MontoPagado += payment.Monto;
            cuotaCasa.SaldoPendiente = cuotaCasa.MontoTotal - cuotaCasa.MontoPagado;
            cuota.MontoRecaudado += payment.Monto;
            cuotaCasa.Estado = cuotaCasa.SaldoPendiente <= 0
                ? EstadoCuotaCasa.Confirmada
                : EstadoCuotaCasa.PagoParcial;

            string mensaje, titulo;
            if (cuotaCasa.Estado == EstadoCuotaCasa.PagoParcial)
            {
                mensaje = $"Pago parcial confirmado · {cuota.Motivo} · Ref: {payment.Referencia}";
                titulo = "Cuota Especial: Pago Parcial";
            }
            else
            {
                mensaje = $"Pago confirmado · {cuota.Motivo} · Ref: {payment.Referencia}";
                titulo = "Cuota Especial: Pago Confirmado";
            }

            payment.Estado = "Confirmada";

            db.Payments.Update(payment);
            db.CuotaEspecialCasa.Update(cuotaCasa);
            db.CuotaEspecial.Update(cuota);

            await _movimientoRepo.SaveMovimiento(
                db,
                monto: payment.Monto,
                montoBs: payment.MontoBs,
                tasa: payment.Tasa,
                idCasa: cuotaCasa.HouseId,
                cuota: cuota,
                factura: null
            );

            var usuario = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.HouseId == cuotaCasa.HouseId);

            if (usuario == null)
                throw new Exception("Usuario no encontrado para la casa");

            await db.Notificaciones.AddAsync(new Notificaciones
            {
                UserId = usuario.Id,
                Titulo = titulo,
                Mensaje = mensaje,
                Tipo = "Payments",
                ReferenciaId = payment.Id
            });

            await db.SaveChangesAsync();

            return usuario.Id;
        }
    }
}
