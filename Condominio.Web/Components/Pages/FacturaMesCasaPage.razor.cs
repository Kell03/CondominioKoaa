using Condominio.Application.Services;
using Condominio.Domain.Entities;
using Condominio.Infrastructure.Repositories;
using Microsoft.AspNetCore.Components;
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

       
        private List<string> MetodosPago = new List<string>
    {
        "Pago Movil"
    };

        private IQueryable<FacturaMesCasa> items;

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
            await CheckAdminRole();

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


        private async Task CheckAdminRole()
        {
            var authState = await AuthProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            isAdmin = user.IsInRole("Administrador");
        }



        private async Task EditUser(FacturaMesCasa item)
        {
            selectedItem = item;
            selectedIndex = 1;
            StateHasChanged();
            await Task.CompletedTask;
        }




        private async Task SaveItem()
        {

                selectedItem.UpdatedAt = DateTime.Now;
            selectedItem.Estado = "En Revisión"; // Cambiar el estado a "En Revisión"
            FacturaMesCasaRepository.Update(selectedItem);
                await FacturaMesCasaRepository.SaveChangesAsync();
                // Si tienes SaveChanges en el repositorio
                await LoadData(); // Recargar la lista
                selectedIndex = 0;
                selectedItem = new FacturaMesCasa();

                StateHasChanged();

           
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
