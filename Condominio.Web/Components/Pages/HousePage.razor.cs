using Condominio.Domain.Entities;
using Condominio.Infrastructure.Repositories;
using Radzen;

namespace Condominio.Web.Components.Pages
{
    public partial class HousePage
    {




        IList<Houses> selectedEmployees;
        Houses selectedItem = new Houses();



        IEnumerable<Houses> House;

        private IQueryable<Houses> items;

        protected override async Task OnInitializedAsync()
        {
            await LoadUsers();

            House = await HouseRepository.GetAllAsync();

        }

        private async Task LoadUsers()
        {
            // Usas el método específico si existe
            var itemList = await HouseRepository.GetAllAsync();
            selectedEmployees = new List<Houses>() { itemList.FirstOrDefault() };

            items = itemList.AsQueryable();
        }


        private async Task SaveItem()
        {

            if (selectedItem.Id != 0)
            {
                selectedItem.UpdatedAt = DateTime.Now;
                HouseRepository.Update(selectedItem);
                await HouseRepository.SaveChangesAsync();
                // Si tienes SaveChanges en el repositorio
                await LoadUsers(); // Recargar la lista
                selectedIndex = 0;
                selectedItem = new Houses();

                StateHasChanged();

            }
            else
            {


                await HouseRepository.AddAsync(selectedItem);
                await HouseRepository.SaveChangesAsync();
                // Si tienes SaveChanges en el repositorio
                await LoadUsers(); // Recargar la lista
                selectedIndex = 0;
                selectedItem = new Houses();

                StateHasChanged();
            }
        }




        private async Task ConfirmDelete(Houses item)
        {
            var result = await DialogService.Confirm(
                $"¿Estás seguro de eliminar la casa {item.Number}?",
                "Confirmar eliminación",
                new ConfirmOptions() { OkButtonText = "Sí", CancelButtonText = "No" }
            );

            if (result == true)
            {
                await DeleteUser(item);
            }
        }

        private async Task DeleteUser(Houses item)
        {
            HouseRepository.Delete(item);
            await HouseRepository.SaveChangesAsync();
            // Si tienes SaveChanges en el repositorio
            await LoadUsers(); // Recargar la lista
            StateHasChanged();

        }


        private async Task EditUser(Houses item)
        {
            selectedItem = item;
            selectedIndex = 1;
            StateHasChanged();
            await Task.CompletedTask;
        }



        private async Task AddUser()
        {
            selectedItem = new Houses();
            selectedIndex = 1;
            StateHasChanged();
            await Task.CompletedTask;
        }




    }

}

