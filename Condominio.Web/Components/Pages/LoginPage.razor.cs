using Condominio.Application.Services;
using Condominio.Domain.Entities;
using Radzen;
using System.Security.Claims;

namespace Condominio.Web.Components.Pages
{
    public partial class LoginPage
    {

        private string email = "";
        private string password = "";
        private string errorMessage = "";
        private bool isLoading = false;

        private async Task Login()
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                errorMessage = "Por favor ingresa email y contraseña";
                return;
            }

            isLoading = true;
            errorMessage = "";

            try
            {


                //NavigationManager.NavigateTo("/");


                var response = await Http.PostAsJsonAsync("/api/loginendpoint", new { Email = email, Password = password });
                if (response.IsSuccessStatusCode)
                {
                    var userData = await response.Content.ReadFromJsonAsync<Users>();
                    AppState.CurrentUser = new Users
                    {
                        Id = userData.Id,
                        Name = userData.Name,
                        Email = userData.Email,
                        Role = userData.Role
                    };


                    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, userData.Id.ToString()),
        new Claim(ClaimTypes.Name, userData.Name),
        new Claim(ClaimTypes.Email, userData.Email),
        new Claim(ClaimTypes.Role, userData.Role)
    };

                    var identity = new ClaimsIdentity(claims, "CookieAuth");
                    var principal = new ClaimsPrincipal(identity);


                    if (AuthProvider is CustomAuthStateProvider custom)
                    {
                        custom.NotifyUserAuthentication(principal);
                    }
                    NavigationManager.NavigateTo("/");

                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error: {ex.Message}";
            }
            finally
            {
                isLoading = false;
            }
        }
    }
}
