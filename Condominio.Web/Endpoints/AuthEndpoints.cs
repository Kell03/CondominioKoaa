using Condominio.Application.Services;
using Microsoft.AspNetCore.Authentication;
using System.Net.NetworkInformation;
using System.Security.Claims;

namespace Condominio.Web.Endpoints
{
    public static class AuthEndpoints
    {
  
        public static void MapAuthEndpoints(this WebApplication app)
        {
            // LOGIN
            app.MapPost("/api/loginendpoint", async (
                HttpContext context,
                LoginRequest request,
                AuthService authService,
                AppState appState) =>
            {
                var user = await authService.Login(request.Email, request.Password);

                if (user == null)
                    return Results.Unauthorized();

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                var identity = new ClaimsIdentity(claims, "CookieAuth");
                var principal = new ClaimsPrincipal(identity);

                // ✅ Crea la cookie (esto SÍ funciona desde HTTP)
                await context.SignInAsync("CookieAuth", principal);

                // ✅ Poblar AppState para el request actual
                appState.CurrentUser = user;

                return Results.Ok(new { user.Id, user.Name, user.Email, user.Role });
            });

            // LOGOUT
            app.MapPost("/api/logout", async (HttpContext context, AppState appState) =>
            {
                await context.SignOutAsync("CookieAuth");
                appState.CurrentUser = null;  // ✅ limpiar
                return Results.Ok();
            });
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }
}
