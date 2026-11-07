using Condominio.Application.Services;
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



        private bool _esMobile = false;
        private bool _cargando = true;


        private IEnumerable<FacturaMes> Invoices;
        private int totalFacturas = 0;

        private int currentPage = 1;
        private int TotalPages => (int)Math.Ceiling((double)totalFacturas / 10);
        private string _filtroGlobal = "";



        private async Task ChangePage(int page)
        {
            if (page < 1 || page > TotalPages) return;

            currentPage = page;

            // Llamamos a LoadData con el Skip y Take correctos para la nueva página
            await LoadData(new Radzen.LoadDataArgs
            {
                Skip = (currentPage - 1) * 10,
                Top = 10
            });
        }
        private async Task LoadData(Radzen.LoadDataArgs args)
        {
            try
            {
                await Task.Yield();

                var skip = args.Skip ?? 0;
                var take = args.Top ?? 10;

                string filtroActual = !string.IsNullOrEmpty(args.Filter) ? args.Filter : _filtroGlobal;


                var (datos, total) = await FacturaMesRepository.GetPagedAsync(skip, take, filtroActual);
                Invoices = datos.ToList();
                totalFacturas = total;
              
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR EN LOADDATA: {ex.Message}");
            }
        }

        private async Task BuscarGlobal(string texto)
        {
            _filtroGlobal = texto;
            await LoadData(new Radzen.LoadDataArgs { Skip = 0, Top = 10 });
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    // ✅ Un pequeño delay para asegurar que la pantalla esté renderizada
                    await Task.Delay(100);

                    _esMobile = await JSRuntime.InvokeAsync<bool>("esPantallaMovil");
                    Console.WriteLine($"📱 Mobile: {_esMobile}");

                    if (_esMobile)
                    {
                        // Simulamos la carga de la página 1
                        await LoadData(new Radzen.LoadDataArgs { Skip = 0, Top = 10 });
                    }

                    if (firstRender && !_esMobile && grid != null && Invoices != null && Invoices.Any())
                    {
                        // 2. Buscamos la primera factura que tenga hijos
                        var primeraConHijos = Invoices.FirstOrDefault(x => x.FacturaMesHijos != null && x.FacturaMesHijos.Any());

                        if (primeraConHijos != null)
                        {
                            await grid.ExpandRow(primeraConHijos);
                        }
                    }
                    // Forzamos a Blazor a redibujar la pantalla con el nuevo valor
                    StateHasChanged();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error al detectar mobile: {ex.Message}");
                }
                finally
                {
                    _cargando = false;
                    StateHasChanged();
                }
            }
        }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                await base.OnInitializedAsync();

                Invoices = await FacturaMesRepository.GetAllForGastosAsync();
            }
            catch (Exception ex)
            {
                ;
            }

        }

        void RowRender(RowRenderEventArgs<FacturaMes> args)
        {
            args.Expandable = (args.Data.FacturaMesHijos != null && args.Data.FacturaMesHijos.Any());
        }

        void RowExpand(FacturaMes order)
        {
           
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
            if (factura?.FacturaMesHijos == null) return 0;

            // Sumamos y redondeamos a 2 decimales
            return Math.Round(factura.FacturaMesHijos.Select(x => x.Monto).Sum(), 2);
        }

        private async Task AddUser()
        {
           // await LoadStateAsync();
           //
           // var result = await DialogService.OpenAsync<FacturaDialog>(
           //     "Factura",         // título
           //     null,              // parámetros
           //     new DialogOptions()
           // );
           //
           // if (result != null && (bool)result == true)
           // {
           //     await LoadData();  // Recargar los datos
           //     StateHasChanged(); // Refrescar la UI
           // }

            selectedIndex = 1;



          

        }



        private async Task ConfirmDelete(FacturaMes item)
        {
            try
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
            catch(Exception ex)
            {
                ;
            }
           
        }


        private async Task ConfirmSend(FacturaMes item)
        {
            var result = await DialogService.Confirm(
                $"¿Enviar factura de {ObtenerNombreMes(item.Mes)} {item.Year}?\n\n" +
                $"Esta acción enviará la factura a TODOS los propietarios.\n" +
                $"Los propietarios recibirán la notificación en sus correos.\n\n" +
                $"¿Estás seguro de continuar?",
                "Confirmar envío masivo",
                new ConfirmOptions()
                {
                    OkButtonText = " Sí, enviar a todos",
                    CancelButtonText = " Cancelar",
                }
            );

            if (result == true)
            {
                await MarcarComoEnviado(item);
            }
        }


        private async Task DeleteUser(FacturaMes item)
        {
            await FacturaMesRepository.DeleteWithHijos(item);
            // Si tienes SaveChanges en el repositorio
            if (grid != null)
            {
                await grid.Reload();
            }
            StateHasChanged();

        }


        private async Task MarcarComoEnviado(FacturaMes factura)
        {
            if (factura == null) return;

            // 1. Actualizar el campo Enviado
            factura.Enviado = true;

            // 2. Guardar en la base de datos
            await FacturaMesRepository.DistribuirFacturaEntreCasas(factura);

            // 3. Recargar la lista para actualizar la UI
            if (grid != null)
            {
                await grid.Reload();
            }
            StateHasChanged();
        }

    }




}
