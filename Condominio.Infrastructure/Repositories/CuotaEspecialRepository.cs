using Condominio.Domain.DB;
using Condominio.Domain.Entities;
using Condominio.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Condominio.Infrastructure.Repositories
{
    public class CuotaEspecialRepository : GenericRepository<CuotaEspecial>
    {
        private readonly NotificacionRepository _notifRepo;
        private readonly INotificacionRealTimeService _notificacionService;

        public CuotaEspecialRepository(
            IDbContextFactory<AppDbContext> factory,
            NotificacionRepository notifRepo,
            INotificacionRealTimeService notificacionService) : base(factory)
        {
            _notifRepo = notifRepo;
            _notificacionService = notificacionService;
        }

        public override async Task<IEnumerable<CuotaEspecial>> GetAllAsync()
        {
            await using var db = await _factory.CreateDbContextAsync();

            return await db.Set<CuotaEspecial>()
                .Include(x => x.CuotaEspecialCasas)
                .Include(x => x.Proveedor)
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task DeleteWithHijos(CuotaEspecial entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            await using var db = await _factory.CreateDbContextAsync();
            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                var cuota = await db.CuotaEspecial
                    .Include(c => c.CuotaEspecialCasas)
                    .FirstOrDefaultAsync(c => c.Id == entity.Id);

                if (cuota == null)
                    throw new Exception("Cuota especial no encontrada");

                if (cuota.CuotaEspecialCasas != null && cuota.CuotaEspecialCasas.Any())
                {
                    db.CuotaEspecialCasa.RemoveRange(cuota.CuotaEspecialCasas);
                }

                db.CuotaEspecial.Remove(cuota);

                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($" Error al eliminar cuota especial: {ex.Message}");
                throw;
            }
        }

        public async Task DistribuirCuotaEspecialEntreCasas(CuotaEspecial entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            await using var db = await _factory.CreateDbContextAsync();
            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                var cuota = await db.CuotaEspecial
                    .FirstOrDefaultAsync(c => c.Id == entity.Id);

                if (cuota == null)
                    throw new Exception("Cuota especial no encontrada");

                if (cuota.MontoTotal == 0)
                    throw new Exception("El monto total de la cuota es 0");

                var casasIds = await db.Houses
                    .Where(h => h.IsActive)
                    .Select(h => h.Id)
                    .ToListAsync();

                int totalCasas = casasIds.Count;

                if (totalCasas == 0)
                    throw new Exception("No hay casas activas");

                decimal montoPorCasa = decimal.Round((decimal)cuota.MontoTotal / totalCasas, 2);

                var cuotasCasas = new List<CuotaEspecialCasa>(totalCasas);
                var listIdUsers = new List<int>();

                foreach (var houseId in casasIds)
                {
                    cuotasCasas.Add(new CuotaEspecialCasa
                    {
                        CuotaEspecialId = cuota.Id,
                        CuotaEspecial = cuota,
                        HouseId = houseId,
                        MontoTotal = montoPorCasa,
                        MontoPagado = 0,
                        SaldoPendiente = montoPorCasa,
                        Estado = EstadoCuotaCasa.Pendiente,
                    });

                    var usuario = await db.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.HouseId == houseId);

                    if (usuario != null)
                    {
                        await db.Notificaciones.AddAsync(new Notificaciones
                        {
                            UserId = usuario.Id,
                            Titulo = "Nueva Cuota Especial",
                            Mensaje = $"Cuota Especial de {cuota.NombreMes} {cuota.Year} disponible por ${montoPorCasa:N2}",
                            Tipo = "Cuota Especial",
                            ReferenciaId = cuota.Id
                        });
                        listIdUsers.Add(usuario.Id);
                    }
                }

                await db.CuotaEspecialCasa.AddRangeAsync(cuotasCasas);

                cuota.Enviado = true;
                cuota.Motivo = entity.Motivo;
                db.CuotaEspecial.Update(cuota);

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                if (listIdUsers.Any())
                {
                    await _notificacionService.EnviarNotificacionMultipleAsync(listIdUsers);
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($" Error al distribuir cuota especial: {ex.Message}");
                throw;
            }
        }
    }
}