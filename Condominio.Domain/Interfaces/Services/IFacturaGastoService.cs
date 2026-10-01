using Condominio.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Condominio.Domain.Interfaces.Services
{
    public interface IFacturaGastoService
    {
        Task<int> SubirFacturaAsync(
       Stream stream,
       string nombre,
       string contentType,
       long tamano,
       int mes,
       int anio,
       string? comentario);

        Task<List<ImagenesFacturas>> ObtenerTodasAsync();

        Task<string> ObtenerUrlFacturaAsync(int facturaId);

        Task EliminarFacturaAsync(int facturaId);


    }
}
