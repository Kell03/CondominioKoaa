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
    public class FacturaMesRepository : GenericRepository<FacturaMes>
    {
        private readonly NotificacionRepository _notifRepo;
        private readonly INotificacionRealTimeService _notificacionService;

        public FacturaMesRepository(
            IDbContextFactory<AppDbContext> factory,
            NotificacionRepository notifRepo,
            INotificacionRealTimeService notificacionService) : base(factory)
        {
            _notifRepo = notifRepo;
            _notificacionService = notificacionService;
        }



        public async Task<(List<FacturaMes> Items, int Total)> GetPagedAsync(
      int skip, int take, string filtro = null, string orderBy = null)
        {
            await using var db = await _factory.CreateDbContextAsync();

            var query = db.Set<FacturaMes>()
                .Include(x => x.FacturaMesHijos)
                    .ThenInclude(x => x.CategoriaGasto)
                        .ThenInclude(x => x.Proveedor)
                .AsNoTracking()
                .AsQueryable();

            query = query.Where(x => x.Motivo == null || x.Motivo == "");

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                // Extraemos el valor limpio
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

                // ✅ BUSCAMOS LOS MESES QUE COINCIDEN
                var mesesCoincidentes = ObtenerMesesCoincidentes(f);

                query = query.Where(x =>
                    x.Year.ToString().Contains(f) ||
                    (x.Motivo != null && x.Motivo.ToLower().Contains(f)) ||
                    x.Mes.ToString().Contains(f) ||
                    (mesesCoincidentes.Any() && mesesCoincidentes.Contains(x.Mes)) // 👈 La magia aquí
                );
            }

            var total = await query.CountAsync();

            // ... (el resto del código de orden y paginación se queda igual)

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return (items, total);
        }

        private List<int> ObtenerMesesCoincidentes(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return new List<int>();

            var meses = new Dictionary<int, string>
    {
        { 1, "enero" }, { 2, "febrero" }, { 3, "marzo" }, { 4, "abril" },
        { 5, "mayo" }, { 6, "junio" }, { 7, "julio" }, { 8, "agosto" },
        { 9, "septiembre" }, { 10, "octubre" }, { 11, "noviembre" }, { 12, "diciembre" }
    };

            var textoLower = texto.ToLower();

            // Buscamos todos los meses que contengan el texto (ej: "o" -> [1, 2, 5, 6, 7, 8, 10, 11])
            var coincidentes = meses
                .Where(m => m.Value.Contains(textoLower))
                .Select(m => m.Key)
                .ToList();

            return coincidentes;
        }

        public override async Task<IEnumerable<FacturaMes>> GetAllAsync()
        {
            await using var db = await _factory.CreateDbContextAsync();

            return await db.Set<FacturaMes>()
                .Include(x => x.FacturaMesHijos).ThenInclude(x => x.CategoriaGasto).ThenInclude(x => x.Proveedor)
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<FacturaMes>> GetAllForGastosAsync()
        {
            await using var db = await _factory.CreateDbContextAsync();

            return await db.Set<FacturaMes>()
                .Include(x => x.FacturaMesHijos).ThenInclude(x => x.CategoriaGasto).ThenInclude(x => x.Proveedor)
                .AsNoTracking()
                .Where(x => x.Motivo == null || x.Motivo == "")
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task DeleteWithHijos(FacturaMes entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            await using var db = await _factory.CreateDbContextAsync();
            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                var factura = await db.FacturaMes
                    .Include(f => f.FacturaMesHijos)
                    .FirstOrDefaultAsync(f => f.Id == entity.Id);

                if (factura == null)
                    throw new Exception("Factura no encontrada");

                if (factura.FacturaMesHijos != null && factura.FacturaMesHijos.Any())
                {
                    db.FacturaMesHijo.RemoveRange(factura.FacturaMesHijos);
                }

                db.FacturaMes.Remove(factura);

                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error al eliminar: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> SaveWithFacturaHijo(FacturaMes item, List<FacturaMesHijo> children)
        {
            await using var db = await _factory.CreateDbContextAsync();
            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                // 1. Guardamos la factura para obtener el ID
                db.FacturaMes.Add(item);
                await db.SaveChangesAsync();

                var categoriasPorcentaje = db.CategoriasGasto.Where(x => x.Porcentaje == true).ToList();

                // 2. Calculamos la base (solo los gastos fijos)
                decimal montoBase = children
     .Where(x => x.IdCategoria == null || !categoriasPorcentaje.Select(x => x.Id).Contains(x.IdCategoria.Value))
     .Sum(c => (categoriasPorcentaje.FirstOrDefault(x => x.Id == c.IdCategoria)?.EsResta == true) ? -(decimal)c.Monto : (decimal)c.Monto);

                decimal totalFactura = montoBase;

                // 3. Calculamos los gastos de porcentaje
                foreach (var child in children.Where(x => x.IdCategoria != null && categoriasPorcentaje.Select(x => x.Id).Contains(x.IdCategoria.Value)))
                {
                    // Calculamos el monto del porcentaje sobre la base
                    var monto = categoriasPorcentaje.Where(x => x.Id == child.IdCategoria).Select(x => x.Monto).FirstOrDefault();
                    decimal montoCalculado = montoBase * ((decimal)monto / 100.0m);
                    // ✅ AGREGAR ESTA LÍNEA (Si la categoría es EsResta, el porcentaje se vuelve negativo)
                    if (categoriasPorcentaje.FirstOrDefault(x => x.Id == child.IdCategoria)?.EsResta == true) montoCalculado = -montoCalculado;



                    // Asignamos el monto al hijo
                    child.Monto = (double)montoCalculado;

                    // Sumamos al total general
                    totalFactura += montoCalculado;
                }

                // 4. Asignamos el ID a todos los hijos
                foreach (var child in children)
                {
                    child.FacturaMesId = item.Id;
                }

                // 5. Insertamos los hijos
                db.FacturaMesHijo.AddRange(children);
                await db.SaveChangesAsync();

                // 6. Actualizamos el monto total de la factura maestra
                item.MontoTotal = (double)totalFactura;
                db.FacturaMes.Update(item);
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

        public async Task<bool> SaveListWithFacturaHijo(FacturaMes itemOriginal, List<FacturaMesHijo> childrenOriginal, List<int> houseIds)
        {
            var listIdUsers = new List<int>();
            int guardadasExitosamente = 0;

            foreach (var idcasa in houseIds)
            {
                await using var db = await _factory.CreateDbContextAsync();
                await using var transaction = await db.Database.BeginTransactionAsync();

                try
                {
                    var item = new FacturaMes
                    {
                        Mes = itemOriginal.Mes,
                        Year = itemOriginal.Year,
                        Motivo = itemOriginal.Motivo,
                        IsActive = itemOriginal.IsActive,
                        EstadoPago = itemOriginal.EstadoPago
                    };

                    db.FacturaMes.Add(item);
                    await db.SaveChangesAsync();

                    var children = new List<FacturaMesHijo>();
                    foreach (var child in childrenOriginal)
                    {
                        children.Add(new FacturaMesHijo
                        {
                            FacturaMesId = item.Id,
                            Motivo = child.Motivo,
                            Monto = child.Monto,
                            IdCategoria = child.IdCategoria,
                            IdProveedor = child.IdProveedor,
                            Pagado = false
                        });
                    }

                    db.FacturaMesHijo.AddRange(children);
                    await db.SaveChangesAsync();

                    item.MontoTotal = children.Sum(c => c.Monto);
                    db.FacturaMes.Update(item);
                    await db.SaveChangesAsync();

                    var facturaCasa = new FacturaMesCasa
                    {
                        FacturaMesId = item.Id,
                        HouseId = idcasa,
                        MontoTotal = (decimal)item.MontoTotal,
                        SaldoPendiente = (decimal)item.MontoTotal,
                        MontoPagado = 0,
                        Estado = "Pendiente"
                    };

                    db.FacturaMesCasa.Add(facturaCasa);

                    var usuario = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.HouseId == idcasa);
                    if (usuario != null)
                    {
                        await db.Notificaciones.AddAsync(new Notificaciones
                        {
                            UserId = usuario.Id,
                            Titulo = "Nueva factura",
                            Mensaje = $"Factura de {item.Motivo} - {item.Year} disponible por ${item.MontoTotal:N2}",
                            Tipo = "Factura",
                            ReferenciaId = item.Id
                        });
                        listIdUsers.Add(usuario.Id);
                    }

                    await db.SaveChangesAsync();
                    await transaction.CommitAsync();
                    guardadasExitosamente++;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"Error al guardar factura para la casa {idcasa}: {ex.Message}");
                }
            }

            if (listIdUsers.Any())
            {
                await _notificacionService.EnviarNotificacionMultipleAsync(listIdUsers);
            }

            return guardadasExitosamente > 0;
        }

        public async Task DistribuirFacturaEntreCasas(FacturaMes entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            await using var db = await _factory.CreateDbContextAsync();
            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                var factura = await db.FacturaMes.FirstOrDefaultAsync(f => f.Id == entity.Id);

                if (factura == null)
                    throw new Exception("Factura no encontrada");

                if (factura.MontoTotal == 0)
                    throw new Exception("El monto total de la factura es 0");

                var casasIds = await db.Houses
                    .Where(h => h.IsActive)
                    .Select(h => h.Id)
                    .ToListAsync();

                int totalCasas = casasIds.Count;

                if (totalCasas == 0)
                    throw new Exception("No hay casas activas");

                decimal montoPorCasa = decimal.Round((decimal)factura.MontoTotal / totalCasas, 2);

                var facturasCasas = new List<FacturaMesCasa>(totalCasas);
                var listIdUsers = new List<int>();

                foreach (var houseId in casasIds)
                {
                    facturasCasas.Add(new FacturaMesCasa
                    {
                        FacturaMesId = factura.Id,
                        HouseId = houseId,
                        MontoTotal = montoPorCasa,
                        SaldoPendiente = montoPorCasa,
                        Estado = "Pendiente"
                    });

                    var usuario = await db.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.HouseId == houseId);

                    if (usuario != null)
                    {
                        await db.Notificaciones.AddAsync(new Notificaciones
                        {
                            UserId = usuario.Id,
                            Titulo = "Nueva factura",
                            Mensaje = $"Factura de {factura.NombreMes} {factura.Year} disponible por ${montoPorCasa:N2}",
                            Tipo = "Factura",
                            ReferenciaId = factura.Id
                        });
                        listIdUsers.Add(usuario.Id);
                    }
                }

                await db.FacturaMesCasa.AddRangeAsync(facturasCasas);

                factura.Enviado = true;
                db.FacturaMes.Update(factura);

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                if (listIdUsers.Any())
                {
                    await _notificacionService.EnviarNotificacionMultipleAsync(listIdUsers);
                }

                Console.WriteLine($"✅ Factura {factura.Mes}/{factura.Year} distribuida entre {totalCasas} casas");
                Console.WriteLine($"📊 Cada casa paga: ${montoPorCasa}");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"❌ Error al distribuir factura: {ex.Message}");
                throw;
            }
        }
    }
}