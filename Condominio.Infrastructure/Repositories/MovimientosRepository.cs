using Condominio.Domain.DB;
using Condominio.Domain.Entities;
using Condominio.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
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

        public async Task<(List<Movimientos> Items, int Total)> GetPagedAsync(
    int skip, int take, string filtro = null, string orderBy = null)
        {
            await using var db = await _factory.CreateDbContextAsync();

            var query = db.Set<Movimientos>()
                .Include(x => x.House)
                .Include(x => x.CuotaEspecial)
                .Include(x => x.Factura)
                .AsNoTracking()
                .AsQueryable();

            // ✅ FILTRO GLOBAL
            // if (!string.IsNullOrWhiteSpace(filtro))
            // {
            //     string valorBusqueda = filtro;
            //     if (filtro.Contains("Contains(\""))
            //     {
            //         var inicio = filtro.IndexOf("Contains(\"") + 10;
            //         var fin = filtro.IndexOf("\")", inicio);
            //         if (fin > inicio)
            //         {
            //             valorBusqueda = filtro.Substring(inicio, fin - inicio);
            //         }
            //     }
            //
            //     var f = valorBusqueda.ToLower();
            //
            //     // Buscamos en la descripción, tipo, monto o casa
            //     query = query.Where(x =>
            //         x.Descripcion != null && x.Descripcion.ToLower().Contains(f) ||
            //         x.Tipo.ToString().ToLower().Contains(f) ||
            //         x.Monto.ToString().Contains(f) ||
            //         (x.House != null && x.House.Number.Contains(f))
            //     );
            // }
            if (!string.IsNullOrWhiteSpace(filtro))
            {
                // ✅ 1. Interceptar filtro de Fecha (si contiene DateTime.Parse)
                if (filtro.Contains("DateTime.Parse"))
                {
                    // Buscamos específicamente el texto entre comillas que sigue a DateTime.Parse(
                    var match = System.Text.RegularExpressions.Regex.Match(filtro, @"DateTime\.Parse\(""([^""]+)""");

                    if (match.Success)
                    {
                        // Extraemos la fecha (ej. "2026-11-22")
                        string fechaStr = match.Groups[1].Value;

                        if (DateTime.TryParse(fechaStr, out DateTime fecha))
                        {
                            // Creamos el rango: desde las 00:00:00 hasta el día siguiente a las 00:00:00
                            string fechaInicio = fecha.ToString("yyyy-MM-dd 00:00:00");
                            string fechaFin = fecha.AddDays(1).ToString("yyyy-MM-dd 00:00:00");

                            // Reemplazamos TODA la expresión compleja por un rango válido para MySQL
                            // Nota: Asegúrate de que el nombre de la propiedad sea "Fecha"
                            filtro = $"Fecha >= DateTime(\"{fechaInicio}\") && Fecha < DateTime(\"{fechaFin}\")";
                        }
                    }
                }

                // ✅ Pasar el filtro a EF Core
                query = query.Where(filtro);
            }
            // Contamos el total (después de aplicar el filtro)
            var total = await query.CountAsync();

            // Traemos la página actual
            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return (items, total);
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



        public async Task<decimal> GetAvailableCashAsync()
        {
            await using var db = await _factory.CreateDbContextAsync();

            return await db.Set<Movimientos>()
                .SumAsync(x => x.IdGasto == null ? (x.MontoEfectivo ?? 0) : -(x.MontoEfectivo ?? 0));
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
            Gasto? gasto = null,
            decimal? montoEfectivo = null)
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
                MontoEfectivo = montoEfectivo,
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