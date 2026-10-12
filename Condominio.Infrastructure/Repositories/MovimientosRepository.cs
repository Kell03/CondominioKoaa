using Condominio.Domain.DB;
using Condominio.Domain.Entities;
using Condominio.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Amazon.S3.Util.S3EventNotification;

namespace Condominio.Infrastructure.Repositories
{
    public class MovimientosRepository : GenericRepository<Movimientos>
    {
        private readonly NotificacionRepository _notifRepo;
        private readonly INotificacionRealTimeService _notificacionService;

        public MovimientosRepository(AppDbContext context, NotificacionRepository notifRepo, INotificacionRealTimeService notificacionService) : base(context)
        {
            _notifRepo = notifRepo;
            _notificacionService = notificacionService;

        }



        public async Task SaveMoviemiento(decimal monto, TipoMovimiento tipo, int? idCasa, FacturaMes? factura, CuotaEspecial? cuota)
        {
            try
            {
                _context.ChangeTracker.Clear();


                TipoMovimiento tipo;

                if (factura != null || cuota != null)
                    tipo = TipoMovimiento.Entrada;
                else
                    tipo = TipoMovimiento.Salida;

                var movimiento = new Movimientos
                {
                    Fecha = DateTime.Now,
                    Tipo = tipo,
                    Monto = monto,
                    IdCasa = idCasa,
                    IdFactura = factura?.Id,
                    IdCuotaEspecial = cuota?.Id,
                    Descripcion = factura != null
                        ? $"Factura {factura.Mes}/{factura.Year}"
                        : cuota != null
                            ? cuota.Motivo
                            : "Movimiento",
                    CreatedAt = DateTime.Now
                };

                await _context.Movimientos.AddAsync(movimiento);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en AddAsync: {ex.Message}");
                throw;
            }
        }
    }
}
