using Condominio.Domain.Entities;
using Condominio.Infrastructure.Repositories;
using Radzen;

namespace Condominio.Web.Components.Pages
{
    public partial class CategoriaPage
    {





        IList<CategoriasGasto> selectedEmployees;
        CategoriasGasto selectedItem = new CategoriasGasto();


        private IList<Proveedor> proveedores;

        IEnumerable<CategoriasGasto> House;

        private IQueryable<CategoriasGasto> items;

        protected override async Task OnInitializedAsync()
        {
            await LoadUsers();

            House = await categoriaRepository.GetAllAsync();
            var itemList = await proveedorRepository.GetAllAsync();
            proveedores = itemList.ToList();


        }

        private async Task LoadUsers()
        {
            // Usas el método específico si existe
            var itemList = await categoriaRepository.GetAllAsync();
            selectedEmployees = new List<CategoriasGasto>() { itemList.FirstOrDefault() };

            items = itemList.AsQueryable();
        }


        private async Task SaveItem()
        {
            try
            {
                if (selectedItem.Id != 0)
                {
                    selectedItem.UpdatedAt = DateTime.Now;
                    categoriaRepository.Update(selectedItem);
                    await categoriaRepository.SaveChangesAsync();
                    // Si tienes SaveChanges en el repositorio
                    await LoadUsers(); // Recargar la lista
                    selectedIndex = 0;
                    selectedItem = new CategoriasGasto();

                    StateHasChanged();

                }
                else
                {


                    await categoriaRepository.AddAsync(selectedItem);
                    await categoriaRepository.SaveChangesAsync();
                    // Si tienes SaveChanges en el repositorio
                    await LoadUsers(); // Recargar la lista
                    selectedIndex = 0;
                    selectedItem = new CategoriasGasto();

                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                // Manejo de errores
                Console.WriteLine($"Error al guardar el item: {ex.Message}");
            }
           
        }




        private async Task ConfirmDelete(CategoriasGasto item)
        {
            var result = await DialogService.Confirm(
                $"¿Estás seguro de eliminar la categoria {item.Nombre}?",
                "Confirmar eliminación",
                new ConfirmOptions() { OkButtonText = "Sí", CancelButtonText = "No" }
            );

            if (result == true)
            {
                await DeleteUser(item);
            }
        }

        private async Task DeleteUser(CategoriasGasto item)
        {
            categoriaRepository.Delete(item);
            await categoriaRepository.SaveChangesAsync();
            // Si tienes SaveChanges en el repositorio
            await LoadUsers(); // Recargar la lista
            StateHasChanged();

        }


        private async Task EditUser(CategoriasGasto item)
        {
            selectedItem = item;
            selectedIndex = 1;
            StateHasChanged();
            await Task.CompletedTask;
        }



        private async Task AddUser()
        {
            selectedItem = new CategoriasGasto();
            selectedIndex = 1;
            StateHasChanged();
            await Task.CompletedTask;
        }



    }
}
