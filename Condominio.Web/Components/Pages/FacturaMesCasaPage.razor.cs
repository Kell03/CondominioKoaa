using Condominio.Application.Services;
using Condominio.Domain.Entities;
using Condominio.Infrastructure.Repositories;
using Condominio.Web.Components.Dialogs;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using System.Globalization;

namespace Condominio.Web.Components.Pages
{
    public partial class FacturaMesCasaPage
    {

        RadzenDataGrid<FacturaMesCasa> grid;
        IList<FacturaMesCasa> selectedEmployees;
        FacturaMesCasa selectedItem = new FacturaMesCasa();
        private bool isAdmin = false;
        private IEnumerable<FacturaMes> FacturasMes;
        private IEnumerable<Houses> HouseList;
        Payments Pago = new Payments();
        private IEnumerable<Payments> PagosList;
        private List<string> MetodosPago = new List<string>
        {
        "Pago Movil"
        };
        private IQueryable<FacturaMesCasa> items;
        private BcvRates rates;





        protected override async Task OnInitializedAsync()
        {
            await CheckAdminRole();
            await LoadData();
            rates = await BcvScraper.GetRatesAsync();
            await Task.Delay(5000);  // 2000 milisegundos = 2 segundos
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



        private async Task EditUser(FacturaMesCasa item)
        {
            if(isAdmin == true)
            {
                return;
            }
            Pago = new Payments();
            Pago.Tasa = rates?.EUR; 
            Pago.MontoBs = 0;
            await LoadPagos(item.Id);
            selectedItem = item;
            selectedIndex = 1;
            StateHasChanged();
            await Task.CompletedTask;
        }



        #region Pagos 

        private async Task SavePago()
        {

            Pago.FacturaMesCasaId = selectedItem.Id;
            await FacturaMesCasaRepository.RegistrarPagoFactura(Pago);
            await FacturaMesCasaRepository.SaveChangesAsync();
            // Si tienes SaveChanges en el repositorio
            await LoadPagos((int)Pago.FacturaMesCasaId); // Recargar la lista
            Pago = new Payments();

            StateHasChanged();


        }

        private async Task LoadPagos(int idFacturaMes)
        {
            PagosList = await FacturaMesCasaRepository.GetPaymentsForUserAsync(AppState.CurrentUser.Id, idFacturaMes);
        }



        private void OnMontoChanged(decimal? value)
        {
            if (value.HasValue && value.Value > 0 && Pago.Tasa > 0)
            {
                Pago.MontoBs = Math.Round(value.Value * (decimal)Pago.Tasa, 2);
                Pago.Monto = (decimal)value;
            }
            else
            {
                Pago.MontoBs = null;
            }
            StateHasChanged();
        }



        private async Task AddPagosList(FacturaMesCasa factura)
        {

            var parameters = new Dictionary<string, object>
        {
            { "IdItem", factura.Id },
            { "Cuotas", false } // Indica que no es una cuota especial
        };

            var result = await DialogService.OpenAsync<ListPayment>(
                $"Pago de {factura.NombreMes} {factura.FacturaMes.Year} casa {factura.House.Number}",
                parameters,
                  new DialogOptions
                  {
                      Style = "margin-top: 20px; width: 600px; max-width: 90vw;",  // ✅ RESPONSIVE
                      Resizable = true,
                      Draggable = true,
                  }
            );


            await LoadData();
            StateHasChanged();


        }
        #endregion




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
