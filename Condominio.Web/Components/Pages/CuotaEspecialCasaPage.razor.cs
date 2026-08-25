using Condominio.Application.Services;
using Condominio.Domain.Entities;
using Condominio.Infrastructure.Repositories;
using Radzen;

namespace Condominio.Web.Components.Pages
{
    public partial class CuotaEspecialCasaPage
    {

        IList<CuotaEspecialCasa> selectedEmployees;
        CuotaEspecialCasa selectedItem = new CuotaEspecialCasa();
        private List<int> years = new List<int>();

        private int selectedMonth = DateTime.Now.Month;
        private int selectedYear = DateTime.Now.Year;
        private bool isAdmin = false;

        IEnumerable<CuotaEspecialCasa> Cuotas;

        private IQueryable<CuotaEspecialCasa> items;


        private List<string> MetodosPago = new List<string>
    {
        "Pago Movil"
    };
        protected override async Task OnInitializedAsync()
        {
            await LoadData();

            for (int i = 2020; i <= 2030; i++)
            {
                years.Add(i);
            }

            Cuotas = await CuotaEspecialCasaRepository.GetAllAsync();
            await CheckAdminRole();
        }

        private async Task LoadData()
        {
            try
            {

                var role = AppState.CurrentUser.Role;
                IEnumerable<CuotaEspecialCasa> itemList;

                if (role == "Administrador")
                {
                    itemList = await CuotaEspecialCasaRepository.GetAllAsync();
                }
                else
                {
                    itemList = await CuotaEspecialCasaRepository.GetAllForUserAsync(AppState.CurrentUser.Id);
                }

                items = itemList.AsQueryable();

                selectedEmployees = itemList.Any()
                    ? new List<CuotaEspecialCasa> { itemList.First() }
                    : new List<CuotaEspecialCasa>();

                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al cargar datos: {ex.Message}");
                // Opcional: mostrar notificación al usuario
            }
        }

        private async Task CheckAdminRole()
        {
            var authState = await AuthProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            isAdmin = user.IsInRole("Administrador");
        }
        private async Task SaveItem()
        {

            selectedItem.UpdatedAt = DateTime.Now;
            selectedItem.Estado = "En Revisión"; // Cambiar el estado a "En Revisión"
            CuotaEspecialCasaRepository.Update(selectedItem);
            await CuotaEspecialCasaRepository.SaveChangesAsync();
            // Si tienes SaveChanges en el repositorio
            await LoadData(); // Recargar la lista
            selectedIndex = 0;
            selectedItem = new CuotaEspecialCasa();

            StateHasChanged();
        
        }




       



        private async Task EditUser(CuotaEspecialCasa item)
        {
            selectedItem = item;
            selectedIndex = 1;
            StateHasChanged();
            await Task.CompletedTask;
        }



        private async Task AddUser()
        {
            selectedItem = new CuotaEspecialCasa();
            selectedIndex = 1;
            StateHasChanged();
            await Task.CompletedTask;
        }


        private async Task ConfirmSend(CuotaEspecialCasa item)
        {
            var result = await DialogService.Confirm(
                $"¿Confirmar pago de {item.User.Name} para el" +  
            $" {item.CuotaEspecial.NombreMes} {item.CuotaEspecial.Year}?\n\n" +
                $"Se le notificará al propietario que su pago ha sido aprobado.\n\n" +
                $"¿Deseas continuar?",
                "Confirmar aprobación de pago",
                new ConfirmOptions()
                {
                    OkButtonText = " Sí, aprobar pago",
                    CancelButtonText = " Cancelar",
                }
            );

            if (result == true)
            {
                await ConfirmarPago(item);
            }
        }

        private async Task ConfirmarPago(CuotaEspecialCasa item)
        {
            if (item == null) return;



            // 2. Guardar en la base de datos
            await CuotaEspecialCasaRepository.ConfirmarPagoCuotaCasa(item);
            await CuotaEspecialCasaRepository.SaveChangesAsync();

            // 3. Recargar la lista para actualizar la UI
            await LoadData();
            StateHasChanged();
        }



    }
}
