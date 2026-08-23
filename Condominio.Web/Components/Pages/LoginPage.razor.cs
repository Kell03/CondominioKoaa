using Radzen;

namespace Condominio.Web.Components.Pages
{
    public partial  class LoginPage
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
                var user = await AuthService.Login(email, password);

                if (user != null)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Bienvenido",
                        Detail = $"Hola {user.Name}!",
                        Duration = 4000
                    });

                    NavigationManager.NavigateTo("/", true);
                }
                else
                {
                    errorMessage = "Email o contraseña incorrectos";
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
