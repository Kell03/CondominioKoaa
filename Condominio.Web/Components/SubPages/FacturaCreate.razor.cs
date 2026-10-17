using Condominio.Domain.Entities;
using Condominio.Infrastructure.Repositories;
using Condominio.Web.Components.Dialogs;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace Condominio.Web.Components.SubPages
{
    public partial class FacturaCreate
    {



        FacturaMes Item = new FacturaMes();
        private int selectedMonth = DateTime.Now.Month;
        private int selectedYear = DateTime.Now.Year;

        private RadzenDataGrid<FacturaMesHijo> ordersGrid;
        DataGridEditMode editMode = DataGridEditMode.Multiple;

        List<FacturaMesHijo> ordersToInsert = new List<FacturaMesHijo>();
        List<FacturaMesHijo> ordersToUpdate = new List<FacturaMesHijo>();
        private string? yearInput = DateTime.Now.Year.ToString();

        private List<FacturaMesHijo> orders = new();
        private List<CategoriasGasto> categorias = new();
        private List<Proveedor> proveedores = new();
        private FacturaMesHijo itemEditando = new();
        private bool mostrarPanel = false;
        private int indiceEditando = -1;


        private void AgregarItem()
        {
            itemEditando = new FacturaMesHijo();
            indiceEditando = -1;
            mostrarPanel = true;
        }

        private void EditarItem(FacturaMesHijo item)
        {
            // Copia para no modificar el original hasta guardar
            itemEditando = new FacturaMesHijo
            {
                Id = item.Id,
                IdCategoria = item.IdCategoria,
                Monto = item.Monto,
                Motivo = item.Motivo
            };
            indiceEditando = orders.IndexOf(item);
            mostrarPanel = true;
        }

        // ✅ Guardar
        private async Task GuardarItem()

        {
            if (indiceEditando == -1)
            {
                // Nuevo
                var idproveedor = categorias.Where(x => x.Id  == itemEditando.IdCategoria).Select(x => x.IdProveedor).FirstOrDefault();
                itemEditando.IdProveedor = idproveedor;
                orders.Add(itemEditando);
            }
            else
            {
                // Editar
                var idproveedor = categorias.Where(x => x.Id == itemEditando.IdCategoria).Select(x => x.IdProveedor).FirstOrDefault();
                itemEditando.IdProveedor = idproveedor;
                orders[indiceEditando] = itemEditando;
            }

            CancelarEdicion();

            await ordersGrid.Reload();

            StateHasChanged();
        }

        // ✅ Cancelar
        private void CancelarEdicion()
        {
            itemEditando = new FacturaMesHijo();
            indiceEditando = -1;
            mostrarPanel = false;
        }

        // ✅ Eliminar
        private void DeleteRow(FacturaMesHijo item)
        {
            orders.Remove(item);
        }


        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            Item.Year = int.Parse(yearInput);
            orders = new List<FacturaMesHijo>();
            var itemList = await CategoriaRepository.GetAllAsync();
            categorias = itemList.Where(x => x.Isfijo).ToList();


            // 2. Recorrer las categorías y crear un FacturaMesHijo por cada una
            foreach (var categoria in categorias)
            {
                var nuevoHijo = new FacturaMesHijo
                {
                    IdCategoria = categoria.Id,
                    IdProveedor = categoria.IdProveedor, // Se auto-asigna el proveedor de la categoría
                    Motivo =null,           // Usamos el nombre de la categoría como motivo
                    Monto = (double)categoria.Monto,                           // Monto inicial en 0
                    Pagado = false                       // Aún no está pagado
                };

                orders.Add(nuevoHijo);
            }

            await ordersGrid.Reload();

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

        private List<int> years = new List<int>();
        


        private async Task SaveItem()
        {


            Item.UpdatedAt = DateTime.Now;
            await FacturaMesRepository.SaveWithFacturaHijo(Item, orders);


            await FacturaMesRepository.SaveChangesAsync();
            DialogService.Close(true);
            Volver();

        }

        private void OnYearChanged(FacturaMes item)
        {
            if (!string.IsNullOrEmpty(yearInput) && yearInput.Length == 4 && int.TryParse(yearInput, out int year))
            {
                selectedYear = year;
                item.Year = selectedYear;
            }
            else if (!string.IsNullOrEmpty(yearInput) && yearInput.Length > 4)
            {
                yearInput = yearInput.Substring(0, 4); // Corta a 4 dígitos

                item.Year = int.Parse(yearInput);
            }
        }


        private async Task AgregarCategoriaRapida()
        {
            try
            {
                var resultado = await DialogService.OpenAsync<DialogoNuevaCategoria>(
                     "Nueva Categoría",         // título
                     null,              // parámetros
                     new DialogOptions()
                 );

                // Abrimos el diálogo (asegúrate de inyectar DialogService si no lo has hecho)
                //var resultado = await DialogService.OpenAsync<DialogoNuevaCategoria>("Nueva Categoría",
                //    null,
                //    new DialogOptions { Width = "400px", CloseDialogOnOverlayClick = true });

                // Si guardó, el resultado será la Categoría
                if (resultado is CategoriasGasto nuevaCategoria)
                {
                    // 1. Recargamos la lista de categorías
                    categorias = (await CategoriaRepository.GetAllAsync()).ToList();

                    // 2. Seleccionamos automáticamente la nueva categoría
                    itemEditando.IdCategoria = nuevaCategoria.Id;

                    StateHasChanged();
                }
            }
            catch(Exception ex)
            {
                throw;
            }
        
        }
    }
}
