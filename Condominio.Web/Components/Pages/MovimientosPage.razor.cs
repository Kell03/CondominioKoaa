using Condominio.Domain.Entities;
using Radzen;

namespace Condominio.Web.Components.Pages
{
    public partial class MovimientosPage
    {



        IList<Movimientos> selectedEmployees;
        Movimientos selectedItem = new Movimientos();
        int selectedIndex = 0;


        IEnumerable<Movimientos> House;

        private IQueryable<Movimientos> items;

        protected override async Task OnInitializedAsync()
        {
            await LoadUsers();

            House = await Movimientorepository.GetAllAsync();

        }

        private async Task LoadUsers()
        {
            // Usas el método específico si existe
            var itemList = await Movimientorepository.GetAllAsync();
            selectedEmployees = new List<Movimientos>() { itemList.FirstOrDefault() };

            items = itemList.AsQueryable();
        }


        



       

       


       


      

    }
}
