using Condominio.Domain.Entities;
using Microsoft.JSInterop;
using Radzen;

namespace Condominio.Web.Components.Pages
{
    public partial class MovimientosPage
    {



        IList<Movimientos> selectedEmployees;
        Movimientos selectedItem = new Movimientos();
        int selectedIndex = 0;


        IEnumerable<Movimientos> House;

        private IQueryable<Movimientos> items;

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

            House = await Movimientorepository.GetAllAsync();

        }

        private async Task LoadUsers()
        {
            // Usas el método específico si existe
            var itemList = await Movimientorepository.GetAllAsync();
            selectedEmployees = new List<Movimientos>() { itemList.FirstOrDefault() };

            items = itemList.AsQueryable();
        }


        



       

       


       


      

    }
}
