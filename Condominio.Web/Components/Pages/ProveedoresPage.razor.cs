using Condominio.Domain.Entities;
using Radzen;

namespace Condominio.Web.Components.Pages
{
    public partial class ProveedoresPage
    {




        IList<Proveedor> selectedEmployees;
        Proveedor selectedItem = new Proveedor();



        IEnumerable<Proveedor> House;

        private IQueryable<Proveedor> items;

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
                    proveedorRepository.Update(selectedItem);
                    await proveedorRepository.SaveChangesAsync();
                    // Si tienes SaveChanges en el repositorio
                    await LoadUsers(); // Recargar la lista
                    selectedIndex = 0;
                    selectedItem = new Proveedor();

                    StateHasChanged();

                }
                else
                {


                    await proveedorRepository.AddAsync(selectedItem);
                    await proveedorRepository.SaveChangesAsync();
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
            proveedorRepository.Delete(item);
            await proveedorRepository.SaveChangesAsync();
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
