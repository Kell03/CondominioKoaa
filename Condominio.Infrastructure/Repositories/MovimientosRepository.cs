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
        public override async Task<IEnumerable<Movimientos>> GetAllAsync()
        {
            _context.ChangeTracker.Clear();

            return await _dbSet.Include(c => c.House).Include(x => x.CuotaEspecial).Include(x => x.Factura).OrderByDescending(x => x.CreatedAt).ToListAsync();
        }


        public async Task SaveMovimiento(decimal monto, decimal? montoBs, decimal? tasa, FacturaMes? factura = null, CuotaEspecial? cuota = null, int? idCasa = null, Gasto? gasto = null)
        {
            try
            {


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
                    MontoBs = montoBs,
                    Tasa = tasa,
                    IdCasa = idCasa,
                    IdFactura = factura?.Id,
                    IdGasto = gasto?.Id,
                    IdCuotaEspecial = cuota?.Id,
                    Descripcion = factura != null
                        ? $"Factura {factura.Mes}/{factura.Year}"
                        : cuota != null
                            ? cuota.Motivo
                            : "Gasto",
                    CreatedAt = DateTime.Now
                };

                await _context.Movimientos.AddAsync(movimiento);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en AddAsync: {ex.Message}");
                throw;
            }
        }
    }
}
