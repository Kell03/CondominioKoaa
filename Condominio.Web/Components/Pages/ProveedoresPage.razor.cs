using Condominio.Domain.Entities;
using Microsoft.JSInterop;
using Radzen;

namespace Condominio.Web.Components.Pages
{
    public partial class ProveedoresPage
    {




        IList<Proveedor> selectedEmployees;
        Proveedor selectedItem = new Proveedor();



        IEnumerable<Proveedor> House;

        private IQueryable<Proveedor> items;

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

            House = await proveedorRepository.GetAllAsync();

        }

        private async Task LoadUsers()
        {
            // Usas el método específico si existe
            var itemList = await proveedorRepository.GetAllAsync();
            selectedEmployees = new List<Proveedor>() { itemList.FirstOrDefault() };

            items = itemList.AsQueryable();
        }


        private async Task SaveItem()
        {
            try
            {
                if (selectedItem.Id != 0)
                {
                    selectedItem.UpdatedAt = DateTime.Now;
                    await proveedorRepository.Update(selectedItem);
                    // Si tienes SaveChanges en el repositorio
                    await LoadUsers(); // Recargar la lista
                    selectedIndex = 0;
                    selectedItem = new Proveedor();

                    StateHasChanged();

                }
                else
                {


                    await proveedorRepository.AddAsync(selectedItem);
                    // Si tienes SaveChanges en el repositorio
                    await LoadUsers(); // Recargar la lista
                    selectedIndex = 0;
                    selectedItem = new Proveedor();

                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                // Manejo de errores
                Console.WriteLine($"Error al guardar el item: {ex.Message}");
            }

        }




        private async Task ConfirmDelete(Proveedor item)
        {
            var result = await DialogService.Confirm(
                $"¿Estás seguro de eliminar el proveedor {item.Nombre}?",
                "Confirmar eliminación",
                new ConfirmOptions() { OkButtonText = "Sí", CancelButtonText = "No" }
            );

            if (result == true)
            {
                await DeleteUser(item);
            }
        }

        private async Task DeleteUser(Proveedor item)
        {
            await proveedorRepository.Delete(item);
            // Si tienes SaveChanges en el repositorio
            await LoadUsers(); // Recargar la lista
            StateHasChanged();

        }


        private async Task EditProveedor(Proveedor item)
        {
            selectedItem = item;
            selectedIndex = 1;
            StateHasChanged();
            await Task.CompletedTask;
        }



        private async Task AddProveedor()
        {
            selectedItem = new Proveedor();
            selectedIndex = 1;
            StateHasChanged();
            await Task.CompletedTask;
        }


    }
}
