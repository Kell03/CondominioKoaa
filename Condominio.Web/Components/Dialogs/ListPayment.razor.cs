using Condominio.Application.Services;
using Condominio.Domain.Entities;
using Microsoft.AspNetCore.Components;

namespace Condominio.Web.Components.Dialogs
{
    public partial class ListPayment
    {

        [Parameter]
        public int IdFactura { get; set; }  // ✅ RECIBIR EL ID
        private IEnumerable<Payments> PagosList = Enumerable.Empty<Payments>();
        private FacturaMesCasa FacturaCasa;

        protected override async Task OnInitializedAsync()
        {
            await LoadPagos();

        }

        private async Task LoadPagos()
        {
            PagosList = await FacturaMesCasaRepository.GetPaymentsForInvoiceAsync(IdFactura);
        }

     


    }
}
