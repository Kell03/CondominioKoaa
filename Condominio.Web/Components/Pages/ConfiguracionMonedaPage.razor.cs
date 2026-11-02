using Condominio.Domain.Entities;
using Condominio.Infrastructure.Repositories;
using Microsoft.JSInterop;
using Radzen;

namespace Condominio.Web.Components.Pages
{
    public partial class ConfiguracionMonedaPage
    {

        IList<ConfiguracionMoneda> selectedEmployees;
        ConfiguracionMoneda selectedItem = new ConfiguracionMoneda();

        private List<string> monedas = new List<string>
    {
        "USD",
        "EUR"
        };

        IEnumerable<ConfiguracionMoneda> House;

        private IQueryable<ConfiguracionMoneda> items;

        private bool _esMobile = false;
        private bool _cargando = true;


        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    // ✅ Un pequeño delay para asegurar que la pantalla esté renderizada
                    await Task.Delay(100);

                    _esMobile = await JSRuntime.InvokeAsync<bool>("esPantallaMovil");
                    Console.WriteLine($"📱 Mobile: {_esMobile}");

                    // Forzamos a Blazor a redibujar la pantalla con el nuevo valor
                    StateHasChanged();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error al detectar mobile: {ex.Message}");
                }
                finally
                {
                    _cargando = false;
                    StateHasChanged();
                }
            }
        }

        protected override async Task OnInitializedAsync()
        {
            await LoadUsers();

            House = await MonedaRepository.GetAllAsync();

        }

        private async Task LoadUsers()
        {
            // Usas el método específico si existe
            var itemList = await MonedaRepository.GetAllAsync();
            selectedEmployees = new List<ConfiguracionMoneda>() { itemList.FirstOrDefault() };

            items = itemList.AsQueryable();
        }


        private async Task SaveItem()
        {

           
                selectedItem.FechaActualizacion = DateTime.Now;
                MonedaRepository.Update(selectedItem);
                await MonedaRepository.SaveChangesAsync();
                // Si tienes SaveChanges en el repositorio
                await LoadUsers(); // Recargar la lista
                selectedIndex = 0;
                selectedItem = new ConfiguracionMoneda();

                StateHasChanged();

           
        }






        private async Task EditUser(ConfiguracionMoneda item)
        {
            selectedItem = item;
            selectedIndex = 1;
            StateHasChanged();
            await Task.CompletedTask;
        }



        private async Task AddUser()
        {
            selectedItem = selectedEmployees.FirstOrDefault();
            selectedIndex = 1;
            StateHasChanged();
            await Task.CompletedTask;
        }



    }
}
