using Condominio.Domain.Entities;
using Condominio.Web.Components.Dialogs;
using Radzen;
using Radzen.Blazor;

namespace Condominio.Web.Components.SubPages
{
    public partial class ConfirmacionMultipleCuota
    {


        private RadzenDataGrid<Users> grid;
        private List<Users> users = new();
        private IList<Users> selectedEmployees = new List<Users>();
        private bool allowRowSelectOnRowClick = true;
        private bool isAdmin = false;
        private int anioSeleccionado = DateTime.Now.Year;
        private List<int> monthsSelected = new List<int>();
        List<Payments> paymentsList = new List<Payments>();
        private List<string> roles = new List<string>
    {
        "Administrador",
        "Usuario",
        "Encargado"
    };

        private Dictionary<bool, string> estados = new Dictionary<bool, string>
    {
        { true, "Activo" },
        { false, "Inactivo" }
    };




        IEnumerable<Houses> House;


        protected override async Task OnInitializedAsync()
        {

            //await CheckAdminRole();
            //
            await LoadUsers();

            //House = await HouseRepository.GetAllAsync();

        }

        private async Task LoadUsers()
        {
            // Usas el método específico si existe
            var user = AppState.CurrentUser.Role;

            var userList = await UserRepository.GetAllAsync();
            userList = userList.Where(x => x.HouseId != null).ToList();


            // 3. Traes los pagos
            paymentsList = await PaymentRepository.GetAllPaymentCuotasAsync();

            // 4. Filtro de pagos (asegurando que FacturaMesCasa no sea null)
            var payments = paymentsList
                .Where(x => x.CuotaEspecialCasa != null && x.Estado == "Pendiente")
                .Select(x => x.CuotaEspecialCasa.HouseId)
                .Distinct()
                .ToList();

            // 5. Filtro de usuarios seguros
            users = userList.Where(x => payments.Contains(x.HouseId ?? 0)).ToList();
            // users = userList.ToList();

        }



        private async Task CheckAdminRole()
        {

            var role = AppState.CurrentUser.Role;
            if (role == "Administrador")
            {
                isAdmin = true;
            }
            else
            {
                isAdmin = false;
            }
        }







        private async Task DeleteUser(Users user)
        {
            UserRepository.Delete(user);
            await UserRepository.SaveChangesAsync();
            // Si tienes SaveChanges en el repositorio
            await LoadUsers(); // Recargar la lista
            StateHasChanged();

        }






        private async Task AbrirDialogo()
        {
            if (selectedEmployees == null || !selectedEmployees.Any()) return;

            // Abrimos el nuevo diálogo
            var resultado = await DialogService.OpenAsync<DialogoConfirmacionFacturaMulti>(
                "Confirmar Creación",
                null,
                new DialogOptions { Width = "400px" });

            // Si el usuario le dio Confirmar y el resultado no es nulo...
            if (resultado is DialogoConfirmacionFacturaMulti.FacturaParams parametros)
            {
                anioSeleccionado = parametros.Anio;
                monthsSelected = parametros.MesesSeleccionados.ToList();


                var casasSeleccionadas = selectedEmployees
       .Where(x => x.HouseId.HasValue)
       .Select(x => x.HouseId.Value)
       .ToList();

                // ✅ 2. Hacemos el filtro de pagos de forma limpia y segura
                List<Payments> paymentsForConfirmationList = paymentsList
                    .Where(x =>
                        x.CuotaEspecialCasa != null && // Validar que no sea null
                        casasSeleccionadas.Contains(x.CuotaEspecialCasa.HouseId) &&
                        x.CuotaEspecialCasa.CuotaEspecial != null &&
                        x.CuotaEspecialCasa.CuotaEspecial.Year == anioSeleccionado &&
                        monthsSelected.Contains(x.CuotaEspecialCasa.CuotaEspecial.Mes)
                    )
                    .ToList();

                // (Opcional) Verificar si encontró algo
                if (!paymentsForConfirmationList.Any())
                {
                    NotificationService.Notify(NotificationSeverity.Warning, "Sin resultados", "No se encontraron pagos para los usuarios y meses seleccionados.");
                    return;
                }

                bool exito = await CuotaCasaRepository.MasiveConfirmPayment(paymentsForConfirmationList);

                if (exito)
                {
                    // Notificación de éxito
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Pagos Confirmados",
                        Detail = $"Se han confirmado {paymentsForConfirmationList.Count} pagos correctamente.",
                        Duration = 3000 // 3 segundos
                    });
                }
                else
                {
                    // Notificación de error
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Error al confirmar",
                        Detail = "No se pudieron confirmar los pagos. Intente de nuevo.",
                        Duration = 5000 // 5 segundos para que el usuario pueda leerlo
                    });
                }
                await LoadUsers();
                selectedEmployees.Clear();
                anioSeleccionado = DateTime.Now.Year;
                monthsSelected.Clear();

                StateHasChanged();                // Aquí puedes hacer lo que necesites con el año y los meses.
                // Ejemplo: Console.WriteLine($"Año: {anioSeleccionado}, Meses: {string.Join(",", mesesSeleccionados)}");

                // await HacerMagiaConLosDatos(anioSeleccionado, mesesSeleccionados, selectedEmployees);
            }
        }


    }
}
