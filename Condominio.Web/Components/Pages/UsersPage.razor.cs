using Condominio.Domain.Entities;
using Condominio.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Radzen;
using Radzen.Blazor;

namespace Condominio.Web.Components.Pages
{
    public partial class UsersPage
    {


        IList<Users> selectedEmployees;
        Users selectedUser = new Users();


        private List<string> roles = new List<string>
    {
        "Administrador",
        "Usuario",
        "Encargado"
    };

        private Dictionary<bool, string> estados = new Dictionary<bool, string>
    {
        { true, "Activo" },
        { false, "Inactivo" }
    };




        IEnumerable<Houses> House;

        private IQueryable<Users> users;

        protected override async Task OnInitializedAsync()
        {
            await LoadUsers();

            House = await HouseRepository.GetAllAsync();

        }

        private async Task LoadUsers()
        {
            // Usas el método específico si existe
            var userList = await UserRepository.GetAllAsync();
            selectedEmployees = new List<Users>() { userList.FirstOrDefault() };

            users = userList.AsQueryable();
        }

        private async Task ToggleStatus(int userId)
        {
            var user = await UserRepository.GetByIdAsync(userId);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                UserRepository.Update(user);
                // Si tienes SaveChanges en el repositorio
                await UserRepository.AddAsync(user);
                await LoadUsers(); // Recargar la lista
                StateHasChanged();
            }
        }


        private async Task SaveItem()
        {

            if (selectedUser.Id != 0)
            {
                selectedUser.UpdatedAt = DateTime.Now;
                UserRepository.Update(selectedUser);
                await UserRepository.SaveChangesAsync();
                // Si tienes SaveChanges en el repositorio
                await LoadUsers(); // Recargar la lista
                selectedIndex = 0;
                selectedUser = new Users();

                StateHasChanged();

            }
            else
            {


                await UserRepository.AddAsync(selectedUser);
                await UserRepository.SaveChangesAsync();
                // Si tienes SaveChanges en el repositorio
                await LoadUsers(); // Recargar la lista
                selectedIndex = 0;
                selectedUser = new Users();

                StateHasChanged();
            }
        }

      


        private async Task ConfirmDelete(Users user)
        {
            var result = await DialogService.Confirm(
                $"¿Estás seguro de eliminar al usuario {user.Name}?",
                "Confirmar eliminación",
                new ConfirmOptions() { OkButtonText = "Sí", CancelButtonText = "No" }
            );

            if (result == true)
            {
                await DeleteUser(user);
            }
        }

        private async Task DeleteUser(Users user)
        {
             UserRepository.Delete(user);
                await UserRepository.SaveChangesAsync();
                // Si tienes SaveChanges en el repositorio
                await LoadUsers(); // Recargar la lista
                StateHasChanged();

        }


        private async Task EditUser(Users user)
        {
            selectedUser = user;
            selectedIndex = 1;
            StateHasChanged();
            await Task.CompletedTask;
        }



        private async Task AddUser()
        {
            selectedUser = new Users();
            selectedIndex = 1;
            StateHasChanged();
            await Task.CompletedTask;
        }




    }
}
