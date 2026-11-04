using Condominio.Domain.DB;
using Condominio.Domain.Entities;
using Condominio.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Condominio.Infrastructure.Repositories
{
    public class MovimientosRepository : GenericRepository<Movimientos>
    {
        private readonly NotificacionRepository _notifRepo;
        private readonly INotificacionRealTimeService _notificacionService;

        public MovimientosRepository(
            IDbContextFactory<AppDbContext> factory,
            NotificacionRepository notifRepo,
            INotificacionRealTimeService notificacionService) : base(factory)
        {
            _notifRepo = notifRepo;
            _notificacionService = notificacionService;
        }

        public override async Task<IEnumerable<Movimientos>> GetAllAsync()
        {
            await using var db = await _factory.CreateDbContextAsync();

            return await db.Set<Movimientos>()
                .Include(c => c.House)
                .Include(x => x.CuotaEspecial)
                .Include(x => x.Factura)
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<decimal> GettavailablefundsAsync()
        {
            await using var db = await _factory.CreateDbContextAsync();

            return await db.Set<Movimientos>()
                .SumAsync(x => x.IdGasto == null ? (x.MontoBs ?? 0) : -(x.MontoBs ?? 0));
        }

        // Versión sin db: abre su propio contexto y guarda.
        public async Task SaveMovimiento(
            decimal monto,
            decimal? montoBs,
            decimal? tasa,
            FacturaMes? factura = null,
            CuotaEspecial? cuota = null,
            int? idCasa = null,
            Gasto? gasto = null)
        {
            await using var db = await _factory.CreateDbContextAsync();

            await SaveMovimiento(db, monto, montoBs, tasa, factura, cuota, idCasa, gasto);

            await db.SaveChangesAsync();
        }

        // Versión con db: participa de la transacción del llamador, NO guarda.
        public async Task SaveMovimiento(
            AppDbContext db,
            decimal monto,
            decimal? montoBs,
            decimal? tasa,
            FacturaMes? factura = null,
            CuotaEspecial? cuota = null,
            int? idCasa = null,
            Gasto? gasto = null)
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

            await db.Set<Movimientos>().AddAsync(movimiento);
        }
    }
}