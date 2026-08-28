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
        private IEnumerable<CuotaEspecial> CuotasEspeciales;
        private IEnumerable<Houses> HouseList;

        private int selectedMonth = DateTime.Now.Month;
        private int selectedYear = DateTime.Now.Year;
        private bool isAdmin = false;
        private ApiMoneda monedaData;

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
            await CargarMoneda();
        }

        private async Task CargarMoneda()
        {
            monedaData = await MonedaApiService.GetMonedaAsync();
            StateHasChanged();
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

        private async Task CreateNewItem()
        {
            await LoadCuotas();
            await LoadHouses();
            selectedItem = new CuotaEspecialCasa();
            selectedIndex = 2;
            StateHasChanged();
            await Task.CompletedTask;
        }


        private async Task LoadCuotas()
        {
            CuotasEspeciales = await CuotaEspecialRepository.GetAllAsync();
        }


        private async Task LoadHouses()
        {
            HouseList = await HousesRepository.GetAllAsync();
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
            selectedItem.Estado = "En Revision"; // Cambiar el estado a "En Revisión"
                                                 // Limpiar y convertir a decimal
       
            CuotaEspecialCasaRepository.Update(selectedItem);
            await CuotaEspecialCasaRepository.SaveChangesAsync();
            // Si tienes SaveChanges en el repositorio
            await LoadData(); // Recargar la lista
            selectedIndex = 0;
            selectedItem = new CuotaEspecialCasa();

            StateHasChanged();

        }

        private async Task SaveNewItem()
        {
            try
            {
                // ✅ 1. GUARDAR
                selectedItem.Estado = "Pendiente";
                await CuotaEspecialCasaRepository.AddAsync(selectedItem);
                await CuotaEspecialCasaRepository.SaveChangesAsync();

                // ✅ 2. RECARGAR DATOS
                await LoadData();

                // ✅ 3. CAMBIAR DE TAB Y LIMPIAR
                selectedIndex = 0;
                selectedItem = new CuotaEspecialCasa();

                // ✅ 4. FORZAR ACTUALIZACIÓN DE LA UI
                StateHasChanged();

                // ✅ 5. OPCIONAL: PEQUEÑO DELAY PARA ASEGURAR
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al guardar: {ex.Message}");
            }
        }



        private async Task EditUser(CuotaEspecialCasa item)
        {
            if (!isAdmin && item.Estado != "Pendiente")
            {
                return;
            }


            if(item.MontoBs == null || item.MontoBs == 0)
            {
                item.MontoBs = monedaData != null
                ? item.Monto * (decimal)Math.Round(monedaData.Promedio, 2)
                : item.Monto;
            }
            
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


        private async Task ConfirmSend(CuotaEspecialCasa item, bool confirmar)
        {
            if (confirmar == true)
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
            else
            {
                var result = await DialogService.Confirm(
              $"¿Rechazar pago de {item.User.Name} para el" +
          $" {item.CuotaEspecial.NombreMes} {item.CuotaEspecial.Year}?\n\n" +
              $"Se le notificará al propietario que su pago ha sido pasado a pendiente nuevamente.\n\n" +
              $"¿Deseas continuar?",
              "Confirmar Rechazo de pago",
              new ConfirmOptions()
              {
                  OkButtonText = " Sí, rechazar pago",
                  CancelButtonText = " Cancelar",
              }
          );


                if (result == true)
                {
                    item.Estado = "Pendiente";
                    CuotaEspecialCasaRepository.Update(item);
                    await CuotaEspecialCasaRepository.SaveChangesAsync();
                    // Si tienes SaveChanges en el repositorio
                    await LoadData(); // Recargar la lista

                }

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
