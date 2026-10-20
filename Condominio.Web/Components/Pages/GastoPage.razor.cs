using Condominio.Application.Services;
using Condominio.Domain.Entities;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Radzen;
using Radzen.Blazor;

namespace Condominio.Web.Components.Pages
{
    public partial class GastoPage
    {


        IList<Gasto> selectedEmployees;
        RadzenDataGrid<FacturaMes> gridFactura;
        Gasto selectedItem = new Gasto();
        List<FacturaMes> facturas = new List<FacturaMes>();
        List<FacturaMesHijo> detallesFactura = new List<FacturaMesHijo>();
        List<CuotaEspecial> cuotasEspeciales = new List<CuotaEspecial>();
        IEnumerable<Gasto> item;
        List<string> origenes = new() { "Factura", "Cuota Especial" };
        string origenSeleccionado = "Factura";
        int? facturaSeleccionada = 0;
        private string _montoUsdStr = "0,00";
        private string _montoBsStr = "0,00";
        private bool _isFormattingusd = false;
        private bool _isFormatting = false;

        private IQueryable<Gasto> items;
        private BcvRates rates;
        private bool cargandoRates = true;

        string Moneda = "USD";

        protected override async Task OnInitializedAsync()
        {
            try
            {
               await LoadUsers();

                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error general al cargar: {ex.Message}");
            }
        }

        private async Task LoadUsers()
        {
            // 1. Lanzamos el scraper primero (en segundo plano)
            var tareaRates = BcvScraper.GetRatesAsync();

            // 2. Cargamos la base de datos de forma SECUENCIAL (evita el error de "A second operation was started...")
            items = (await gastosRepository.GetAllAsync()).AsQueryable();
            selectedEmployees = new List<Gasto>() { items.FirstOrDefault() };

            cuotasEspeciales = (await cuotaEspecialRepository.GetAllAsync()).ToList();
            facturas = (await facturaMesRepository.GetAllAsync()).ToList();

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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en BcvScraper: {ex.Message}");
            }
        }


