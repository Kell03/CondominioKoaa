using Condominio.Domain.Entities;
using Radzen;

namespace Condominio.Web.Components.Pages
{
    public partial class GastoPage
    {


        IList<Gasto> selectedEmployees;
        Gasto selectedItem = new Gasto();
        List<FacturaMes> facturas = new List<FacturaMes>();
        List<FacturaMesHijo> detallesFactura = new List<FacturaMesHijo>();
        List<CuotaEspecial> cuotasEspeciales = new List<CuotaEspecial>();
        IEnumerable<Gasto> item;
        List<string> origenes = new() { "Factura", "Cuota Especial" };
        string origenSeleccionado = "Factura";
        int? facturaSeleccionada = 0;


        private IQueryable<Gasto> items;

        protected override async Task OnInitializedAsync()
        {
            await LoadUsers();

            item = await gastosRepository.GetAllAsync();
            var ListacuotasEspeciales = await cuotaEspecialRepository.GetAllAsync();
            var listaFacutas = await facturaMesRepository.GetAllAsync();
            facturas = listaFacutas.ToList();
            cuotasEspeciales = ListacuotasEspeciales.ToList();

        }

        private async Task LoadUsers()
        {
            // Usas el método específico si existe
            var itemList = await gastosRepository.GetAllAsync();
            selectedEmployees = new List<Gasto>() { itemList.FirstOrDefault() };

            items = itemList.AsQueryable();
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
                    await gastosRepository.SaveChangesAsync();
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

    }
}
