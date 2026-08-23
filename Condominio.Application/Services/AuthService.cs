using Condominio.Domain.Entities;
using Condominio.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Condominio.Application.Services
{
    public class AuthService
    {

        private readonly UserRepository _userRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly CustomAuthStateProvider _authStateProvider;

        public AuthService(UserRepository userRepository, CustomAuthStateProvider authStateProvider)
        {
            _userRepository = userRepository;
            _authStateProvider = authStateProvider;
        }

        // ✅ LOGIN: Valida y crea la cookie
        public async Task<Users> Login(string email, string password)
        {
            try
            {
                // 1. Validar credenciales
                var user = await _userRepository.LoginAsync(email, password);
                if (user == null)
                    return null;

                // 2. Obtener claims
                var claims = _userRepository.GetUserClaims(user);

                // 3. Crear identidad y principal
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                _authStateProvider.NotifyUserAuthentication(principal);


                return user;

            }
            catch (Exception ex) {

                throw;
            }
        }

        // ✅ LOGOUT: Elimina la cookie
        public async Task Logout()
        {
            try
            {
                _authStateProvider.NotifyUserLogout();
                await Task.CompletedTask;
            } 
            catch(Exception ex) {
                throw;
            }
        }

        // ✅ OBTENER USUARIO ACTUAL DESDE CLAIMS
        public Users GetCurrentUser()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity.IsAuthenticated)
                return null;

            return new Users
            {
                Id = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0"),
                Name = user.FindFirst(ClaimTypes.Name)?.Value,
                Email = user.FindFirst(ClaimTypes.Email)?.Value,
                Role = user.FindFirst(ClaimTypes.Role)?.Value
            };
        }

        // ✅ VERIFICAR SI ESTÁ AUTENTICADO
        public bool IsAuthenticated()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
        }

        // ✅ OBTENER ROL DEL USUARIO
        public string GetUserRole()
        {
            return _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.Role)?.Value ?? "Invitado";
        }
    }
}

