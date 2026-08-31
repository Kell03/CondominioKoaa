using Condominio.Domain.Entities;
using Condominio.Infrastructure.Repositories;
using Microsoft.AspNetCore.Components; // <-- añadido
using Microsoft.EntityFrameworkCore;
using Radzen;
using Radzen.Blazor;

namespace Condominio.Web.Components.Dialogs
{
    public partial class FacturaDialog
    {

        FacturaMes Item = new FacturaMes();
        private int selectedMonth = DateTime.Now.Month;
        private int selectedYear = DateTime.Now.Year;


        RadzenDataGrid<FacturaMesHijo> ordersGrid;
        IEnumerable<FacturaMesHijo> orders;
        DataGridEditMode editMode = DataGridEditMode.Multiple;

        List<FacturaMesHijo> ordersToInsert = new List<FacturaMesHijo>();
        List<FacturaMesHijo> ordersToUpdate = new List<FacturaMesHijo>();
        private string? yearInput = DateTime.Now.Year.ToString();

        void Reset()
        {
            ordersToInsert.Clear();
            ordersToUpdate.Clear();
        }

        void Reset(FacturaMesHijo order)
        {
            ordersToInsert.Remove(order);
            ordersToUpdate.Remove(order);
        }


        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            orders = new List<FacturaMesHijo>();

        }



        void OnUpdateRow(FacturaMesHijo order)
        {
            Reset(order);

        }


        async Task InsertRow()
        {
            if (!ordersGrid.IsValid) return;

            if (editMode == DataGridEditMode.Single)
            {
                Reset();
            }

            var order = new FacturaMesHijo();
            ordersToInsert.Add(order);
            await ordersGrid.InsertRow(order);
        }

        void OnCreateRow(FacturaMesHijo order)
        {
            
        }


        void CancelEdit(FacturaMesHijo order)
        {
            Reset(order);

            ordersGrid.CancelEditRow(order);
        }


        async Task DeleteRow(FacturaMesHijo order)
        {
            Reset(order);

           
                ordersGrid.CancelEditRow(order);
                await ordersGrid.Reload();
            
        }


        async Task SaveRow(FacturaMesHijo order)
        {
            var t = ordersToInsert.ToList();
            await ordersGrid.UpdateRow(order);
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

        // Inyectar la instancia de DialogService
        [Inject]
        protected DialogService DialogService { get; set; } = default!;

        protected override void OnInitialized()
        {
            // Generar años desde 2020 hasta 2030
            for (int i = 2020; i <= 2030; i++)
            {
                years.Add(i);
            }
        }
        private async Task SaveItem()
        {


            Item.UpdatedAt = DateTime.Now;
            await FacturaMesRepository.SaveWithFacturaHijo(Item, ordersToInsert);


            await FacturaMesRepository.SaveChangesAsync();
            DialogService.Close(true);

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

    }
}
