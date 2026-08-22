using Condominio.Domain.Entities;
using Condominio.Infrastructure.Repositories;
using Radzen;

namespace Condominio.Web.Components.Pages
{
    public partial class FacturaMesCasaPage
    {




        IList<FacturaMesCasa> selectedEmployees;
        FacturaMesCasa selectedItem = new FacturaMesCasa();


        private List<string> MetodosPago = new List<string>
    {
        "Pago Movil"
    };

        private IQueryable<FacturaMesCasa> items;

        protected override async Task OnInitializedAsync()
        {
            await LoadUsers();


        }

        private async Task LoadUsers()
        {
            // Usas el método específico si existe
            var itemList = await FacturaMesCasaRepository.GetAllAsync();
            selectedEmployees = new List<FacturaMesCasa>() { itemList.FirstOrDefault() };

            items = itemList.AsQueryable();
        }


    



        private string ObtenerNombreMes(int? mes)
        {
            if (mes == null)
            {
                return "";
            }
            else
            {
                string[] meses = { "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
                           "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" };
                return meses[(int)mes - 1];
            }
        }


        private async Task EditUser(FacturaMesCasa item)
        {
            selectedItem = item;
            selectedIndex = 1;
            StateHasChanged();
            await Task.CompletedTask;
        }



        private async Task AddUser()
        {
            selectedItem = new FacturaMesCasa();
            selectedIndex = 1;
            StateHasChanged();
            await Task.CompletedTask;
        }


        private string GetMesAnio()
        {
            return $"{selectedItem.NombreMes} {selectedItem.Year}";
        }


        private async Task SaveItem()
        {

                selectedItem.UpdatedAt = DateTime.Now;
            selectedItem.Estado = "En Revisión"; // Cambiar el estado a "En Revisión"
            FacturaMesCasaRepository.Update(selectedItem);
                await FacturaMesCasaRepository.SaveChangesAsync();
                // Si tienes SaveChanges en el repositorio
                await LoadUsers(); // Recargar la lista
                selectedIndex = 0;
                selectedItem = new FacturaMesCasa();

                StateHasChanged();

           
        }



    }
}
