using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Condominio.Infrastructure.Hubs
{
    public class NotificationHub : Hub
    {

        // ✅ Diccionario estático: ConnectionId → UserId
        public static readonly Dictionary<string, string> Conexiones = new();

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var userId = httpContext?.Request.Query["userId"].ToString();

            Console.WriteLine("═══════════════════════════════════");
            Console.WriteLine($"🔌 ConnectionId: '{Context.ConnectionId}'");
            Console.WriteLine($"🔌 UserId desde query: '{userId}'");

            if (!string.IsNullOrEmpty(userId))
            {
                Conexiones[Context.ConnectionId] = userId;
                Console.WriteLine($"✅ Usuario {userId} registrado");
            }
            else
            {
                Console.WriteLine("⚠️ No hay userId en el query string");
            }

            Console.WriteLine("═══════════════════════════════════");

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Conexiones.Remove(Context.ConnectionId);
            Console.WriteLine($"🔌 Desconectado: {Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
        }
    }

}
