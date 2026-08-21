using Condominio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Radzen;
using Radzen.Blazor;

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
            //selectedUser = new Users();
            //selectedIndex = 1;
            //StateHasChanged();
            //await Task.CompletedTask;
        }
    }
}
