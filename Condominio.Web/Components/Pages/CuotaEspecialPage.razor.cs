using Condominio.Domain.Entities;
using Radzen;

namespace Condominio.Web.Components.Pages
{
    public partial class CuotaEspecialPage
    {





        IList<CuotaEspecial> selectedEmployees;
        CuotaEspecial selectedItem = new CuotaEspecial();
        private List<int> years = new List<int>();

        private int selectedMonth = DateTime.Now.Month;
        private int selectedYear = DateTime.Now.Year;


        IEnumerable<CuotaEspecial> Cuotas;

        private IQueryable<CuotaEspecial> items;

        protected override async Task OnInitializedAsync()
        {
            await LoadUsers();

            for (int i = 2020; i <= 2030; i++)
            {
                years.Add(i);
            }

            Cuotas = await CuotaEspecialRepository.GetAllAsync();

        }

        private async Task LoadUsers()
        {
            // Usas el método específico si existe
            var itemList = await CuotaEspecialRepository.GetAllAsync();
            selectedEmployees = new List<CuotaEspecial>() { itemList.FirstOrDefault() };

            items = itemList.AsQueryable();
        }


        private async Task SaveItem()
        {

            if (selectedItem.Id != 0)
            {
                selectedItem.UpdatedAt = DateTime.Now;
                CuotaEspecialRepository.Update(selectedItem);
                await CuotaEspecialRepository.SaveChangesAsync();
                // Si tienes SaveChanges en el repositorio
                await LoadUsers(); // Recargar la lista
                selectedIndex = 0;
                selectedItem = new CuotaEspecial();

                StateHasChanged();

            }
            else
            {
                try
                {

                    await CuotaEspecialRepository.AddAsync(selectedItem);
                    await CuotaEspecialRepository.SaveChangesAsync();
                    // Si tienes SaveChanges en el repositorio
                    await LoadUsers(); // Recargar la lista
                    selectedIndex = 0;
                    selectedItem = new CuotaEspecial();

                    StateHasChanged();
                }
                catch (Exception ex)
                {
                    ;
                }
            }
        }




        private async Task ConfirmDelete(CuotaEspecial item)
        {
            var result = await DialogService.Confirm(
                $"¿Estás seguro de eliminar la casa {item.Id}?",
                "Confirmar eliminación",
                new ConfirmOptions() { OkButtonText = "Sí", CancelButtonText = "No" }
            );

            if (result == true)
            {
                await DeleteUser(item);
            }
        }

        private async Task DeleteUser(CuotaEspecial item)
        {
            CuotaEspecialRepository.Delete(item);
            await CuotaEspecialRepository.SaveChangesAsync();
            // Si tienes SaveChanges en el repositorio
            await LoadUsers(); // Recargar la lista
            StateHasChanged();

        }


        private async Task EditUser(CuotaEspecial item)
        {
            selectedItem = item;
            selectedIndex = 1;
            StateHasChanged();
            await Task.CompletedTask;
        }



        private async Task AddUser()
        {
            selectedItem = new CuotaEspecial();
            selectedIndex = 1;
            StateHasChanged();
            await Task.CompletedTask;
        }


        private List<object> months = new List<object>
        {
            new { Value = 1, Name = "Enero" },
            new { Value = 2, Name = "Febrero" },
            new { Value = 3, Name = "Marzo" },
            new { Value = 4, Name = "Abril" },
            new { Value = 5, Name = "Mayo" },
            new { Value = 6, Name = "Junio" },
            new { Value = 7, Name = "Julio" },
            new { Value = 8, Name = "Agosto" },
            new { Value = 9, Name = "Septiembre" },
            new { Value = 10, Name = "Octubre" },
            new { Value = 11, Name = "Noviembre" },
            new { Value = 12, Name = "Diciembre" }
        };


    }
}
