using Condominio.Domain.DB;
using Condominio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Condominio.Infrastructure.Repositories
{
    public class NotificacionRepository : GenericRepository<Notificaciones>
    {
        public NotificacionRepository(IDbContextFactory<AppDbContext> factory) : base(factory) { }

        public async Task<IEnumerable<Notificaciones>> GetByUserAsync(int userId)
        {
            await using var db = await _factory.CreateDbContextAsync();

            return await db.Set<Notificaciones>()
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.FechaCreacion)
                .Take(50)
                .ToListAsync();
        }

        public async Task<int> CountUnreadAsync(int userId)
        {
            await using var db = await _factory.CreateDbContextAsync();

            return await db.Set<Notificaciones>()
                .AsNoTracking()
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task MarcarComoLeidaAsync(int notificacionId)
        {
            await using var db = await _factory.CreateDbContextAsync();

            var notif = await db.Set<Notificaciones>().FindAsync(notificacionId);
            if (notif != null)
            {
                notif.IsRead = true;
                notif.FechaLeida = DateTime.Now;
                db.Set<Notificaciones>().Update(notif);
                await db.SaveChangesAsync();
            }
        }

        public async Task MarcarTodasComoLeidasAsync(int userId)
        {
            await using var db = await _factory.CreateDbContextAsync();

            var notifs = await db.Set<Notificaciones>()
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notif in notifs)
            {
                notif.IsRead = true;
                notif.FechaLeida = DateTime.Now;
            }

            await db.SaveChangesAsync();
        }
    }
}