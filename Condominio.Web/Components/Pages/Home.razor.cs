using Condominio.Application.Services;
using Condominio.Domain.Entities;
using Condominio.Infrastructure.Repositories;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Condominio.Web.Components.Pages
{
    public partial class Home
    {

        class DataItem
        {
            public string Category { get; set; }
            public double Value { get; set; }
            public bool Highlight { get; set; } = true;
        }

        public string Title = "";
        public string TitleCuota = "";
        public double Total = 0;
        public double Total2 = 0;
         public double movimientoTotalBs = 0;
         public double movimientoTotalUsd = 0;
        public List<MesModel> months = MesModel.GetMeses();  // ✅ Directo sin cargar en OnInit
        public int? selectedMonth = null;
        DataItem[] revenue = new DataItem[]
        {
        };
        DataItem[] revenue2 = new DataItem[]
        {
        };
        private string? yearInput = DateTime.Now.Year.ToString();
        private int? selectedYear = DateTime.Now.Year;

        IEnumerable<FacturaMes> facturas = [];
        IEnumerable<Movimientos> Movimientos = [];
        IEnumerable<CuotaEspecial> cuotas = [];
        List<string> listaCuotas = [];
        string selectedCuota = "";

        private bool yaCargo = false;

        private BcvRates rates;
        private bool cargandoRates = true;

        string Moneda = "USD";
        decimal tasa = 0;
        private bool isAdmin = false;

        private decimal deudaTotalCondominio = 0;
        private decimal deudaCasaSeleccionada = 0;
        private int? selectedHouseId = null;
        private string casaSeleccionada = "";
        private List<Houses> listaCasas = new();
        [Inject] private AuthenticationStateProvider AuthProvider { get; set; }

        private bool _cargando = true;
        private bool _rolListo = false;


      
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && !yaCargo)
            {
                yaCargo = true;

                try
                {
                    var tareaRates = BcvScraper.GetRatesAsync();
                    await CheckAdminRole();
                    if (isAdmin)
                    {
                        await CargarDeudaGlobal();
                        await CargarListaCasas();
                    }
                    await LoadData();
                    await LoadDataCuotas();
                    var monedas = await MonedaRepository.GetAllAsync();
                    Moneda = monedas.Select(x => x.Moneda).FirstOrDefault();

                    // 3. Esperamos que el scraper termine
                    try
                    {
                        rates = await tareaRates;
                        if (rates == null)
                        {
                            Console.WriteLine("⚠️ rates es null, el BCV no respondió.");
                        }
                        await LoadMovimientos();

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error en BcvScraper: {ex.Message}");
                    }
                    StateHasChanged();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error en OnAfterRenderAsync: {ex.Message}");
                }
                finally
                {
                    // ✅ Esperamos solo los milisegundos necesarios a que el MainLayout
                    // termine de asignar el rol. Si ya está listo, no espera nada.
                    int intentos = 0;
                    while ((AppState.CurrentUser == null || string.IsNullOrEmpty(AppState.CurrentUser.Role)) && intentos < 50)
                    {
                        await Task.Delay(50); // 50ms es casi imperceptible
                        intentos++;
                    }

                    // Ahora que estamos seguros de que el rol existe, actualizamos isAdmin
                    if (AppState.CurrentUser != null && AppState.CurrentUser.Role == "Administrador")
                    {
                        isAdmin = true;
                    }

                    _cargando = false;
                    StateHasChanged();
                }
            }
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

        private async Task LoadData()
        {
            facturas = await FacturaMesRepository.GetAllAsync();
            facturas = facturas.Where(x => x.Motivo == string.Empty || x.Motivo == null).ToList();
           DataItem data1 = new DataItem();
            data1.Category = "Pendiente";
            var pendiente = (double)(Convert.ToDecimal(facturas.Select(x => x.MontoTotal).LastOrDefault())
                - (facturas.Select(x => x.MontoRecaudado).LastOrDefault() ?? 0));
            data1.Value = pendiente;
            DataItem data2 = new DataItem();
            data2.Category = "Pagado";
            var pagado = (double)(facturas.Select(x => x.MontoRecaudado).LastOrDefault() ?? 0);
            data2.Value = pagado;
            revenue = new DataItem[] { data2, data1 };
            var item = facturas.LastOrDefault();
            Title = $"{item?.NombreMes} {item?.Year}";
            Total = item?.MontoTotal ?? 0;

        }


        private async Task LoadMovimientos()
        {
            Movimientos = await MovimientoRepository.GetAllAsync();
            var totalMenosUsd = Movimientos.Where(x => x.IdGasto != null).Sum(x => x.MontoBs);
            var totalmasUsd = Movimientos.Where(x => x.IdGasto == null).Sum(x => x.MontoBs);
            movimientoTotalBs = (double)((totalmasUsd - totalMenosUsd) ?? 0);

            var prop = rates.GetType().GetProperty(Moneda);
            tasa = prop != null ? (decimal)prop.GetValue(rates) : 0m;

            movimientoTotalUsd = (double)Math.Round((decimal)movimientoTotalBs / (decimal)tasa, 2);

        }

        private async Task LoadDataCuotas()
        {
            cuotas = await CuotaEspecialRepository.GetAllAsync();
            listaCuotas = cuotas.Select(x => x.Motivo).ToList();
            DataItem data1 = new DataItem();
            data1.Category = "Pendiente";
            var pendiente = cuotas.Select(x => x.MontoTotal).LastOrDefault() - cuotas.Select(x => x.MontoRecaudado).LastOrDefault();
            data1.Value =  (double)pendiente;
            DataItem data2 = new DataItem();
            data2.Category = "Pagado";
            var pagado = (double)cuotas.Select(x => x.MontoRecaudado).LastOrDefault();
            data2.Value = pagado;
            revenue2 = new DataItem[] { data2, data1 };
            var item = cuotas.LastOrDefault();
            TitleCuota = $"{item?.Motivo}";
            Total2 = (double)(item?.MontoTotal ?? 0);

        }



        private async Task LoadFacturasChart()
        {
            revenue = new DataItem[] { }; // Reinicia el gráfico antes de cargar nuevos datos
            var currentFactura = facturas.Where(f => f.Year == selectedYear && f.Mes == selectedMonth).FirstOrDefault();
            if (currentFactura == null)
            {
                return;
            }
            DataItem data1 = new DataItem();
            data1.Category = "Pendiente";
            var pendiente = (double)(Convert.ToDecimal(currentFactura.MontoTotal)
                - (currentFactura.MontoRecaudado ?? 0));
            data1.Value = pendiente;
            DataItem data2 = new DataItem();
            data2.Category = "Pagado";
            var pagado = (double)(currentFactura.MontoRecaudado ?? 0);
            data2.Value = pagado;
            revenue = new DataItem[] { data2, data1 };
            Title = $"{currentFactura.NombreMes} {currentFactura.Year}";
            Total = currentFactura.MontoTotal;
            StateHasChanged();

        }


        private async Task LoadCuotasChart()
        {
            revenue2 = new DataItem[] { }; // Reinicia el gráfico antes de cargar nuevos datos
            var currentCuota = cuotas.Where(c => c.Motivo == selectedCuota).FirstOrDefault();
            if (currentCuota == null)
            {
                return;
            }
            DataItem data1 = new DataItem();
            data1.Category = "Pendiente";
            var pendiente = currentCuota.MontoTotal - currentCuota.MontoRecaudado ;
            data1.Value = (double)pendiente;
            DataItem data2 = new DataItem();
            data2.Category = "Pagado";
            var pagado = (double)currentCuota.MontoRecaudado;
            data2.Value = pagado;
            revenue2 = new DataItem[] { data2, data1 };
            TitleCuota = $"{currentCuota.Motivo}";
            Total2 = (double)currentCuota.MontoTotal;
            StateHasChanged();

        }

        private void OnYearChanged()
        {
            if (!string.IsNullOrEmpty(yearInput) && yearInput.Length == 4 && int.TryParse(yearInput, out int year))
            {
                selectedYear = year;
            }
            else if (!string.IsNullOrEmpty(yearInput) && yearInput.Length > 4)
            {
                yearInput = yearInput.Substring(0, 4); // Corta a 4 dígitos
            }
        }



        // ✅ Método para calcular la deuda TOTAL de todo el condominio
        private async Task CargarDeudaGlobal()
        {
            // Traemos todas las facturas y cuotas (paginación de 10000 para asegurar que traiga todo)
            var (facturas, _) = await FacturaMesCasaRepository.GetPagedAsync(0, 10000, null);
            var (cuotas, _) = await CuotaEspecialCasaRepository.GetPagedAsync(0, 10000, null);

            var deudaFacturas = facturas.Where(x => x.SaldoPendiente > 0).Sum(x => x.SaldoPendiente);
            var deudaCuotas = cuotas.Where(x => x.SaldoPendiente > 0).Sum(x => x.SaldoPendiente);

            deudaTotalCondominio = deudaFacturas + deudaCuotas;
        }

        // ✅ Método para cargar el dropdown de las casas
        private async Task CargarListaCasas()
        {
            var casas = await HouseRepository.GetAllAsync();
            listaCasas = casas.OrderBy(x => int.TryParse(x.Number, out int n) ? n : 0).ToList();
        }

        // ✅ Método que se ejecuta al elegir una casa en el Dropdown
        private async Task CalcularDeudaCasa()
        {
            if (selectedHouseId == null || selectedHouseId == 0) return;

            casaSeleccionada = listaCasas.FirstOrDefault(x => x.Id == selectedHouseId)?.Number ?? "";

            // Filtramos las facturas y cuotas de esa casa en específico
            var (facturas, _) = await FacturaMesCasaRepository.GetPagedAsync(0, 10000, null);
            var (cuotas, _) = await CuotaEspecialCasaRepository.GetPagedAsync(0, 10000, null);

            var deudaF = facturas.Where(x => x.HouseId == selectedHouseId && x.SaldoPendiente > 0).Sum(x => x.SaldoPendiente);
            var deudaC = cuotas.Where(x => x.HouseId == selectedHouseId && x.SaldoPendiente > 0).Sum(x => x.SaldoPendiente);

            deudaCasaSeleccionada = deudaF + deudaC;
            StateHasChanged();
        }
    }
}
