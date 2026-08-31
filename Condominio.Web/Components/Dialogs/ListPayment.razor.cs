using Condominio.Application.Services;
using Condominio.Domain.Entities;
using Microsoft.AspNetCore.Components;
using Radzen;

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


        private async Task ConfirmSend(Payments item)
        {
            
                var result = await DialogService.Confirm(
                $"¿Confirmar pago con referencia {(item.Referencia)}?\n\n" +
                $"Se le notificará al propietario que su pago ha sido aprobado.\n\n" +
                $"¿Deseas continuar?",
                "Confirmar aprobación de pago",
               new ConfirmOptions()
               {
                   OkButtonText = " Sí, aprobar pago",
                   CancelButtonText = " Cancelar",
               }
          );

                if (result == true)
                {
                    await ConfirmPayment(item);
                }
            
          


        }

        private async Task ConfirmPayment(Payments payment)
        {
                

            await FacturaMesCasaRepository.ConfirmPayment(payment);

            await LoadPagos();
            StateHasChanged();
            

        }



    }
}
