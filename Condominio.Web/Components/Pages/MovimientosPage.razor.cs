using Condominio.Application.Services;
using Condominio.Domain.Entities;
using Condominio.Infrastructure.Repositories;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;

namespace Condominio.Web.Components.Pages
{
    public partial class MovimientosPage
    {



        IList<Movimientos> selectedEmployees;
        Movimientos selectedItem = new Movimientos();
        int selectedIndex = 0;


        IEnumerable<Movimientos> House;


        private bool _esMobile = false;
        private bool _cargando = true;
        RadzenDataGrid<Movimientos> grid;

        private IEnumerable<Movimientos> items;
        private int totalFacturas = 0;

        private int currentPage = 1;
        private int TotalPages => (int)Math.Ceiling((double)totalFacturas / 10);
        private string _filtroGlobal = "";



        private async Task ChangePage(int page)
        {
            if (page < 1 || page > TotalPages) return;

            currentPage = page;

            // Llamamos a LoadData con el Skip y Take correctos para la nueva página
            await LoadData(new Radzen.LoadDataArgs
            {
                Skip = (currentPage - 1) * 10,
                Top = 10
            });
        }
        private async Task LoadData(Radzen.LoadDataArgs args)
        {
            try
            {
                await Task.Yield();

                var skip = args.Skip ?? 0;
                var take = args.Top ?? 10;

                string filtroActual = !string.IsNullOrEmpty(args.Filter) ? args.Filter : _filtroGlobal;


               
                    var (datos, total) = await Movimientorepository.GetPagedAsync(skip, take, filtroActual);
                    items = datos.ToList();
                    totalFacturas = total;
              
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR EN LOADDATA: {ex.Message}");
            }
        }

        private async Task BuscarGlobal(string texto)
        {
            _filtroGlobal = texto;
            await LoadData(new Radzen.LoadDataArgs { Skip = 0, Top = 10 });
        }
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

                    if (_esMobile)
                    {
                        // Simulamos la carga de la página 1
                        await LoadData(new Radzen.LoadDataArgs { Skip = 0, Top = 10 });
                    }
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
