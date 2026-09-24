using Condominio.Domain.DB;
using Condominio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Condominio.Infrastructure.Repositories
{
    public class NotificacionRepository : GenericRepository<Notificaciones>
    {

        public NotificacionRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<Notificaciones>> GetByUserAsync(int userId)
        {
            try
            {
                return await _dbSet
               .AsNoTracking()
               .Where(n => n.UserId == userId)
               .OrderByDescending(n => n.FechaCreacion)
               .Take(50)  // Últimas 50
               .ToListAsync();

            }
            catch(Exception ex)
            {
                throw;
            }
           
        }

        // ✅ CONTAR NO LEÍDAS
        public async Task<int> CountUnreadAsync(int userId)
        {

            try
            {

                return await _dbSet
                .AsNoTracking()
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            }
            catch(Exception ex)
            {
                throw;
            }
            
        }

        // ✅ MARCAR COMO LEÍDA
        public async Task MarcarComoLeidaAsync(int notificacionId)
        {
            var notif = await _dbSet.FindAsync(notificacionId);
            if (notif != null)
            {
                notif.IsRead = true;
                notif.FechaLeida = DateTime.Now;
                _dbSet.Update(notif);
                await _context.SaveChangesAsync();
            }
        }

        // ✅ MARCAR TODAS COMO LEÍDAS
        public async Task MarcarTodasComoLeidasAsync(int userId)
        {
            var notifs = await _dbSet
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notif in notifs)
            {
                notif.IsRead = true;
                notif.FechaLeida = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

    }
}
