using Condominio.Domain.DB;
using Condominio.Domain.Entities;
using Condominio.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace Condominio.Infrastructure.Repositories
{
    public class FacturaMesCasaRepository : GenericRepository<FacturaMesCasa>
    {
        private readonly NotificacionRepository _notifRepo;
        private readonly INotificacionRealTimeService _notificacionService;
        private readonly MovimientosRepository _movimientoRepo;   // ← NUEVO

        public FacturaMesCasaRepository(
       AppDbContext context,
       NotificacionRepository notifRepo,
       INotificacionRealTimeService notificacionService,
       MovimientosRepository movimientoRepo) : base(context)  // ← NUEVO
        {
            _notifRepo = notifRepo;
            _notificacionService = notificacionService;
            _movimientoRepo = movimientoRepo;   // ← NUEVO
        }

        public override async Task<IEnumerable<FacturaMesCasa>> GetAllAsync()
        {
            _context.ChangeTracker.Clear();

            // ✅ UNA SOLA CONSULTA CON JOIN (más eficiente)
            var query = from factura in _dbSet
                        join house in _context.Houses on factura.HouseId equals house.Id
                        join user in _context.Users on house.Id equals user.HouseId into ownerGroup
                        from owner in ownerGroup.DefaultIfEmpty()
                        join payments in _context.Payments on factura.Id equals payments.FacturaMesCasaId into paymentGroup

                        // 🔥 1. AGREGAMOS EL JOIN A FACTURAMES
                        join facturaMes in _context.FacturaMes on factura.FacturaMesId equals facturaMes.Id

                        select new FacturaMesCasa
                        {
                            Id = factura.Id,
                            FacturaMesId = factura.FacturaMesId,
                            HouseId = factura.HouseId,
                            MontoTotal = factura.MontoTotal,
                            MontoPagado = factura.MontoPagado,
                            SaldoPendiente = factura.SaldoPendiente,
                            MontoBs = factura.MontoBs,
                            Estado = factura.Estado,
                            Comentario = factura.Comentario,
                            CreatedAt = factura.CreatedAt,
                            UpdatedAt = factura.UpdatedAt,
                            House = house,

                            // 🔥 2. ASIGNAMOS LA ENTIDAD QUE ACABAMOS DE TRAER EN EL JOIN
                            FacturaMes = facturaMes,

                            Payments = paymentGroup.ToList(),
                            User = owner
                        };

            // ✅ Incluir hijos
            var result = await query
                .OrderByDescending(x => x.FacturaMes.Year)
                .ThenByDescending(x => x.FacturaMes.Mes)
                .ToListAsync();

            // ✅ Cargar hijos por separado (1 consulta adicional)
            var facturaIds = result.Select(x => x.FacturaMesId).Distinct().ToList();
            if (facturaIds.Any())
            {
                var hijos = await _context.FacturaMesHijo
                    .Where(h => facturaIds.Contains(h.FacturaMesId))
                    .ToListAsync();

                foreach (var factura in result)
                {
                    // ✅ Validamos que FacturaMes no sea nulo para evitar NullReferenceException
                    if (factura.FacturaMes != null)
                    {
                        factura.FacturaMes.FacturaMesHijos = hijos
                            .Where(h => h.FacturaMesId == factura.FacturaMesId)
                            .ToList();
                    }
                }
            }

            return result;
        }


        public async Task<IEnumerable<FacturaMesCasa>> GetAllForUserAsync(int id)
        {
            _context.ChangeTracker.Clear();

            var idCasa = await _context.Users.Where(x => x.Id == id).Select(x => x.HouseId).FirstOrDefaultAsync();
            return await _dbSet.Include(x => x.House).Include(x => x.FacturaMes).ThenInclude(x => x.FacturaMesHijos)
                .ThenInclude(x => x.CategoriaGasto).Where(x => x.House.Id == idCasa)
                .OrderByDescending(x => x.FacturaMes.Year)
                .ThenByDescending(x => x.FacturaMes.Mes).ToListAsync();
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


        public async Task<bool> RegistrarMultiplesPagoFactura(List<Payments> pagos)
        {
            if (pagos == null || !pagos.Any()) return false;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // ✅ 1. Transformamos la lista usando LINQ (Select)
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

                // ✅ 2. Agregamos TODA la lista al contexto de una vez
                await _context.Payments.AddRangeAsync(listaPayments);

                // ✅ 3. Guardamos (EF Core optimiza los inserts masivos internamente)
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"❌ Error: {ex.Message}");
                return false;
            }
        }

        public async Task<IEnumerable<Payments>> GetPaymentsForUserAsync(int userId, int idFacturaMes)
        {
            try
            {
                _context.ChangeTracker.Clear();

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
                    .Where(p =>
                        (p.FacturaMesCasa != null && p.FacturaMesCasa.HouseId == houseId && p.FacturaMesCasaId == idFacturaMes)
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


        public async Task<IEnumerable<Payments>> GetPaymentsForInvoiceAsync(int idFacturaMes)
        {
            try
            {
                _context.ChangeTracker.Clear();

                var payments = await _context.Payments
                    .Include(p => p.FacturaMesCasa)
                        .ThenInclude(f => f.FacturaMes)
                    .Include(p => p.FacturaMesCasa)
                        .ThenInclude(f => f.House)
                    .Where(p =>
                         p.FacturaMesCasaId == idFacturaMes
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

                int usuarioId = await PaymentConfirm(payment, transaction);


                if(usuarioId > 0)
                {
                    await transaction.CommitAsync();


                    await _notificacionService.EnviarNotificacionAsync(usuarioId);

                    return true; // Éxito

                }
                else
                {
                    throw new Exception($"Falló al confirmar");
                }

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



        public async Task<bool> MasiveConfirmPayment(List<Payments> payments)
        {
            if (payments == null || !payments.Any()) return false;

            // ✅ 1. Iniciar transacción UNA SOLA VEZ
            using var transaction = await _context.Database.BeginTransactionAsync();

            var usuariosA_Notificar = new List<int>();

            try
            {
                // ✅ 2. Recorrer TODOS los pagos
                foreach (var payment in payments)
                {
                    int usuarioId = await PaymentConfirm(payment, transaction);

                    if (usuarioId > 0)
                    {
                        usuariosA_Notificar.Add(usuarioId);
                    }
                    else
                    {
                        // Si uno falla, lanzamos la excepción para abortar TODO
                        throw new Exception($"Falló al confirmar el pago con ID {payment.Id}");
                    }
                }

                // ✅ 3. Si el bucle termina sin errores, hacemos el Commit GLOBAL
                await transaction.CommitAsync();

                // ✅ 4. Enviamos las notificaciones DESPUÉS del commit exitoso
                foreach (var userId in usuariosA_Notificar.Distinct())
                {
                    await _notificacionService.EnviarNotificacionAsync(userId);
                }

                return true; // Todo el lote fue exitoso
            }
            catch (Exception ex)
            {
                // ❌ 5. Si algo falló, hacemos un único Rollback GLOBAL
                await transaction.RollbackAsync();
                Console.WriteLine($"❌ Error en lote masivo, se revirtió todo: {ex.Message}");
                return false;
            }
        }

        private async Task<int> PaymentConfirm(Payments payment, IDbContextTransaction transaction)
        {

            try
            {
                var facturaMesCasa = await _context.FacturaMesCasa
              .FirstOrDefaultAsync(x => x.Id == payment.FacturaMesCasaId);

                if (facturaMesCasa == null)
                    throw new Exception("Factura no encontrada");



                var factura = await _context.FacturaMes
                    .FirstOrDefaultAsync(x => x.Id == facturaMesCasa.FacturaMesId);

                if (factura == null)
                    throw new Exception("Factura no encontrada");



                // 2. ✅ SUMAR el monto recaudado (NO asignar)
                facturaMesCasa.MontoPagado = facturaMesCasa.MontoPagado + payment.Monto;


                facturaMesCasa.SaldoPendiente = facturaMesCasa.MontoTotal - facturaMesCasa.MontoPagado;

                factura.MontoRecaudado += payment.Monto;

                facturaMesCasa.Estado = facturaMesCasa.SaldoPendiente <= 0 ? "Confirmada" : "Pago Parcial";

                string mensaje = "";
                string titulo = "";

                if (facturaMesCasa.Estado == "Pago Parcial")
                {
                    mensaje = $"Pago parcial confirmado · {factura.NombreMes} {factura.Year} · Ref: {payment.Referencia}";
                    titulo = $"Factura: Pago Parcial";
                }
                else
                {
                    mensaje = $"Pago confirmado · {factura.NombreMes} {factura.Year} · Ref: {payment.Referencia}";
                    titulo = $"Factura: Pago Confirmado";

                }

                payment.Estado = "Confirmada";


                _context.Payments.Update(payment);
                _context.FacturaMesCasa.Update(facturaMesCasa);

                _context.FacturaMes.Update(factura);

                await _movimientoRepo.SaveMovimiento(
                    monto: payment.Monto,
                    montoBs: payment.MontoBs,
                    tasa: payment.Tasa,
                    idCasa: facturaMesCasa.HouseId,
                    factura: factura,
                    cuota: null
                );

                var usuario = await _context.Users
        .AsNoTracking()
        .FirstOrDefaultAsync(u => u.HouseId == facturaMesCasa.HouseId);

                await _context.Notificaciones.AddAsync(new Notificaciones
                {
                    UserId = usuario.Id,
                    Titulo = titulo,
                    Mensaje = mensaje,
                    Tipo = "Payments",
                    ReferenciaId = payment.Id
                });

                await _context.SaveChangesAsync();
                return usuario.Id;
            }
            catch (Exception ex)
            {
                return 0;
            }

        }

    }
}