        private async Task SaveItem()
        {
            try
            {
                if (selectedItem.Id != 0)
                {
                    selectedItem.UpdatedAt = DateTime.Now;

                    gastosRepository.Update(selectedItem);
                    await gastosRepository.SaveChangesAsync();
                    // Si tienes SaveChanges en el repositorio
                    await LoadUsers(); // Recargar la lista
                    selectedIndex = 0;
                    selectedItem = new Gasto();

                    StateHasChanged();

                }
                else
                {


                    await gastosRepository.AddAsync(selectedItem);
                    // Si tienes SaveChanges en el repositorio
                    await LoadUsers(); // Recargar la lista
                    selectedIndex = 0;
                    selectedItem = new Gasto();

                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                // Manejo de errores
                Console.WriteLine($"Error al guardar el item: {ex.Message}");
            }

        }




        private async Task ConfirmDelete(Gasto item)
        {
            var result = await DialogService.Confirm(
                $"¿Estás seguro de eliminar el Gasto {item.Comprobante}?",
                "Confirmar eliminación",
                new ConfirmOptions() { OkButtonText = "Sí", CancelButtonText = "No" }
            );

            if (result == true)
            {
                await DeleteUser(item);
            }
        }

        private async Task OnOrigenChanged()
        {
            // Limpiar detalle seleccionado
            selectedItem.FacturaMesHijoId = null;
            selectedItem.CuotaEspecialId = null;
            detallesFactura.Clear();
            selectedItem.Monto = 0;

        }


        private async Task OnFacturaChanged()
        {
            // Limpiar detalle seleccionado
            selectedItem.FacturaMesHijoId = null;
            detallesFactura.Clear();

            if (!facturaSeleccionada.HasValue)
                return;

            // Cargar los detalles de la factura (solo los NO pagados)
            var detalles = await facturaMeHijoRepository.GetAllFByFacturaAsync(facturaSeleccionada.Value);

            if (detalles.Any())
            {
                detallesFactura = detalles.Where(d => !d.Pagado).ToList();
            }
        }


        private async Task OnDetalleChanged()
        {
           

            if (!selectedItem.FacturaMesHijoId.HasValue)
                return;

            // Cargar los detalles de la factura (solo los NO pagados)
            var detalle = await facturaMeHijoRepository.GetByIdAsync(selectedItem.FacturaMesHijoId.Value);

            if (detalle != null)
            {
                selectedItem.Monto = Convert.ToDecimal(detalle.Monto);
                selectedItem.MontoBs = Math.Round((decimal)detalle.Monto * (decimal)selectedItem.Tasa, 2);
                _montoUsdStr = selectedItem.Monto.ToString("N2", new System.Globalization.CultureInfo("es-ES"));
                _montoBsStr = selectedItem.MontoBs.Value.ToString("N2", new System.Globalization.CultureInfo("es-ES"));
            }
        }

        private async Task OnCuotaChanged()
        {


            if (!selectedItem.CuotaEspecialId.HasValue)
                return;

            // Cargar los detalles de la factura (solo los NO pagados)
            var cuota =cuotasEspeciales.Where(x => x.Id == selectedItem.CuotaEspecialId).FirstOrDefault();

            if (cuota != null)
            {
                selectedItem.Monto = cuota.MontoTotal;
                selectedItem.MontoBs = Math.Round(cuota.MontoTotal * (decimal)selectedItem.Tasa, 2);
                _montoUsdStr = selectedItem.Monto.ToString("N2", new System.Globalization.CultureInfo("es-ES"));
                _montoBsStr = selectedItem.MontoBs.Value.ToString("N2", new System.Globalization.CultureInfo("es-ES"));
            }
        }



        private async Task DeleteUser(Gasto item)
        {
            gastosRepository.Delete(item);
            await gastosRepository.SaveChangesAsync();
            // Si tienes SaveChanges en el repositorio
            await LoadUsers(); // Recargar la lista
            StateHasChanged();

        }


        private async Task EditGasto(Gasto item)
        {
            selectedItem = item;
            selectedIndex = 1;
            StateHasChanged();
            await Task.CompletedTask;
        }



        private async Task AddGasto()
        {

            try
            {

                 selectedItem = new Gasto();

                selectedItem.FechaPago = DateTime.Today;
                var prop = rates.GetType().GetProperty(Moneda);
                selectedItem.Tasa = prop != null ? (decimal)prop.GetValue(rates) : 0m;

                selectedIndex = 1;
                StateHasChanged();
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                // Manejo de errores
                Console.WriteLine($"Error al agregar el item: {ex.Message}");
            }
            
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
                    selectedItem.Monto = 0;
                    StateHasChanged();
                    return;
                }

                var valorNumerico = decimal.Parse(numeros) / 100;

                if (valorNumerico > 0 && selectedItem.Tasa > 0)
                {
                    selectedItem.Monto = Math.Round(valorNumerico, 2);
                    selectedItem.MontoBs = Math.Round(valorNumerico * (decimal)selectedItem.Tasa, 2);
                    _montoUsdStr = selectedItem.Monto.ToString("N2", new System.Globalization.CultureInfo("es-ES"));
                    _montoBsStr = selectedItem.MontoBs.Value.ToString("N2", new System.Globalization.CultureInfo("es-ES"));
                }
                else
                {
                    selectedItem.Monto = 0;
                    selectedItem.MontoBs = 0;
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
                    selectedItem.MontoBs = 0;
                    selectedItem.Monto = 0;
                    StateHasChanged();
                    return;
                }

                // ✅ DIVIDIR ENTRE 100 (1 → 0.01)
                var valorNumerico = decimal.Parse(numeros) / 100;

                if (valorNumerico > 0 && selectedItem.Tasa > 0)
                {
                    selectedItem.Monto = Math.Round(valorNumerico / (decimal)selectedItem.Tasa, 2);
                    selectedItem.MontoBs = Math.Round(valorNumerico, 2);
                    _montoBsStr = selectedItem.MontoBs.Value.ToString("N2", new System.Globalization.CultureInfo("es-ES"));
                    _montoUsdStr = selectedItem.Monto.ToString("N2", new System.Globalization.CultureInfo("es-ES"));
                }
                else
                {
                    selectedItem.Monto = 0;
                    selectedItem.MontoBs = 0;
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


        private async Task PagarDetalleFactura(FacturaMes factura, FacturaMesHijo detalle)
        {


            selectedItem = new Gasto();
            selectedItem.FechaPago = DateTime.Today;
            var prop = rates.GetType().GetProperty(Moneda);
            selectedItem.Tasa = prop != null ? (decimal)prop.GetValue(rates) : 0m;
            origenSeleccionado = "Factura";
            selectedItem.FacturaMesHijoId = null;
            detallesFactura.Clear();
            facturaSeleccionada = factura.Id;
            if (!facturaSeleccionada.HasValue)
                return;

            // Cargar los detalles de la factura (solo los NO pagados)
          
                detallesFactura = factura.FacturaMesHijos.Where(d => !d.Pagado).ToList();
            

            selectedItem.FacturaMesHijoId = detalle.Id;


            if (detalle != null)
            {
                selectedItem.Monto = Convert.ToDecimal(detalle.Monto);
                selectedItem.MontoBs = Math.Round((decimal)detalle.Monto * (decimal)selectedItem.Tasa, 2);
                _montoUsdStr = selectedItem.Monto.ToString("N2", new System.Globalization.CultureInfo("es-ES"));
                _montoBsStr = selectedItem.MontoBs.Value.ToString("N2", new System.Globalization.CultureInfo("es-ES"));
            }


            selectedIndex = 2;
            StateHasChanged();
        }

    }
}
