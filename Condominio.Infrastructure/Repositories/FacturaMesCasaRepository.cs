using Condominio.Domain.DB;
using Condominio.Domain.Entities;
using Condominio.Domain.Interfaces;
using Condominio.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
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
        private readonly BrevoEmailService _emailService;
        private readonly IConfiguration _config;
        private readonly PdfGenerator _pdfGenerator;

        public FacturaMesCasaRepository(
            IDbContextFactory<AppDbContext> factory,
            NotificacionRepository notifRepo,
            PdfGenerator pdfGenerator,
            INotificacionRealTimeService notificacionService,
            MovimientosRepository movimientoRepo,
             IConfiguration config,
             BrevoEmailService emailService
             ) : base(factory)
        {
            _notifRepo = notifRepo;
            _notificacionService = notificacionService;
            _movimientoRepo = movimientoRepo;
            _pdfGenerator = pdfGenerator;
            _emailService = emailService;
            _config = config;
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
                // .Include(x => x.User) ❌ ELIMINADO
                .Include(x => x.Payments)
                .Include(x => x.FacturaMes)
                    .ThenInclude(fm => fm.FacturaMesHijos)
                      .ThenInclude(hijo => hijo.CategoriaGasto) 
                       .ThenInclude(p => p.Proveedor) 
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
                // ✅ 1er Orden: Las que tienen pagos pendientes van primero
                .OrderByDescending(x => x.Payments.Any(p => p.Estado == "Pendiente"))
                // ✅ 2do Orden: Si hay varias pendientes, se ordenan por fecha (más recientes primero)
                .ThenByDescending(x => x.FacturaMes.Year)
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
                // ✅ Pasamos solo el ID del pago para que PaymentConfirm lo busque fresco en la BD
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
                // ✅ Pasamos solo el ID del pago para que PaymentConfirm lo busque fresco en la BD
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
                Console.WriteLine($"❌ Error en lote masivo, se revirtió todo: {ex.Message}");
                return false;
            }
        }

        private async Task<int> PaymentConfirm(AppDbContext db, int paymentId)
        {
            // 1. Buscamos el pago real directamente en la base de datos
            var payment = await db.Payments
                .FirstOrDefaultAsync(x => x.Id == paymentId);

            if (payment == null)
                throw new Exception("Pago no encontrado");

            // 2. Buscamos la factura (EF Core la rastreará automáticamente)
            var facturaMesCasa = await db.FacturaMesCasa
                .FirstOrDefaultAsync(x => x.Id == payment.FacturaMesCasaId);

            if (facturaMesCasa == null)
                throw new Exception("Factura no encontrada");

            var factura = await db.FacturaMes
                .FirstOrDefaultAsync(x => x.Id == facturaMesCasa.FacturaMesId);

            if (factura == null)
                throw new Exception("Factura maestra no encontrada");

            // 3. Actualizamos los montos
            // ✅ 1. Calculamos el total que pagó el residente (Monto + Efectivo)
            decimal totalPagado = payment.Monto + (payment.MontoEfectivo ?? 0);

            // ✅ 2. Sumamos ese total a la factura de la casa y a la factura maestra
            facturaMesCasa.MontoPagado += totalPagado;
            facturaMesCasa.SaldoPendiente = facturaMesCasa.MontoTotal - facturaMesCasa.MontoPagado;
            factura.MontoRecaudado += totalPagado;

            facturaMesCasa.Estado = facturaMesCasa.SaldoPendiente <= 0 ? "Confirmada" : "Pago Parcial";

            // 4. Preparamos el mensaje
            string mensaje, titulo;
            if (facturaMesCasa.Estado == "Pago Parcial")
            {
                string body = (string.IsNullOrEmpty(factura.Motivo)) ? $"{factura.NombreMes}{factura.Year}" : $"{factura.Motivo}";
                mensaje = $"Pago parcial confirmado · {body} · Ref: {payment.Referencia}";
                titulo = "Factura: Pago Parcial";
            }
            else
            {
                string body = (string.IsNullOrEmpty(factura.Motivo)) ? $"{factura.NombreMes}{factura.Year}" : $"{factura.Motivo}";
                mensaje = $"Pago confirmado · {body} · Ref: {payment.Referencia}";
                titulo = "Factura: Pago Confirmado";
            }

            // 5. Actualizamos el estado del pago
            payment.Estado = "Confirmada";



            // Registramos el movimiento (solo si lo tienes en tu lógica)
            await _movimientoRepo.SaveMovimiento(
    db,
    monto: payment.Monto,
    montoBs: payment.MontoBs,
    tasa: payment.Tasa,
    idCasa: facturaMesCasa.HouseId,
    factura: factura,
    cuota: null,
    montoEfectivo: payment.MontoEfectivo
);


            // 6. Buscamos al usuario para notificarle
            var usuario = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.HouseId == facturaMesCasa.HouseId);

            if (usuario == null)
                throw new Exception("Usuario no encontrado para la casa");

            // 7. Registramos la notificación en la BD
            await db.Notificaciones.AddAsync(new Notificaciones
            {
                UserId = usuario.Id,
                Titulo = titulo,
                Mensaje = mensaje,
                Tipo = "Payments",
                ReferenciaId = payment.Id
            });


            // 9. Guardamos TODO. Como todo está rastreado por el mismo `db`, 
            // EF Core hace un solo UPDATE para cada entidad modificada.
            await db.SaveChangesAsync();

            return usuario.Id;
        }


        private async Task<int> PaymentDenied(AppDbContext db, int paymentId)
        {
            // 1. Buscamos el pago real directamente en la base de datos
            var payment = await db.Payments
                .FirstOrDefaultAsync(x => x.Id == paymentId);

            if (payment == null)
                throw new Exception("Pago no encontrado");

            // 2. Buscamos la factura (EF Core la rastreará automáticamente)
            var facturaMesCasa = await db.FacturaMesCasa
                .FirstOrDefaultAsync(x => x.Id == payment.FacturaMesCasaId);

            if (facturaMesCasa == null)
                throw new Exception("Factura no encontrada");

            var factura = await db.FacturaMes
                .FirstOrDefaultAsync(x => x.Id == facturaMesCasa.FacturaMesId);

            if (factura == null)
                throw new Exception("Factura maestra no encontrada");


            // 4. Preparamos el mensaje
            string mensaje, titulo;

                string body = (string.IsNullOrEmpty(factura.Motivo)) ? $"{factura.NombreMes}{factura.Year}" : $"{factura.Motivo}";
                mensaje = $"Pago Rechazado · {factura.Motivo} · Ref: {payment.Referencia}";
                titulo = "Factura: Pago Rechazado";
            

            // 5. Actualizamos el estado del pago
            payment.Estado = "Rechazado";



            // 6. Buscamos al usuario para notificarle
            var usuario = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.HouseId == facturaMesCasa.HouseId);

            if (usuario == null)
                throw new Exception("Usuario no encontrado para la casa");

            // 7. Registramos la notificación en la BD
            await db.Notificaciones.AddAsync(new Notificaciones
            {
                UserId = usuario.Id,
                Titulo = titulo,
                Mensaje = mensaje,
                Tipo = "Payments",
                ReferenciaId = payment.Id
            });


            // 9. Guardamos TODO. Como todo está rastreado por el mismo `db`, 
            // EF Core hace un solo UPDATE para cada entidad modificada.
            await db.SaveChangesAsync();

            return usuario.Id;
        }




        public async Task EnviarCorreosFacturaAsync(int facturaMesCasaId)
        {
            await using var db = await _factory.CreateDbContextAsync();

            FacturaMesCasa facturaMesCasa = await db.FacturaMesCasa.Include(f => f.FacturaMes)
                .FirstOrDefaultAsync(f => f.Id == facturaMesCasaId);

            int facturaId = facturaMesCasa.FacturaMesId;

            var factura = await db.FacturaMes
                .Include(f => f.FacturaMesHijos).ThenInclude(fh => fh.CategoriaGasto)
                .FirstOrDefaultAsync(f => f.Id == facturaId);

            if (factura == null) throw new Exception("Factura no encontrada");

            var casasPendientes = await db.FacturaMesCasa
     .Where(f => f.Id == facturaMesCasaId &&
                 (f.CorreoEnviado == false || f.CorreoEnviado == null))
     .ToListAsync();

            if (!casasPendientes.Any())
            {
                Console.WriteLine("⚠️ No hay correos pendientes por enviar en esta factura.");
                return;
            }


            var usuariosPorCasa = await db.Users
                .AsNoTracking()
                .Where(u => u.HouseId != null && u.HouseId == facturaMesCasa.HouseId)
                .GroupBy(u => u.HouseId!.Value)
                .ToDictionaryAsync(g => g.Key, g => g.First().Email);

            // Casas sin correo válido: las marcamos de una vez para no trabar pendientes
            var casasSinCorreo = casasPendientes
                .Where(c => !usuariosPorCasa.TryGetValue(c.HouseId, out var email) || string.IsNullOrWhiteSpace(email))
                .ToList();


            var casasConCorreo = casasPendientes.Except(casasSinCorreo).ToList();


            // Correo principal en "To" (puedes usar también _emailService.ObtenerRemitente() si lo agregaste)
            string correoPrincipalTo = "notificacionesensenada@solunarinf.com";

            var listaBcc = casasConCorreo
                .Select(c => usuariosPorCasa[c.HouseId]!.Trim())
                .Where(email => !email.Equals(correoPrincipalTo, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            decimal montoParaPdf = casasConCorreo.First().MontoTotal;
            var totalCasas = await db.Houses.CountAsync();
            byte[] pdfBytes = _pdfGenerator.GenerarAviso(factura, totalCasas, pagado:true);

            string nombreArchivo = $"Recibo_{factura.NombreMes}_{factura.Year}.pdf";
            string asunto = $"Recibo de Cobro - {factura.NombreMesCompleto}";
            string mensaje = $@"
<h3>Estimado propietario,</h3>
<p>Adjunto encontrará el recibo de cobro correspondiente al mes de <strong>{factura.NombreMesCompleto}</strong>.</p>
<p>El monto cancelado por su inmueble es de <strong>${montoParaPdf:N2}</strong>.</p>
<br/>
<p>Saludos cordiales,<br/>Administración La Ensenada</p>";

            // Brevo permite máximo 99 destinatarios por envío (1 en To + 98 en Bcc)
            const int maxBccPorEnvio = 98;
            var lotesBcc = listaBcc.Chunk(maxBccPorEnvio).ToList();
            var fechaEnvio = DateTime.Now;

            foreach (var lote in lotesBcc)
            {
                var bccLoteList = lote.ToList();

                await _emailService.EnviarCorreoConAdjuntoAsync(
                    destinatarioPrincipal: correoPrincipalTo,
                    asunto: asunto,
                    mensajeHtml: mensaje,
                    pdfBytes: pdfBytes,
                    nombreArchivo: nombreArchivo,
                    correosOcultosBcc: bccLoteList
                );

                // Marcamos como enviadas solo las casas de este lote que ya salió bien
                var casasDelLote = casasConCorreo
                    .Where(c => bccLoteList.Contains(usuariosPorCasa[c.HouseId]!.Trim(), StringComparer.OrdinalIgnoreCase))
                    .ToList();



                Console.WriteLine($"📧 Lote enviado en copia oculta a {bccLoteList.Count} destinatarios.");

                if (lotesBcc.Count > 1)
                {
                    await Task.Delay(1000);
                }
            }

           



            Console.WriteLine("✅ Proceso de envío de correos finalizado.");
        }
    }
}