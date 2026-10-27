using Condominio.Application.Services;
using Condominio.Domain.Entities;
using Condominio.Web.Components.Dialogs;
using Radzen;
using Radzen.Blazor;
using System.Data;

namespace Condominio.Web.Components.Pages
{
    public partial class SaldosIniciales
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
        private List<Houses> houses = new();
        private List<CategoriasGasto> categorias = new();
        private List<Proveedor> proveedores = new();
        private FacturaMesHijo itemEditando = new();
        private bool mostrarPanel = false;
        private int indiceEditando = -1;
        private List<Houses> casasSeleccionadas = new();

        RadzenDataGrid<FacturaMesCasa> grid;
        private IList<FacturaMesCasa> invoices = new List<FacturaMesCasa>();
        IEnumerable<FacturaMesCasa> employees;
        private IQueryable<FacturaMesCasa> items;

        private bool isUploading = false;
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
                var idproveedor = categorias.Where(x => x.Id == itemEditando.IdCategoria).Select(x => x.IdProveedor).FirstOrDefault();
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
            IEnumerable<FacturaMesCasa> itemList;
            var housesList = await HouseRepository.GetAllAsync();
            houses = housesList.ToList();
            itemList = await FacturaMesCasaRepository.GetAllAsync();
            itemList = itemList.Where(x => x.FacturaMes.Motivo != null).ToList();
            items = itemList.AsQueryable();
            employees = itemList.Any()
                ? new List<FacturaMesCasa> { itemList.First() }
                : new List<FacturaMesCasa>();


            StateHasChanged();


            // Si el año está vacío, le ponemos el actual
            if (string.IsNullOrWhiteSpace(yearInput))
            {
                yearInput = DateTime.Now.Year.ToString();
            }
            Item.Year = int.Parse(yearInput);

            orders = new List<FacturaMesHijo>();

            // Array con los nombres de los meses para que se vean bonitos
            string[] nombresMeses = { "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
                              "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" };

            // ✅ Recorremos del 1 al 12 para crear los meses
            for (int i = 1; i <= 12; i++)
            {
                var nuevoHijo = new FacturaMesHijo
                {
                    IdCategoria = null,      // ✅ Sin categoría
                    IdProveedor = null,      // ✅ Sin proveedor
                    Motivo = nombresMeses[i - 1], // ✅ El nombre del mes (Enero, Febrero...)
                    Monto = 0,               // ✅ Monto inicial en 0
                    Pagado = false
                };

                orders.Add(nuevoHijo);
            }

            // Refrescamos el grid para que se vean los 12 meses
            if (ordersGrid != null)
            {
                await ordersGrid.Reload();
            }
        }


        private List<int> years = new List<int>();



        private async Task SaveItem()
        {


            Item.UpdatedAt = DateTime.Now;
            await FacturaMesRepository.SaveListWithFacturaHijo(Item, orders.Where(x => x.Monto > 0).ToList(), casasSeleccionadas.Select(x => x.Id).ToList());


            await FacturaMesRepository.SaveChangesAsync();
            DialogService.Close(true);
            selectedIndex = 0;
            casasSeleccionadas = new();
            await OnInitializedAsync();
            StateHasChanged();

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
            catch (Exception ex)
            {
                throw;
            }

        }


        private async Task AbrirDialogoCasas()
        {
            // 1. Preparamos los parámetros para pasarle las casas que ya estaban seleccionadas
            var parametros = new Dictionary<string, object>
    {
        { "CasasSeleccionadasPreviamente", casasSeleccionadas }
    };

            var resultado = await DialogService.OpenAsync<CasasListDialog>(
                "Seleccionar Casas a Facturar",
                parametros,
                new DialogOptions { Width = "600px", Height = "600px" });


            // 3. Si el usuario le dio "Aceptar" y devolvió una lista...
            if (resultado is List<Houses> seleccionadas)
            {
                casasSeleccionadas = seleccionadas;
                StateHasChanged(); // Refrescamos la pantalla para que cambie el texto del botón
            }
        }



        void RowRender(RowRenderEventArgs<FacturaMesCasa> args)
        {
            args.Expandable = (args.Data.FacturaMes.FacturaMesHijos != null && args.Data.FacturaMes.FacturaMesHijos.Any());
        }

        void RowExpand(FacturaMesCasa order)
        {
            if (order.FacturaMes.FacturaMesHijos == null)
            {
                order.FacturaMes.FacturaMesHijos = order.FacturaMes.FacturaMesHijos.ToList();
            }
        }
    }
}
