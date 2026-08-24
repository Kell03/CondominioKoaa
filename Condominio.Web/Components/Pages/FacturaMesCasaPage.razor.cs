using Condominio.Domain.Entities;
using Condominio.Infrastructure.Repositories;
using Radzen;
using Radzen.Blazor;

namespace Condominio.Web.Components.Pages
{
    public partial class FacturaMesCasaPage
    {




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
            // Usas el método específico si existe
            var itemList = await FacturaMesCasaRepository.GetAllAsync();
            selectedEmployees = new List<FacturaMesCasa>() { itemList.FirstOrDefault() };

            items = itemList.AsQueryable();
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



    }
}
