using Condominio.Application.Services;
using Condominio.Domain.Entities;
using Condominio.Infrastructure.Repositories;
using Condominio.Web.Components.Dialogs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
using System.Globalization;

namespace Condominio.Web.Components.Pages
{
    public partial class FacturaMesCasaPage
    {

        RadzenDataGrid<FacturaMesCasa> grid;
        private IList<FacturaMesCasa> invoices = new List<FacturaMesCasa>();
        IEnumerable<FacturaMesCasa> employees;
        bool allowRowSelectOnRowClick = true;

        FacturaMesCasa selectedItem = new FacturaMesCasa();
        private bool isAdmin = false;
        private IEnumerable<FacturaMes> FacturasMes;
        private IEnumerable<Houses> HouseList;
        Payments Pago = new Payments();
        ConfiguracionCondominio? condominio = new ConfiguracionCondominio();
        List<ConfiguracionMoneda>? monedas = new List<ConfiguracionMoneda>();
        string Moneda = "USD";
        private IEnumerable<Payments> PagosList;
        private List<string> MetodosPago = new List<string>
        {
        "Pago Movil",
        "Efectivo"
        };
        private BcvRates rates;

        private string _montoBsStr = "0,00";
        private string _montoUsdStr = "0,00";
        private bool _isFormatting = false;
        private bool _isFormattingusd = false;


        private bool _esMobile = false;
        private bool _cargando = true;

        private IEnumerable<FacturaMesCasa> items;
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


                if (isAdmin)
                {
                    var (datos, total) = await FacturaMesCasaRepository.GetPagedAsync(skip, take, filtroActual);
                    items = datos.ToList();
                    totalFacturas = total;
                }
                else
                {
                    var (datos, total) = await FacturaMesCasaRepository.GetPagedForUserAsync(skip, take, AppState.CurrentUser.Id, filtroActual);
                    items = datos.ToList();
                    totalFacturas = total;
                }
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
            await CheckAdminRole();
            allowRowSelectOnRowClick = true;
            condominio = await ConfiguracionCondominioRepository.ObtenerAsync();
            var monedasList = await MonedaRepository.GetAllAsync();
            monedas = monedasList.ToList(); 
            Moneda = monedas.Select(x => x.Moneda).FirstOrDefault();
            rates = await BcvScraper.GetRatesAsync();

            var configMonedaActual = monedas.FirstOrDefault(x => x.Moneda == Moneda);
            if (configMonedaActual != null && configMonedaActual.TasaManual.HasValue && configMonedaActual.TasaManual > 0)
            {
                // ✅ Usamos ToUpper() por si la variable "Moneda" viene en minúsculas
                var prop = rates.GetType().GetProperty(Moneda.ToUpper());
                if (prop != null)
                {
                    prop.SetValue(rates, configMonedaActual.TasaManual.Value);
                }
                else
                {
                    // Si la reflexión falla, forzamos manualmente (asumiendo que solo usas USD)
                    rates.USD = configMonedaActual.TasaManual.Value;
                }
            }
            await Task.Delay(5000);  // 2000 milisegundos = 2 segundos
            StateHasChanged();


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
            var prop = rates.GetType().GetProperty(Moneda);
            Pago.Tasa = prop != null ? (decimal)prop.GetValue(rates) : 0m;

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
            // Si tienes SaveChanges en el repositorio
            await LoadPagos((int)Pago.FacturaMesCasaId); // Recargar la lista

            // ✅ SWEET ALERT: Abrimos un diálogo modal de éxito
            await DialogService.OpenAsync<DialogoExito>("Pago Registrado",
      new Dictionary<string, object>
      {
        { "Titulo", "¡Pago Exitoso!" },
        { "Mensaje", "Tu pago ha sido reportado. El administrador lo confirmará en breve." }
      },
     new DialogOptions
     {
         // ✅ Margin-top: 25vh lo baja hasta el centro de la pantalla
         // El resto mantiene tus márgenes laterales y el diseño limpio
         Style = "width: 90vw; max-width: 350px; margin-top: 25vh; border-radius: 16px; box-shadow: 0 10px 25px rgba(0,0,0,0.2); overflow: hidden;",
         ShowClose = false,
         CloseDialogOnOverlayClick = true
     });


            

            Pago = new Payments();
            Pago.Tasa = rates.EUR;
            Pago.Monto = 0;
            Pago.MontoBs = 0;
            _montoUsdStr = "0,00";
            _montoUsdStr = "0,00";
            selectedIndex = 0;
            StateHasChanged();


        }

