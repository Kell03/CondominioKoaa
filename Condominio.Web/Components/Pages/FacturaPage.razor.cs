using Condominio.Domain.Entities;
using Condominio.Infrastructure.Repositories;
using Condominio.Web.Components.Dialogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using Microsoft.VisualBasic;
using Radzen;
using Radzen.Blazor;
using System.Runtime;
using System.Text.Json;

namespace Condominio.Web.Components.Pages
{
    public partial class FacturaPage
    {

        RadzenDataGrid<FacturaMes> grid;

        IEnumerable<FacturaMes> Invoices;


        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            Invoices = await FacturaMesRepository.GetAllAsync();

        }

        void RowRender(RowRenderEventArgs<FacturaMes> args)
        {
            args.Expandable = (args.Data.FacturaMesHijos != null && args.Data.FacturaMesHijos.Any());
        }

        void RowExpand(FacturaMes order)
        {
            if (order.FacturaMesHijos == null)
            {
                order.FacturaMesHijos = order.FacturaMesHijos.ToList();
            }
        }


        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            if (firstRender)
            {
                await grid.ExpandRow(Invoices.FirstOrDefault());
            }
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


        private double ObtenerMontoTotal(FacturaMes factura)
        {
            return factura.FacturaMesHijos.Select(x => x.Monto).Sum();
        }


        private async Task AddUser()
        {
            await LoadStateAsync();

      

            var result = await DialogService.OpenAsync<FacturaDialog>(
       "Factura",         // título
       null,              // parámetros
       new DialogOptions()
   );

            if (result != null && (bool)result == true)
            {
                await LoadData();  // Recargar los datos
                StateHasChanged(); // Refrescar la UI
            }

        }


        private async Task LoadData()
        {
            Invoices = await FacturaMesRepository.GetAllAsync();

        }



        private async Task LoadStateAsync()
        {
            await Task.CompletedTask;

            var result = await JSRuntime.InvokeAsync<string>("window.localStorage.getItem", "DialogSettings");
          
        }


        private async Task ConfirmDelete(FacturaMes item)
        {
            var result = await DialogService.Confirm(
                $"¿Estás seguro de eliminar la factura {ObtenerNombreMes(item.Mes)} {item.Year}?",
                "Confirmar eliminación",
                new ConfirmOptions() { OkButtonText = "Sí", CancelButtonText = "No" }
            );

            if (result == true)
            {
                await DeleteUser(item);
            }
        }



        private async Task DeleteUser(FacturaMes item)
        {
            await FacturaMesRepository.DeleteWithHijos(item);
            await FacturaMesRepository.SaveChangesAsync();
            // Si tienes SaveChanges en el repositorio
            await LoadData(); // Recargar la lista
            StateHasChanged();

        }
    }




}
