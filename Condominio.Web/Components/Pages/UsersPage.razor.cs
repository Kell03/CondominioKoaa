using Condominio.Application.Services;
using Condominio.Domain.Entities;
using Condominio.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;

namespace Condominio.Web.Components.Pages
{
    public partial class UsersPage
    {


        IList<Users> selectedEmployees;
        Users selectedUser = new Users();
        private bool isAdmin = false;


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

        private bool isUploading = false;

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

            await CheckAdminRole();

            await LoadUsers();

            House = await HouseRepository.GetAllAsync();

        }

        private async Task LoadUsers()
        {
            // Usas el método específico si existe
            var user = AppState.CurrentUser.Role;
            if(isAdmin)
            {
                var userList = await UserRepository.GetAllAsync();
                selectedEmployees = new List<Users>() { userList.FirstOrDefault() };

                users = userList.AsQueryable();
            }
            else
            {
                var userList = await UserRepository.GetAllByUserAsync(AppState.CurrentUser.Id);
                selectedEmployees = new List<Users>() { userList.FirstOrDefault() };
                users = userList.AsQueryable();
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

        private async Task SaveItem()
        {

            if (selectedUser.Id != 0)
            {
                selectedUser.UpdatedAt = DateTime.Now;
                await UserRepository.UpdateAsync(selectedUser);
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
