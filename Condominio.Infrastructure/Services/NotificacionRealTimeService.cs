using Condominio.Domain.Interfaces;
using Condominio.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Condominio.Infrastructure.Services
{
    public class NotificacionRealTimeService : INotificacionRealTimeService
    {

        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificacionRealTimeService(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        // ✅ Enviar a UN usuario
        public async Task EnviarNotificacionAsync(int userId)
        {
            // Buscar todas las conexiones activas de ese usuario
            var conexiones = NotificationHub.Conexiones
                .Where(c => c.Value == userId.ToString())
                .Select(c => c.Key)
                .ToList();

            Console.WriteLine($"📤 Enviando notificación a userId={userId}. Conexiones activas: {conexiones.Count}");

            foreach (var connectionId in conexiones)
            {
                await _hubContext.Clients
                    .Client(connectionId)
                    .SendAsync("NuevaNotificacion");
            }
        }

        // ✅ Enviar a MÚLTIPLES usuarios
        public async Task EnviarNotificacionMultipleAsync(List<int> userIds)
        {
            if (userIds == null || !userIds.Any())
                return;

            // Buscar todas las conexiones de esos usuarios
            var userIdsStr = userIds.Select(id => id.ToString()).ToHashSet();

            var conexiones = NotificationHub.Conexiones
                .Where(c => userIdsStr.Contains(c.Value))
                .Select(c => c.Key)
                .ToList();

            Console.WriteLine($"📤 Enviando notificación a {userIds.Count} usuarios. Conexiones activas: {conexiones.Count}");

            foreach (var connectionId in conexiones)
            {
                await _hubContext.Clients
                    .Client(connectionId)
                    .SendAsync("NuevaNotificacion");
            }
        }
    }
}