        private RenderFragment MostrarAlertaExito() => builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "style", "text-align: center; padding: 20px;");

            // Icono
            builder.OpenComponent<RadzenIcon>(2);
            builder.AddAttribute(3, "Icon", "check_circle");
            builder.AddAttribute(4, "Style", "font-size: 60px; color: #28a745; margin-bottom: 15px;");
            builder.CloseComponent();

            // Título
            builder.OpenComponent<RadzenText>(5);
            builder.AddAttribute(6, "TextStyle", TextStyle.H5);
            builder.AddAttribute(7, "Style", "margin-bottom: 10px;");
            builder.AddAttribute(8, "ChildContent", (RenderFragment)((b) => b.AddContent(9, "Tu pago ha sido reportado")));
            builder.CloseComponent();

            // Mensaje
            builder.OpenComponent<RadzenText>(10);
            builder.AddAttribute(11, "TextStyle", TextStyle.Body1);
            builder.AddAttribute(12, "Style", "color: #6c757d;");
            builder.AddAttribute(13, "ChildContent", (RenderFragment)((b) => b.AddContent(14, "El administrador revisará tu pago y lo confirmará en breve.")));
            builder.CloseComponent();

            builder.CloseElement(); // Cierra el div
        };

        private async Task LoadPagos(int idFacturaMes)
        {
            PagosList = await FacturaMesCasaRepository.GetPaymentsForUserAsync(AppState.CurrentUser.Id, idFacturaMes);
        }

        private void OnInputUsdChanged(string value)
        {
            if (_isFormattingusd) return;
            _isFormattingusd = true;

            try
            {
                // ✅ FILTRAR: SOLO NÚMEROS, COMA Y PUNTO
                var filtered = new string(value?.Where(c => char.IsDigit(c) || c == ',' || c == '.').ToArray() ?? Array.Empty<char>());
                _montoUsdStr = filtered;

                var numeros = new string(_montoUsdStr.Where(char.IsDigit).ToArray());

                if (string.IsNullOrEmpty(numeros))
                {
                    _montoUsdStr = "0,00";
                    Pago.Monto = 0;
                    Pago.MontoBs = 0;
                    StateHasChanged();
                    return;
                }

                var valorNumerico = decimal.Parse(numeros) / 100;

                if (valorNumerico > 0 && Pago.Tasa > 0)
                {
                    Pago.Monto = Math.Round(valorNumerico, 2);
                    Pago.MontoBs = Math.Round(valorNumerico * (decimal)Pago.Tasa, 2);
                    _montoUsdStr = Pago.Monto.ToString("N2", new System.Globalization.CultureInfo("es-ES"));
                    _montoBsStr = Pago.MontoBs.Value.ToString("N2", new System.Globalization.CultureInfo("es-ES"));
                }
                else
                {
                    Pago.Monto = 0;
                    Pago.MontoBs = 0;
                    _montoUsdStr = "0,00";
                }

                StateHasChanged();
            }
            finally
            {
                _isFormattingusd = false;
            }
        }
        private void OnInputChanged(string value)
        {
            if (_isFormatting) return;
            _isFormatting = true;

            try
            {
                // ✅ SI CONTIENE LETRAS, IGNORAR
                if (value != null && value.Any(c => char.IsLetter(c)))
                {
                    StateHasChanged();
                    return;
                }

                // ✅ FILTRAR: SOLO NÚMEROS, COMA Y PUNTO
                var filtered = new string(value?.Where(c => char.IsDigit(c) || c == ',' || c == '.').ToArray() ?? Array.Empty<char>());
                _montoBsStr = filtered;

                // ✅ LIMPIAR: SOLO NÚMEROS
                var numeros = new string(_montoBsStr.Where(char.IsDigit).ToArray());

                if (string.IsNullOrEmpty(numeros))
                {
                    _montoBsStr = "0,00";
                    Pago.MontoBs = 0;
                    Pago.Monto = 0;
                    StateHasChanged();
                    return;
                }

                // ✅ DIVIDIR ENTRE 100 (1 → 0.01)
                var valorNumerico = decimal.Parse(numeros) / 100;

                if (valorNumerico > 0 && Pago.Tasa > 0)
                {
                    Pago.Monto = Math.Round(valorNumerico / (decimal)Pago.Tasa, 2);
                    Pago.MontoBs = Math.Round(valorNumerico, 2);
                    _montoBsStr = Pago.MontoBs.Value.ToString("N2", new System.Globalization.CultureInfo("es-ES"));
                    _montoUsdStr = Pago.Monto.ToString("N2", new System.Globalization.CultureInfo("es-ES"));
                }
                else
                {
                    Pago.Monto = 0;
                    Pago.MontoBs = 0;
                    _montoBsStr = "0,00";
                    _montoUsdStr = "0,00";
                }

                StateHasChanged();
            }
            finally
            {
                _isFormatting = false;
            }
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


            if (grid != null)
            {
                await grid.Reload();
            }
            StateHasChanged();


        }
        #endregion




        void RowRender(RowRenderEventArgs<FacturaMesCasa> args)
        {
            if (isAdmin)
            {
                args.Expandable = false;
                return;
            }

            args.Expandable = (args.Data?.FacturaMes?.FacturaMesHijos != null && args.Data.FacturaMes.FacturaMesHijos.Any());
        }

        void RowExpand(FacturaMesCasa order)
        {
        }



        private void SelectAll(bool isSelected)
        {
            if (isSelected)
            {
                // Selecciona todos los items de la lista actual
                invoices = items.ToList();
            }
            else
            {
                // Deselecciona todos
                invoices.Clear();
            }
        }

        private void SelectRow(FacturaMesCasa item, bool isSelected)
        {
            if (isSelected && !invoices.Contains(item))
            {
                invoices.Add(item);
            }
            else if (!isSelected && invoices.Contains(item))
            {
                invoices.Remove(item);
            }
        }
    }
}
