using Condominio.Domain.DB;
using Condominio.Domain.Entities;
using Condominio.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Condominio.Infrastructure.Repositories
{
    public class CuotaEspecialCasaRepository : GenericRepository<CuotaEspecialCasa>
    {
        private readonly INotificacionRealTimeService _notificacionService;
        private readonly NotificacionRepository _notifRepo;
        private readonly MovimientosRepository _movimientoRepo;

        public CuotaEspecialCasaRepository(
            IDbContextFactory<AppDbContext> factory,
            NotificacionRepository notifRepo,
            INotificacionRealTimeService notificacionService,
            MovimientosRepository movimientoRepo) : base(factory)
        {
            _notifRepo = notifRepo;
            _notificacionService = notificacionService;
            _movimientoRepo = movimientoRepo;
        }


        public async Task<(List<CuotaEspecialCasa> Items, int Total)> GetPagedAsync(
    int skip, int take, string filtro = null)
        {
            await using var db = await _factory.CreateDbContextAsync();

            var query = from item in db.Set<CuotaEspecialCasa>()
                        join user in db.Users on item.HouseId equals user.HouseId into ownerGroup
                        from owner in ownerGroup.DefaultIfEmpty()
                        join payments in db.Payments on item.Id equals payments.CuotaEspecialCasaId into paymentGroup
                        select new { item, owner, paymentGroup };

            // ✅ FILTRO GLOBAL (Buscador o embudo)
            if (!string.IsNullOrWhiteSpace(filtro))
            {
                string valorBusqueda = filtro;
                // ✅ 1. Manejar el filtro del buscador general (Contains)
                if (filtro.Contains("Contains(\""))
                {
                    var inicio = filtro.IndexOf("Contains(\"") + 10;
                    var fin = filtro.IndexOf("\")", inicio);
                    if (fin > inicio)
                    {
                        valorBusqueda = filtro.Substring(inicio, fin - inicio);
                    }
                }
                // ✅ 2. Manejar el filtro del embudo de Radzen (Igualdad ==)
                else if (filtro.Contains("== \""))
                {
                    var inicio = filtro.IndexOf("== \"") + 4;
                    var fin = filtro.IndexOf("\"", inicio);
                    if (fin > inicio)
                    {
                        valorBusqueda = filtro.Substring(inicio, fin - inicio);
                    }
                }

                var f = valorBusqueda.ToLower();

                // ✅ 3. Aplicar el filtro
                query = query.Where(x =>
                    x.item.Estado.ToString().ToLower().Contains(f) ||
                    x.item.House.Number.ToLower().Contains(f) || // Asegúrate de que Number sea string
                    (x.owner != null && x.owner.Name != null && x.owner.Name.ToLower().Contains(f)) ||
                    (x.owner != null && x.owner.Cedula != null && x.owner.Cedula.ToLower().Contains(f)) ||
                    (x.item.Referencia != null && x.item.Referencia.ToLower().Contains(f)) ||
                    x.item.MontoTotal.ToString().Contains(f)
                );
            }

            var total = await query.CountAsync();

            var result = await query
                .Select(x => new CuotaEspecialCasa
                {
                    Id = x.item.Id,
                    CuotaEspecialId = x.item.CuotaEspecialId,
                    HouseId = x.item.HouseId,
                    MontoTotal = x.item.MontoTotal,
                    MontoPagado = x.item.MontoPagado,
                    SaldoPendiente = x.item.SaldoPendiente,
                    MontoBs = x.item.MontoBs,
                    Estado = x.item.Estado,
                    Referencia = x.item.Referencia,
                    MetodoPago = x.item.MetodoPago,
                    FechaPago = x.item.FechaPago,
                    Comentario = x.item.Comentario,
                    CreatedAt = x.item.CreatedAt,
                    UpdatedAt = x.item.UpdatedAt,
                    House = x.item.House,
                    CuotaEspecial = x.item.CuotaEspecial,
                    Payments = x.paymentGroup.ToList(),
                    User = x.owner
                })
// ✅ 1er Orden: Las que tienen pagos pendientes primero
.OrderByDescending(x => x.Payments.Any(p => p.Estado == "Pendiente"))
// ✅ 2do Orden: Luego por fecha de creación (más recientes primero)
.ThenByDescending(x => x.CreatedAt)
.Skip(skip)
.Take(take)
.ToListAsync();

            return (result, total);
        }


        public async Task<(List<CuotaEspecialCasa> Items, int Total)> GetPagedForUserAsync(
    int skip, int take, int userId, string filtro = null)
        {
            await using var db = await _factory.CreateDbContextAsync();

            // 1. Buscamos el ID de la casa del usuario
            var houseId = await db.Users
                .Where(u => u.Id == userId)
                .Select(u => u.HouseId)
                .FirstOrDefaultAsync();

            if (houseId == null || houseId == 0)
                return (new List<CuotaEspecialCasa>(), 0);

            // 2. Armamos la query base filtrando por su casa
            var query = db.Set<CuotaEspecialCasa>()
                .Where(x => x.HouseId == houseId)
                .Include(x => x.House)
                .Include(x => x.CuotaEspecial)
                .Include(x => x.Payments)
                .AsNoTracking()
                .AsQueryable();

            // 3. Filtro global
            if (!string.IsNullOrWhiteSpace(filtro))
            {
                string valorBusqueda = filtro;
                if (filtro.Contains("Contains(\""))
                {
                    var inicio = filtro.IndexOf("Contains(\"") + 10;
                    var fin = filtro.IndexOf("\")", inicio);
                    if (fin > inicio)
                    {
                        valorBusqueda = filtro.Substring(inicio, fin - inicio);
                    }
                }

                var f = valorBusqueda.ToLower();

                query = query.Where(x =>
                    x.Estado.ToString().ToLower().Contains(f) ||
                    (x.Referencia != null && x.Referencia.ToLower().Contains(f)) ||
                    x.MontoTotal.ToString().Contains(f)
                );
            }

            var total = await query.CountAsync();

            // 4. Paginamos
            var result = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return (result, total);
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

        // ✅ OPTIMIZADO: Recibe solo el ID para evitar conflictos de rastreo
        public async Task<bool> ConfirmarPagoCuotaCasa(int cuotaCasaId)
        {
            await using var db = await _factory.CreateDbContextAsync();
            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                var item = await db.CuotaEspecialCasa.FirstOrDefaultAsync(x => x.Id == cuotaCasaId);
                if (item == null) throw new Exception("Cuota Casa no encontrada");

                var cuota = await db.CuotaEspecial.FirstOrDefaultAsync(x => x.Id == item.CuotaEspecialId);
                if (cuota == null) throw new Exception("Cuota Especial no encontrada");

                cuota.MontoRecaudado += item.MontoTotal;
                item.Estado = EstadoCuotaCasa.Confirmada;

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
                    MontoEfectivo = pago.MontoEfectivo,
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
                int usuarioId = await PaymentConfirm(db, payment.Id);

                await transaction.CommitAsync();
                await _notificacionService.EnviarNotificacionAsync(usuarioId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error al guardar: {ex.Message}");
                return false;
            }
        }


        public async Task<bool> DeniedPayment(Payments payment)
        {
            await using var db = await _factory.CreateDbContextAsync();
            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                int usuarioId = await PaymentDenied(db, payment.Id);

                await transaction.CommitAsync();
                await _notificacionService.EnviarNotificacionAsync(usuarioId);
                return true;
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
                    int usuarioId = await PaymentConfirm(db, payment.Id);
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

        // ✅ OPTIMIZADO: Busca el pago por ID, sin rastrear objetos de la vista
        private async Task<int> PaymentConfirm(AppDbContext db, int paymentId)
        {
            var payment = await db.Payments.FirstOrDefaultAsync(x => x.Id == paymentId);
            if (payment == null)
                throw new Exception("Pago no encontrado");

            var cuotaCasa = await db.CuotaEspecialCasa.FirstOrDefaultAsync(x => x.Id == payment.CuotaEspecialCasaId);
            if (cuotaCasa == null)
                throw new Exception("Cuota no encontrada");

            var cuota = await db.CuotaEspecial.FirstOrDefaultAsync(x => x.Id == cuotaCasa.CuotaEspecialId);
            if (cuota == null)
                throw new Exception("Cuota no encontrada");

            decimal totalPagado = payment.Monto + (payment.MontoEfectivo ?? 0);

            cuotaCasa.MontoPagado += totalPagado;
            cuotaCasa.SaldoPendiente = cuotaCasa.MontoTotal - cuotaCasa.MontoPagado;
            cuota.MontoRecaudado += totalPagado;
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

            await _movimientoRepo.SaveMovimiento(
                db,
                monto: payment.Monto,
                montoBs: payment.MontoBs,
                montoEfectivo: payment.MontoEfectivo,
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


        private async Task<int> PaymentDenied(AppDbContext db, int paymentId)
        {
            var payment = await db.Payments.FirstOrDefaultAsync(x => x.Id == paymentId);
            if (payment == null)
                throw new Exception("Pago no encontrado");

            var cuotaCasa = await db.CuotaEspecialCasa.FirstOrDefaultAsync(x => x.Id == payment.CuotaEspecialCasaId);
            if (cuotaCasa == null)
                throw new Exception("Cuota no encontrada");

            var cuota = await db.CuotaEspecial.FirstOrDefaultAsync(x => x.Id == cuotaCasa.CuotaEspecialId);
            if (cuota == null)
                throw new Exception("Cuota no encontrada");

         

            string mensaje, titulo;
          
                mensaje = $"Pago Rechazado · {cuota.Motivo} · Ref: {payment.Referencia}";
                titulo = "Cuota Especial: Pago Rechazado";
            

            payment.Estado = "Rechazado";


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