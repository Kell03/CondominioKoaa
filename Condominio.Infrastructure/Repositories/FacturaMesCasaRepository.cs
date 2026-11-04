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
    public class FacturaMesCasaRepository : GenericRepository<FacturaMesCasa>
    {
        private readonly NotificacionRepository _notifRepo;
        private readonly INotificacionRealTimeService _notificacionService;
        private readonly MovimientosRepository _movimientoRepo;

        public FacturaMesCasaRepository(
            IDbContextFactory<AppDbContext> factory,
            NotificacionRepository notifRepo,
            INotificacionRealTimeService notificacionService,
            MovimientosRepository movimientoRepo) : base(factory)
        {
            _notifRepo = notifRepo;
            _notificacionService = notificacionService;
            _movimientoRepo = movimientoRepo;
        }

        public async Task<(List<FacturaMesCasa> Items, int Total)> GetPagedForUserAsync(int skip, int take, int userId, string filtro = null)
        {
            await using var db = await _factory.CreateDbContextAsync();

            var houseId = await db.Users
                .Where(u => u.Id == userId)
                .Select(u => u.HouseId)
                .FirstOrDefaultAsync();

            if (houseId == null || houseId == 0)
                return (new List<FacturaMesCasa>(), 0);

            var query = db.Set<FacturaMesCasa>()
                .Where(x => x.HouseId == houseId) // ✅ Filtro base: solo su casa
                .Include(x => x.House)
                .Include(x => x.User)
                .Include(x => x.Payments)
                .Include(x => x.FacturaMes)
                    .ThenInclude(fm => fm.FacturaMesHijos)
                .AsNoTracking();

            // ✅ FILTRO GLOBAL (Estado, Año, Mes)
            if (!string.IsNullOrWhiteSpace(filtro))
            {
                // ✅ TRUCO: Si el filtro viene de Radzen, extraemos solo el valor de búsqueda.
                // Radzen envía algo como: x.house.number.Contains("1")
                string valorBusqueda = filtro;

                // Si contiene la palabra "Contains", extraemos lo que está entre comillas
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

                // Ahora sí, buscamos el valor limpio
                query = query.Where(x =>
                    x.Estado.ToLower().Contains(f) ||
                    x.FacturaMes.Year.ToString().Contains(f) ||
                    x.FacturaMes.Mes.ToString().Contains(f) ||
                    x.MontoTotal.ToString().Contains(f)
                );






            }

            var total = await query.CountAsync();

            var result = await query
                .OrderByDescending(x => x.FacturaMes.Year)
                .ThenByDescending(x => x.FacturaMes.Mes)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return (result, total);
        }
        public async Task<(List<FacturaMesCasa> Items, int Total)> GetPagedAsync(int skip, int take, string filtro = null)
        {
            await using var db = await _factory.CreateDbContextAsync();

            var query = from factura in db.Set<FacturaMesCasa>()
                        join house in db.Houses on factura.HouseId equals house.Id
                        join user in db.Users on house.Id equals user.HouseId into ownerGroup
                        from owner in ownerGroup.DefaultIfEmpty()
                        join payments in db.Payments on factura.Id equals payments.FacturaMesCasaId into paymentGroup
                        join facturaMes in db.FacturaMes on factura.FacturaMesId equals facturaMes.Id
                        select new { factura, house, owner, paymentGroup, facturaMes };

            // ✅ FILTRO GLOBAL
            if (!string.IsNullOrWhiteSpace(filtro))
            {
                // ✅ TRUCO: Si el filtro viene de Radzen, extraemos solo el valor de búsqueda.
                // Radzen envía algo como: x.house.number.Contains("1")
                string valorBusqueda = filtro;

                // Si contiene la palabra "Contains", extraemos lo que está entre comillas
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

                // Ahora sí, buscamos el valor limpio
                query = query.Where(x =>
                    x.house.Number.ToLower().Contains(f) ||
                    x.factura.Estado.ToLower().Contains(f) ||
                    x.facturaMes.Year.ToString().Contains(f) ||
                    x.facturaMes.Mes.ToString().Contains(f) ||
                    x.factura.MontoTotal.ToString().Contains(f)
                );
            }

            var total = await query.CountAsync();

            var result = await query
                .Select(x => new FacturaMesCasa
                {
                    Id = x.factura.Id,
                    FacturaMesId = x.factura.FacturaMesId,
                    HouseId = x.factura.HouseId,
                    MontoTotal = x.factura.MontoTotal,
                    MontoPagado = x.factura.MontoPagado,
                    SaldoPendiente = x.factura.SaldoPendiente,
                    MontoBs = x.factura.MontoBs,
                    Estado = x.factura.Estado,
                    Comentario = x.factura.Comentario,
                    CreatedAt = x.factura.CreatedAt,
                    UpdatedAt = x.factura.UpdatedAt,
                    House = x.house,
                    FacturaMes = x.facturaMes,
                    Payments = x.paymentGroup.ToList(),
                    User = x.owner
                })
                .OrderByDescending(x => x.FacturaMes.Year)
                .ThenByDescending(x => x.FacturaMes.Mes)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return (result, total);
        }
        public async Task<(List<FacturaMesCasa> Items, int Total)> GetPagedSaldoAsync(
    int skip, int take, string filtro = null, string orderBy = null)
        {
            await using var db = await _factory.CreateDbContextAsync();

            // 1. Query base con el JOIN y la condición de Saldo (que tenga Motivo)
            var query = from factura in db.Set<FacturaMesCasa>()
                        join house in db.Houses on factura.HouseId equals house.Id
                        join user in db.Users on house.Id equals user.HouseId into ownerGroup
                        from owner in ownerGroup.DefaultIfEmpty()
                        join payments in db.Payments on factura.Id equals payments.FacturaMesCasaId into paymentGroup
                        join facturaMes in db.FacturaMes on factura.FacturaMesId equals facturaMes.Id
                        where facturaMes.Motivo != null && facturaMes.Motivo != "" // ✅ Solo los que tienen Motivo
                        select new { factura, house, owner, paymentGroup, facturaMes };

            // 2. ✅ FILTRO GLOBAL (Buscador o embudo)
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

                // Buscamos en casa, estado, año, mes y EL MOTIVO
                query = query.Where(x =>
                    x.house.Number.Contains(f) ||
                    x.factura.Estado.ToLower().Contains(f) ||
                    (x.owner != null && x.owner.Name != null && x.owner.Name.ToLower().Contains(f)) ||
                    (x.owner != null && x.owner.Cedula != null && x.owner.Cedula.ToLower().Contains(f)) ||

                    x.facturaMes.Year.ToString().Contains(f) ||
                    x.facturaMes.Mes.ToString().Contains(f) ||
                    x.facturaMes.Motivo.ToLower().Contains(f) // 👈 Prioridad aquí
                );
            }

            // 3. Contar el total
            var total = await query.CountAsync();

            // 4. Traer la página actual
            var result = await query
                .Select(x => new FacturaMesCasa
                {
                    Id = x.factura.Id,
                    FacturaMesId = x.factura.FacturaMesId,
                    HouseId = x.factura.HouseId,
                    MontoTotal = x.factura.MontoTotal,
                    MontoPagado = x.factura.MontoPagado,
                    SaldoPendiente = x.factura.SaldoPendiente,
                    MontoBs = x.factura.MontoBs,
                    Estado = x.factura.Estado,
                    Comentario = x.factura.Comentario,
                    CreatedAt = x.factura.CreatedAt,
                    UpdatedAt = x.factura.UpdatedAt,
                    House = x.house,
                    FacturaMes = x.facturaMes,
                    Payments = x.paymentGroup.ToList(),
                    User = x.owner
                })
                .OrderByDescending(x => x.FacturaMes.Year)
                .ThenByDescending(x => x.FacturaMes.Mes)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            // 5. Cargar los hijos
            var facturaIds = result.Select(x => x.FacturaMesId).Distinct().ToList();
            if (facturaIds.Any())
            {
                var hijos = await db.FacturaMesHijo
                    .Where(h => facturaIds.Contains(h.FacturaMesId))
                    .AsNoTracking()
                    .ToListAsync();

                foreach (var factura in result)
                {
                    if (factura.FacturaMes != null)
                    {
                        factura.FacturaMes.FacturaMesHijos = hijos
                            .Where(h => h.FacturaMesId == factura.FacturaMesId)
                            .ToList();
                    }
                }
            }

            return (result, total);
        }

        public async Task<IEnumerable<FacturaMesCasa>> GetAllForUserAsync(int id)
        {
            await using var db = await _factory.CreateDbContextAsync();

            var idCasa = await db.Users.Where(x => x.Id == id).Select(x => x.HouseId).FirstOrDefaultAsync();

            return await db.Set<FacturaMesCasa>()
                .Include(x => x.House)
                .Include(x => x.FacturaMes).ThenInclude(x => x.FacturaMesHijos).ThenInclude(x => x.CategoriaGasto)
                .Where(x => x.House.Id == idCasa)
                .AsNoTracking()
                .OrderByDescending(x => x.FacturaMes.Year)
                .ThenByDescending(x => x.FacturaMes.Mes)
                .ToListAsync();
        }

        public async Task<bool> ConfirmarPagoFacturaCasa(FacturaMesCasa item)
        {
            await using var db = await _factory.CreateDbContextAsync();
            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                var factura = await db.FacturaMes.FirstOrDefaultAsync(x => x.Id == item.FacturaMesId);
                if (factura == null)
                    throw new Exception("Factura no encontrada");

                factura.MontoRecaudado += item.MontoTotal;
                item.Estado = "Confirmada";

                db.FacturaMesCasa.Update(item);
                db.FacturaMes.Update(factura);

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

        public async Task<bool> RegistrarPagoFactura(Payments pago)
        {
            await using var db = await _factory.CreateDbContextAsync();
            try
            {
                var payment = new Payments
                {
                    FacturaMesCasaId = pago.FacturaMesCasaId,
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
                    FacturaMesCasaId = item.FacturaMesCasaId,
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

        public async Task<IEnumerable<Payments>> GetPaymentsForUserAsync(int userId, int idFacturaMes)
        {
            await using var db = await _factory.CreateDbContextAsync();

            var houseId = await db.Users
                .Where(u => u.Id == userId)
                .Select(u => u.HouseId)
                .FirstOrDefaultAsync();

            if (houseId == null || houseId == 0)
                return new List<Payments>();

            return await db.Payments
                .Include(p => p.FacturaMesCasa).ThenInclude(f => f.FacturaMes)
                .Include(p => p.FacturaMesCasa).ThenInclude(f => f.House)
                .Where(p => p.FacturaMesCasa != null
                         && p.FacturaMesCasa.HouseId == houseId
                         && p.FacturaMesCasaId == idFacturaMes)
                .AsNoTracking()
                .OrderByDescending(p => p.FechaPago)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payments>> GetPaymentsForInvoiceAsync(int idFacturaMes)
        {
            await using var db = await _factory.CreateDbContextAsync();

            return await db.Payments
                .Include(p => p.FacturaMesCasa).ThenInclude(f => f.FacturaMes)
                .Include(p => p.FacturaMesCasa).ThenInclude(f => f.House)
                .Where(p => p.FacturaMesCasaId == idFacturaMes)
                .AsNoTracking()
                .OrderByDescending(p => p.FechaPago)
                .ToListAsync();
        }

        public async Task<bool> ConfirmPayment(Payments payment)
        {
            await using var db = await _factory.CreateDbContextAsync();
            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                int usuarioId = await PaymentConfirm(db, payment);

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
                    int usuarioId = await PaymentConfirm(db, payment);
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
                Console.WriteLine($"❌ Error en lote masivo, se revirtió todo: {ex.Message}");
                return false;
            }
        }

        private async Task<int> PaymentConfirm(AppDbContext db, Payments payment)
        {
            var facturaMesCasa = await db.FacturaMesCasa
                .FirstOrDefaultAsync(x => x.Id == payment.FacturaMesCasaId);

            if (facturaMesCasa == null)
                throw new Exception("Factura no encontrada");

            var factura = await db.FacturaMes
                .FirstOrDefaultAsync(x => x.Id == facturaMesCasa.FacturaMesId);

            if (factura == null)
                throw new Exception("Factura no encontrada");

            facturaMesCasa.MontoPagado += payment.Monto;
            facturaMesCasa.SaldoPendiente = facturaMesCasa.MontoTotal - facturaMesCasa.MontoPagado;
            factura.MontoRecaudado += payment.Monto;
            facturaMesCasa.Estado = facturaMesCasa.SaldoPendiente <= 0 ? "Confirmada" : "Pago Parcial";

            string mensaje, titulo;
            if (facturaMesCasa.Estado == "Pago Parcial")
            {
                mensaje = $"Pago parcial confirmado · {factura.NombreMes} {factura.Year} · Ref: {payment.Referencia}";
                titulo = "Factura: Pago Parcial";
            }
            else
            {
                mensaje = $"Pago confirmado · {factura.NombreMes} {factura.Year} · Ref: {payment.Referencia}";
                titulo = "Factura: Pago Confirmado";
            }

            payment.Estado = "Confirmada";

            db.Payments.Update(payment);
            db.FacturaMesCasa.Update(facturaMesCasa);
            db.FacturaMes.Update(factura);

            await _movimientoRepo.SaveMovimiento(
                db,
                monto: payment.Monto,
                montoBs: payment.MontoBs,
                tasa: payment.Tasa,
                idCasa: facturaMesCasa.HouseId,
                factura: factura,
                cuota: null
            );

            var usuario = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.HouseId == facturaMesCasa.HouseId);

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