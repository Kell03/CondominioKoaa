using Condominio.Application.Services;
using Condominio.Domain.Entities;
using Condominio.Infrastructure.Repositories;
using Condominio.Web.Components.Dialogs;
using Radzen;

namespace Condominio.Web.Components.Pages
{
    public partial class CuotaEspecialCasaPage
    {

        IList<CuotaEspecialCasa> selectedEmployees;
        CuotaEspecialCasa selectedItem = new CuotaEspecialCasa();
        private IEnumerable<CuotaEspecial> CuotasEspeciales;
        private IEnumerable<Houses> HouseList;
        private bool isAdmin = false;

        IEnumerable<CuotaEspecialCasa> Cuotas;

        private IQueryable<CuotaEspecialCasa> items;

        Payments Pago = new Payments();
        private IEnumerable<Payments> PagosList;
        private List<string> MetodosPago = new List<string>
        {
        "Pago Movil"
        };
        private BcvRates rates;
        private string _montoBsStr = "0,00";
        private string _montoUsdStr = "0,00";
        private bool _isFormatting = false;
        private bool _isFormattingusd = false;
        protected override async Task OnInitializedAsync()
        {
            await CheckAdminRole();
            await LoadData();
            Cuotas = await CuotaEspecialCasaRepository.GetAllAsync();
            rates = await BcvScraper.GetRatesAsync();
            await Task.Delay(5000);
            StateHasChanged();

        }

        private async Task LoadData()
        {
            try
            {

                var role = AppState.CurrentUser.Role;
                IEnumerable<CuotaEspecialCasa> itemList;

                if (role == "Administrador")
                {
                    itemList = await CuotaEspecialCasaRepository.GetAllAsync();
                }
                else
                {
                    itemList = await CuotaEspecialCasaRepository.GetAllForUserAsync(AppState.CurrentUser.Id);
                }

                items = itemList.AsQueryable();

                selectedEmployees = itemList.Any()
                    ? new List<CuotaEspecialCasa> { itemList.First() }
                    : new List<CuotaEspecialCasa>();

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
            await LoadCuotas();
            await LoadHouses();
            selectedItem = new CuotaEspecialCasa();
            selectedIndex = 2;
            StateHasChanged();
            await Task.CompletedTask;
        }


        private async Task LoadCuotas()
        {
            CuotasEspeciales = await CuotaEspecialRepository.GetAllAsync();
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
                isAdmin =false;
            }
        }



        private async Task EditUser(CuotaEspecialCasa item)
        {
            if (isAdmin == true)
            {
                return;
            }

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

            Pago.CuotaEspecialCasaId = selectedItem.Id;
            await CuotaEspecialCasaRepository.RegistrarPagoCuota(Pago);
            await CuotaEspecialCasaRepository.SaveChangesAsync();
            // Si tienes SaveChanges en el repositorio
            await LoadPagos((int)Pago.CuotaEspecialCasaId); // Recargar la lista
            Pago = new Payments();
            Pago.Tasa = rates?.EUR;

            StateHasChanged();


        }

        private async Task LoadPagos(int idFacturaMes)
        {
            PagosList = await CuotaEspecialCasaRepository.GetPaymentsForUserAsync(AppState.CurrentUser.Id, idFacturaMes);
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



        private async Task AddPagosList(CuotaEspecialCasa cuota)
        {

            var parameters = new Dictionary<string, object>
        {
            { "IdItem", cuota.Id },
            { "Cuotas", true } // Indica que no es una cuota especial
        };

            var result = await DialogService.OpenAsync<ListPayment>(
                $"{cuota.CuotaEspecial.Motivo} {cuota.CuotaEspecial.NombreMes} {cuota.CuotaEspecial.Year} casa {cuota.House.Number}",
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



      


       


    }
}
