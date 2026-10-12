using Condominio.Application.Services;
using Condominio.Domain.Entities;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace Condominio.Web.Components.Dialogs
{
    public partial class ListPayment
    {

        [Parameter]
        public int IdItem { get; set; }

        [Parameter]
        public bool Cuotas { get; set; } 
        private IEnumerable<Payments> PagosList = Enumerable.Empty<Payments>();
        private FacturaMesCasa FacturaCasa;

        protected override async Task OnInitializedAsync()
        {
            await LoadPagos();

        }

        private async Task LoadPagos()
        {
            if(Cuotas)
            {
                PagosList = await CuotaEspecialCasaRepository.GetPaymentsForInvoiceAsync(IdItem);
            }
            else
            {
                PagosList = await FacturaMesCasaRepository.GetPaymentsForInvoiceAsync(IdItem);
            }
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
                DialogService.Close();


            }




        }

        private async Task ConfirmPayment(Payments payment)
        {

            try
            {
                if (Cuotas)
                {
                    await CuotaEspecialCasaRepository.ConfirmPayment(payment);
                }
                else
                {
                    await FacturaMesCasaRepository.ConfirmPayment(payment);
                }

                await LoadPagos();
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                // Aquí podrías mostrar un mensaje al usuario
            }
        }



    }
}
