using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Condominio.Application.Services
{
    public class CustomAuthStateProvider : AuthenticationStateProvider  // ✅ CLASE BASE CORRECTA
    {
        private ClaimsPrincipal _currentUser = new ClaimsPrincipal(new ClaimsIdentity());

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            return Task.FromResult(new AuthenticationState(_currentUser));
        }

        // ✅ NOTIFICAR QUE EL USUARIO INICIÓ SESIÓN
        public void NotifyUserAuthentication(ClaimsPrincipal user)
        {
            _currentUser = user;
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }

        // ✅ NOTIFICAR QUE EL USUARIO CERRÓ SESIÓN
        public void NotifyUserLogout()
        {
            _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
        }

        // ✅ OBTENER EL USUARIO ACTUAL
        public ClaimsPrincipal GetCurrentUser()
        {
            return _currentUser;
        }
    }

}
