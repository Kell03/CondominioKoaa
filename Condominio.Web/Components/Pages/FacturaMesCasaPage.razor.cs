using Condominio.Application.Services;
using Condominio.Domain.Entities;
using Condominio.Infrastructure.Repositories;
using Microsoft.AspNetCore.Components;
using System.Globalization;
using Radzen;
using Radzen.Blazor;

namespace Condominio.Web.Components.Pages
{
    public partial class FacturaMesCasaPage
    {

        RadzenDataGrid<FacturaMesCasa> grid;
        IList<FacturaMesCasa> selectedEmployees;
        FacturaMesCasa selectedItem = new FacturaMesCasa();
        private bool isAdmin = false;
        private ApiMoneda monedaData;
        private IEnumerable<FacturaMes> FacturasMes;
        private IEnumerable<Houses> HouseList;
        Payments Pago = new Payments();
        private IEnumerable<Payments> PagosList;
        private List<string> MetodosPago = new List<string>
        {
        "Pago Movil"
        };
        private IQueryable<FacturaMesCasa> items;





        protected override async Task OnInitializedAsync()
        {
            await LoadData();
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
                IEnumerable<FacturaMesCasa> itemList;

                if (role == "Administrador")
                {
                    itemList = await FacturaMesCasaRepository.GetAllAsync();
                }
                else
                {
                    itemList = await FacturaMesCasaRepository.GetAllForUserAsync(AppState.CurrentUser.Id);
                }

                items = itemList.AsQueryable();

                selectedEmployees = itemList.Any()
                    ? new List<FacturaMesCasa> { itemList.First() }
                    : new List<FacturaMesCasa>();

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
            await LoadFacturas();
            await LoadHouses();
            selectedItem = new FacturaMesCasa();
            selectedIndex = 2;
            StateHasChanged();
            await Task.CompletedTask;
        }


        private async Task LoadFacturas()
        {
            FacturasMes = await FacturaMesRepository.GetAllAsync();
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



        private async Task EditUser(FacturaMesCasa item)
        {
            Pago = new Payments();
            await LoadPagos();

            if (item.MontoBs == null || item.MontoBs == 0)
            {
                item.MontoBs = monedaData != null
                ? item.MontoTotal * (decimal)Math.Round(monedaData.Promedio, 2)
                : item.MontoTotal;
            }
            selectedItem = item;
            selectedIndex = 1;
            StateHasChanged();
            await Task.CompletedTask;
        }




        private async Task SaveItem()
        {

            selectedItem.UpdatedAt = DateTime.Now;
            selectedItem.Estado = "En Revision"; // Cambiar el estado a "En Revisión"
                                                 // Limpiar y convertir a decimal

            FacturaMesCasaRepository.Update(selectedItem);
            await FacturaMesCasaRepository.SaveChangesAsync();
            // Si tienes SaveChanges en el repositorio
            await LoadData(); // Recargar la lista
            selectedIndex = 0;
            selectedItem = new FacturaMesCasa();

            StateHasChanged();


        }


        #region Pagos 

        private async Task SavePago()
        {

            Pago.FacturaMesCasaId = selectedItem.Id;
            await FacturaMesCasaRepository.RegistrarPagoFactura(Pago);
            await FacturaMesCasaRepository.SaveChangesAsync();
            // Si tienes SaveChanges en el repositorio
            await LoadPagos(); // Recargar la lista
            Pago = new Payments();

            StateHasChanged();


        }

        private async Task LoadPagos()
        {
            PagosList = await FacturaMesCasaRepository.GetPaymentsForUserAsync(AppState.CurrentUser.Id);
        }


        #endregion


        private async Task SaveNewItem()
        {
            try
            {
                // ✅ 1. GUARDAR
                selectedItem.Estado = "Pendiente";
                await FacturaMesCasaRepository.AddAsync(selectedItem);
                await FacturaMesCasaRepository.SaveChangesAsync();

                // ✅ 2. RECARGAR DATOS
                await LoadData();

                // ✅ 3. CAMBIAR DE TAB Y LIMPIAR
                selectedIndex = 0;
                selectedItem = new FacturaMesCasa();

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


        private string ObtenerNombreMes(int? mes)
        {
            if (mes == null)
            {
                return "";
            }
            else
            {
                string[] meses = { "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
                           "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" };
                return meses[(int)mes - 1];
            }
        }




        private async Task ConfirmSend(FacturaMesCasa item, bool confirmar)
        {
            if (confirmar == true)
            {
                var result = await DialogService.Confirm(
                $"¿Confirmar pago de {ObtenerNombreMes(item.FacturaMes.Mes)} {item.FacturaMes.Year}?\n\n" +
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
          $"{ObtenerNombreMes(item.FacturaMes.Mes)} {item.FacturaMes.Year}?\n\n" +
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
                    FacturaMesCasaRepository.Update(item);
                    await FacturaMesCasaRepository.SaveChangesAsync();
                    // Si tienes SaveChanges en el repositorio
                    await LoadData(); // Recargar la lista

                }

            }


        }

        private async Task ConfirmarPago(FacturaMesCasa facturaCasa)
        {
            if (facturaCasa == null) return;



            // 2. Guardar en la base de datos
            await FacturaMesCasaRepository.ConfirmarPagoFacturaCasa(facturaCasa);
            await FacturaMesCasaRepository.SaveChangesAsync();

            // 3. Recargar la lista para actualizar la UI
            await LoadData();
            StateHasChanged();
        }


        //nuevo grid



        void RowRender(RowRenderEventArgs<FacturaMesCasa> args)
        {
            args.Expandable = (args.Data.FacturaMes.FacturaMesHijos != null && args.Data.FacturaMes.FacturaMesHijos.Any());
        }

        void RowExpand(FacturaMesCasa order)
        {
            if (order.FacturaMes.FacturaMesHijos == null)
            {
                order.FacturaMes.FacturaMesHijos = order.FacturaMes.FacturaMesHijos.ToList();
            }
        }

    }
}
