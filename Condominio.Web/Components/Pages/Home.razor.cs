using Condominio.Domain.Entities;

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
        IEnumerable<CuotaEspecial> cuotas = [];
        List<string> listaCuotas = [];
        string selectedCuota = "";
       

        protected override async Task OnInitializedAsync()
        {
            try
            {
                await base.OnInitializedAsync();

                await LoadData();
                await LoadDataCuotas();
            }
            catch (Exception ex)
            {
                ;
            }

        }


        private async Task LoadData()
        {
            facturas = await FacturaMesRepository.GetAllAsync();
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
            revenue = new DataItem[] { data1, data2 };
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
            revenue2 = new DataItem[] { data1, data2 };
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

    }
}
